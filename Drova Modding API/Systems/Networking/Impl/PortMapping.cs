using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using MelonLogger = MelonLoader.MelonLogger;

namespace Drova_Modding_API.Systems.Networking.Impl
{
    /// <summary>
    /// Opens a direct host's UDP port on the router through UPnP. Plain sockets and HTTP rather than Windows' UPnP COM
    /// object, which Wine and Proton do not have. The router calls run in order on a worker: open, renew, close.
    /// </summary>
    internal sealed class PortMapping
    {
        private const string SearchAddress = "239.255.255.250";
        private const int SearchPort = 1900;
        private const int SearchTimeoutMs = 3_000;
        private const string Description = "Drova Co-op";

        /// <summary>Short enough that a crashed game leaves the port open for an hour at most.</summary>
        private const int LeaseSeconds = 3_600;

        private const long RenewMs = 1_800_000;
        private const long RenewRetryMs = 60_000;

        private static readonly string[] GatewayTypes =
        {
            "urn:schemas-upnp-org:device:InternetGatewayDevice:1",
            "urn:schemas-upnp-org:device:InternetGatewayDevice:2",
        };

        /// <summary>WANIP first: a router can list an idle PPP service next to the one actually in use.</summary>
        private static readonly string[] ServicePrefixes =
        {
            "urn:schemas-upnp-org:service:WANIPConnection:",
            "urn:schemas-upnp-org:service:WANPPPConnection:",
        };

        /// <summary>A system proxy must not get the requests meant for the router.</summary>
        private static readonly HttpClient Http = new(new HttpClientHandler { UseProxy = false })
        {
            Timeout = TimeSpan.FromSeconds(3),
            MaxResponseContentBufferSize = 256 * 1024,
        };

        private readonly int _port;
        private Task _work;
        private string? _controlUrl;
        private string? _serviceType;
        private string? _localAddress;
        private bool _mapped;
        private long _renewAtMs = long.MaxValue;

        internal PortMapping(int port)
        {
            _port = port;
            _work = Task.Run(Open);
        }

        internal void Poll()
        {
            if (!_work.IsCompleted || !_mapped || Environment.TickCount64 < _renewAtMs) return;

            _renewAtMs = long.MaxValue;
            _work = Task.Run(Renew);
        }

        /// <summary>Closes the port once the calls before it finished.</summary>
        /// <returns>Completes when the router answered, or at once when the port was never opened.</returns>
        internal Task Close()
        {
            _work = _work.ContinueWith(_ => Delete(), TaskScheduler.Default);
            return _work;
        }

        private void Open()
        {
            try
            {
                if (!FindGateway())
                {
                    MelonLogger.Msg($"[Networking] no UPnP router answered. Forward UDP port {_port} by hand, or let joiners punch through.");
                    return;
                }

                if (Map()) MelonLogger.Msg($"[Networking] the router opened UDP port {_port} through UPnP.");
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"[Networking] UPnP could not open UDP port {_port}: {e.Message}");
            }
        }

        private void Renew()
        {
            try
            {
                Map();
            }
            catch (Exception e)
            {
                _renewAtMs = Environment.TickCount64 + RenewRetryMs;
                MelonLogger.Warning($"[Networking] UPnP could not renew UDP port {_port}: {e.Message}");
            }
        }

        private void Delete()
        {
            if (!_mapped) return;

            try
            {
                string? error = Soap("DeletePortMapping",
                    $"<NewRemoteHost></NewRemoteHost><NewExternalPort>{_port}</NewExternalPort><NewProtocol>UDP</NewProtocol>");

                _mapped = false;
                if (error == null) MelonLogger.Msg($"[Networking] the router closed UDP port {_port} again.");
                else MelonLogger.Warning($"[Networking] the router refused to close UDP port {_port}, its lease ends within the hour: {error}");
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"[Networking] UPnP could not close UDP port {_port}, its lease ends within the hour: {e.Message}");
            }
        }

        private bool Map()
        {
            string? error = Soap("AddPortMapping",
                $"<NewRemoteHost></NewRemoteHost><NewExternalPort>{_port}</NewExternalPort><NewProtocol>UDP</NewProtocol>" +
                $"<NewInternalPort>{_port}</NewInternalPort><NewInternalClient>{_localAddress}</NewInternalClient><NewEnabled>1</NewEnabled>" +
                $"<NewPortMappingDescription>{Description}</NewPortMappingDescription><NewLeaseDuration>{LeaseSeconds}</NewLeaseDuration>");

            if (error != null)
            {
                _renewAtMs = Environment.TickCount64 + RenewRetryMs;
                MelonLogger.Warning($"[Networking] the router refused to open UDP port {_port}: {error}");
                return false;
            }

            _mapped = true;
            _renewAtMs = Environment.TickCount64 + RenewMs;
            return true;
        }

        private bool FindGateway()
        {
            using Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));

            IPEndPoint group = new(IPAddress.Parse(SearchAddress), SearchPort);
            foreach (string type in GatewayTypes)
            {
                string search = $"M-SEARCH * HTTP/1.1\r\nHOST: {SearchAddress}:{SearchPort}\r\nMAN: \"ssdp:discover\"\r\nMX: 2\r\nST: {type}\r\n\r\n";
                socket.SendTo(Encoding.ASCII.GetBytes(search), group);
            }

            byte[] buffer = new byte[2048];
            HashSet<string> tried = new();
            long deadline = Environment.TickCount64 + SearchTimeoutMs;

            while (true)
            {
                long left = deadline - Environment.TickCount64;
                if (left <= 0) return false;

                socket.ReceiveTimeout = (int)left;
                EndPoint from = new IPEndPoint(IPAddress.Any, 0);
                int size;
                try
                {
                    size = socket.ReceiveFrom(buffer, ref from);
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.TimedOut)
                {
                    return false;
                }
                catch (SocketException)
                {
                    // An oversized or broken answer from one device must not end the search.
                    continue;
                }

                string? location = ReadHeader(Encoding.ASCII.GetString(buffer, 0, size), "LOCATION");
                if (location != null && tried.Add(location) && ReadGateway(location, ((IPEndPoint)from).Address)) return true;
            }
        }

        /// <param name="location">Where the answer says the description is.</param>
        /// <param name="answeredFrom">Who answered. Any LAN device can answer, so the description and the control URL must be on it.</param>
        private bool ReadGateway(string location, IPAddress answeredFrom)
        {
            XDocument description;
            Uri locationUri;
            try
            {
                locationUri = new Uri(location);
                if (locationUri.Scheme != Uri.UriSchemeHttp || !IsHost(locationUri, answeredFrom)) return false;

                description = XDocument.Parse(Http.GetStringAsync(locationUri).GetAwaiter().GetResult());
            }
            catch (Exception)
            {
                // Whatever one device answers, the search goes on with the next.
                return false;
            }

            List<XElement> services = description.Descendants().Where(element => element.Name.LocalName == "service").ToList();
            foreach (string prefix in ServicePrefixes)
            {
                foreach (XElement service in services)
                {
                    string? type = Child(service, "serviceType");
                    string? control = Child(service, "controlURL");

                    // The type goes into the request body and header, so only a version number may follow the prefix.
                    if (type == null || control == null || !type.StartsWith(prefix, StringComparison.Ordinal)
                        || type.Length == prefix.Length || !type[prefix.Length..].All(c => c is >= '0' and <= '9')) continue;

                    if (!Uri.TryCreate(locationUri, control, out Uri? controlUri) || controlUri.Scheme != Uri.UriSchemeHttp
                        || !IsHost(controlUri, answeredFrom) || controlUri.Port != locationUri.Port) continue;

                    _serviceType = type;
                    _controlUrl = controlUri.ToString();
                    _localAddress = LocalAddressTowards(locationUri.Host);
                    return true;
                }
            }

            return false;
        }

        /// <returns>Null on success, else the router's reason.</returns>
        private string? Soap(string action, string arguments)
        {
            string body = "<?xml version=\"1.0\"?><s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" " +
                "s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
                $"<s:Body><u:{action} xmlns:u=\"{_serviceType}\">{arguments}</u:{action}></s:Body></s:Envelope>";

            using HttpRequestMessage request = new(HttpMethod.Post, _controlUrl);
            request.Content = new StringContent(body, Encoding.UTF8, "text/xml");
            request.Headers.TryAddWithoutValidation("SOAPAction", $"\"{_serviceType}#{action}\"");

            using HttpResponseMessage response = Http.SendAsync(request).GetAwaiter().GetResult();
            if (response.IsSuccessStatusCode) return null;

            string? reason = ReadErrorDescription(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            return reason == null ? $"HTTP {(int)response.StatusCode}" : $"HTTP {(int)response.StatusCode}, {reason}";
        }

        private static string? ReadErrorDescription(string answer)
        {
            try
            {
                return XDocument.Parse(answer).Descendants().FirstOrDefault(element => element.Name.LocalName == "errorDescription")?.Value;
            }
            catch (XmlException)
            {
                return null;
            }
        }

        private static bool IsHost(Uri uri, IPAddress address)
        {
            return IPAddress.TryParse(uri.Host, out IPAddress? host) && host.Equals(address);
        }

        private static string LocalAddressTowards(string host)
        {
            // Connecting a UDP socket sends nothing. It only picks the interface that reaches the router.
            using Socket probe = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            probe.Connect(host, 1);
            return ((IPEndPoint)probe.LocalEndPoint!).Address.ToString();
        }

        private static string? ReadHeader(string response, string name)
        {
            foreach (string line in response.Split("\r\n"))
            {
                int colon = line.IndexOf(':');
                if (colon > 0 && line.AsSpan(0, colon).Trim().Equals(name, StringComparison.OrdinalIgnoreCase)) return line.Substring(colon + 1).Trim();
            }

            return null;
        }

        private static string? Child(XElement parent, string name)
        {
            return parent.Elements().FirstOrDefault(element => element.Name.LocalName == name)?.Value.Trim();
        }
    }
}
