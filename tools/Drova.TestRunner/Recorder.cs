using System.Net.Sockets;
using System.Text;

namespace Drova.TestRunner;

/// <summary>
/// Records every instance while a scenario runs: a screenshot and the agent's <c>state</c> every 2 s, and each tracked
/// actor's <c>ai</c> readout (from every instance) every 4 s. Kept with the failure logs when a run fails; the last
/// few passed runs' are kept in <c>recordings/&lt;scenario&gt;</c>. Intermittent sync bugs leave nothing in the logs.
///
/// Its own connection per question, so it never interleaves with the steps' connection.
/// </summary>
internal static class Recorder
{
    private static readonly List<string> _tracked = [];
    private static string? _dir;
    private static Thread? _thread;
    private static volatile bool _stop;

    /// <summary>Adds an actor (guid) whose <c>ai</c> readout is recorded from every instance.</summary>
    internal static void Track(string guid)
    {
        lock (_tracked)
        {
            if (!_tracked.Contains(guid)) _tracked.Add(guid);
        }
    }

    /// <summary>Starts recording whatever instances <paramref name="instances"/> lists at each tick.</summary>
    internal static void Start(Func<(string Name, int Port)[]> instances)
    {
        _dir = Path.Combine(Path.GetTempPath(), "drova-runner", Environment.ProcessId.ToString());
        Directory.CreateDirectory(_dir);
        _stop = false;
        lock (_tracked) _tracked.Clear();

        _thread = new Thread(() =>
        {
            for (int tick = 0; !_stop; tick++)
            {
                string time = DateTime.Now.ToString("HHmmss");
                (string Name, int Port)[] targets;
                try { targets = instances(); } catch { targets = []; }

                StringBuilder index = new();
                foreach ((string name, int port) in targets)
                {
                    Ask(port, $"shot {_dir.Replace('\\', '/')}/{time}_{name}.png");
                    index.AppendLine($"{time} {name} {Ask(port, "state")}");

                    if (tick % 2 != 0) continue;

                    string[] tracked;
                    lock (_tracked) tracked = [.. _tracked];
                    foreach (string guid in tracked) index.AppendLine($"{time} {name} {guid} {Ask(port, "ai " + guid)}");
                }

                File.AppendAllText(Path.Combine(_dir, "index.txt"), index.ToString());

                Thread.Sleep(2000);
            }
        }) { IsBackground = true };
        _thread.Start();
    }

    /// <summary>How many recordings of passed runs are kept.</summary>
    private const int KeptPassed = 5;

    /// <summary>
    /// Stops recording. A failed run's recording goes to <paramref name="keepIn"/>; a passed one's to
    /// <paramref name="passedRoot"/>, keeping the last few - a run can pass with the bug in it (an NPC that walked off
    /// and came back in time), and the recording is the only trace.
    /// </summary>
    internal static void Stop(string? keepIn, string passedRoot)
    {
        _stop = true;
        _thread?.Join(TimeSpan.FromSeconds(10));
        if (_dir == null || !Directory.Exists(_dir)) return;

        try
        {
            if (keepIn != null)
            {
                Directory.Move(_dir, Path.Combine(keepIn, "record"));
                Console.WriteLine("recording kept in " + Path.Combine(keepIn, "record"));
            }
            else
            {
                Directory.CreateDirectory(passedRoot);
                Directory.Move(_dir, Path.Combine(passedRoot, DateTime.Now.ToString("yyyyMMdd-HHmmss")));
                foreach (DirectoryInfo old in new DirectoryInfo(passedRoot).GetDirectories().OrderByDescending(dir => dir.Name).Skip(KeptPassed))
                {
                    old.Delete(true);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("could not keep the recording: " + e.Message);
        }
    }

    private static string Ask(int port, string command)
    {
        try
        {
            using TcpClient client = new("127.0.0.1", port);
            client.ReceiveTimeout = 4000;
            client.SendTimeout = 4000;
            using NetworkStream stream = client.GetStream();
            using StreamWriter writer = new(stream, new UTF8Encoding(false));
            writer.AutoFlush = true;
            writer.NewLine = "\n";
            using StreamReader reader = new(stream, Encoding.UTF8);
            writer.WriteLine(command);
            return reader.ReadLine() ?? "-";
        }
        catch (Exception e)
        {
            return "unreachable: " + e.GetType().Name;
        }
    }
}
