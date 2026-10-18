using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Drova.TestRunner;

/// <summary>
/// Runs a scenario file, every scenario in a folder, or a list of them against one or more Drova instances.
///
/// Usage: <c>Drova.TestRunner &lt;scenario | folder | list.txt&gt; [--game &lt;dir&gt;] [--keep] [--no-record]
/// [--language &lt;code|keep&gt;] [-D name=value]...</c>
///
/// Each instance needs the Drova Modding API (its TestAgent, inert without <c>--agent-port</c>) and whatever mods
/// are under test. Instances are started with <c>--profile &lt;name&gt; --agent-port &lt;port&gt;</c>, so each keeps its
/// own savegames under <c>LocalLow\Just2D\Drova\Sprint_24_&lt;name&gt;</c>; copy a save there once.
///
/// A scenario is one step per line; <c>#</c> starts a comment. Steps:
/// <list type="bullet">
/// <item><c>launch A [extra game args]</c> - start Drova.exe as profile A with its own agent port, wait for the agent.</item>
/// <item><c>attach A &lt;port&gt;</c> - use an instance that is already running with <c>--agent-port</c>.</item>
/// <item><c>A &lt;agent command&gt;</c> - send it (see TestAgent for the built-in commands; mods add their own); an <c>err</c> fails the scenario.</item>
/// <item><c>A wait key=value [timeout=s]</c> - poll <c>state</c> until it holds; a value of <c>*</c> means "anything but -".</item>
/// <item><c>A ?&lt;agent command&gt;</c> - send it and ignore a failing answer.</item>
/// <item><c>A expect &lt;agent command&gt; == text [timeout=s]</c>, <c>contains text</c>, <c>!contains text</c>, <c>&gt;= number</c> or <c>&lt;= number</c> - poll the command until its answer matches.</item>
/// <item><c>sleep &lt;seconds&gt;</c>, <c>set name value</c>, <c>default name value</c> (a -D wins), <c>include &lt;file&gt;</c>.</item>
/// </list>
/// <c>$name</c> is replaced by a variable; <c>$A.key</c> by that key from A's last <c>state</c>.
/// Exit code 0 when every step of every scenario passed, 1 otherwise.
/// </summary>
internal static class Program
{
    private const int FirstPort = 47101;
    private const int DefaultTimeoutSeconds = 30;

    private static readonly Dictionary<string, Instance> _instances = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> _variables = new(StringComparer.OrdinalIgnoreCase);

    private static string? _gameDir;
    private static bool _keep;

    // Both instances recorded throughout (Recorder), kept with a failure's logs. On unless --no-record.
    private static bool _record = true;

    // language.txt as it was before the run, put back when the run ends; null content means there was none.
    private static (string File, string? Content)? _languageBefore;

    private static int Main(string[] args)
    {
        string? target = null;
        string language = "en";

        for (int index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--game" when index + 1 < args.Length: _gameDir = args[++index]; break;
                case "--keep": _keep = true; break;
                case "--no-record": _record = false; break;
                case "--language" when index + 1 < args.Length: language = args[++index]; break;
                case "-D" when index + 1 < args.Length:
                    string[] pair = args[++index].Split('=', 2);
                    if (pair.Length == 2) _variables[pair[0]] = pair[1];
                    break;
                default: target = args[index]; break;
            }
        }

        List<string>? scenarios = target == null ? null : Scenarios(target);
        if (scenarios == null || scenarios.Count == 0)
        {
            Console.Error.WriteLine("usage: Drova.TestRunner <scenario | folder | list.txt> [--game <dir>] [--keep] [--no-record] [--language <code|keep>] [-D name=value]");
            return 2;
        }

        _gameDir ??= GamePaths.FindGameDir();
        Dictionary<string, string> defines = new(_variables, StringComparer.OrdinalIgnoreCase);
        List<(string Name, TimeSpan Time, string? Failure)> results = new();

        Console.CancelKeyPress += (_, _) => RestoreLanguage();
        try
        {
            if (language != "keep") SetLanguage(language);

            for (int index = 0; index < scenarios.Count; index++)
            {
                string scenario = scenarios[index];
                if (scenarios.Count > 1) Console.WriteLine($"=== {Path.GetFileName(scenario)}");

                _variables.Clear();
                foreach (KeyValuePair<string, string> define in defines) _variables[define.Key] = define.Value;

                Stopwatch clock = Stopwatch.StartNew();
                string? failure = RunScenario(scenario, index == scenarios.Count - 1);
                results.Add((Path.GetFileName(scenario), clock.Elapsed, failure));

                // --keep in a suite: stop at the first failure and leave its games running.
                if (failure != null && _keep) break;
            }
        }
        finally
        {
            RestoreLanguage();
        }

        if (scenarios.Count > 1)
        {
            Console.WriteLine();
            Console.WriteLine($"{results.Count(result => result.Failure == null)}/{scenarios.Count} passed");
            foreach ((string name, TimeSpan time, string? failure) in results)
            {
                Console.WriteLine($"  {(failure == null ? "PASSED" : "FAILED")} {time:mm\\:ss} {name}{(failure == null ? "" : "  " + failure)}");
            }
        }

        return results.Count == scenarios.Count && results.All(result => result.Failure == null) ? 0 : 1;
    }

    /// <summary>Runs one scenario, printing each step and PASSED or FAILED.</summary>
    /// <param name="scenario">Path of the scenario file.</param>
    /// <param name="last">Whether the run ends after it: only then does <c>--keep</c> leave passing games running.</param>
    /// <returns>Null when it passed, otherwise the FAILED line.</returns>
    private static string? RunScenario(string scenario, bool last)
    {
        List<string> lines = Expand(scenario);
        string? failure = null;
        Stopwatch clock = Stopwatch.StartNew();
        if (_record) Recorder.Start(() => _instances.Values.Select(instance => (instance.Name, instance.Port)).ToArray());

        try
        {
            for (int index = 0; index < lines.Count; index++)
            {
                string line = StripComment(lines[index]);
                if (line.Length == 0) continue;

                string step = Substitute(line);
                Console.Write($"[{clock.Elapsed:mm\\:ss}] {index + 1,3}: {step}");

                string? error = Run(step);
                if (error == null)
                {
                    Console.WriteLine("  ok");
                    continue;
                }

                Console.WriteLine();
                failure = $"FAILED at line {index + 1}: {error}";
                Console.Error.WriteLine(failure);
                break;
            }
        }
        finally
        {
            foreach (Instance instance in _instances.Values)
            {
                instance.Dispose(_keep && (last || failure != null));
            }
        }

        string? kept = failure != null ? KeepLogs(scenario, _instances.Count) : null;
        _instances.Clear();
        if (_record)
        {
            Recorder.Stop(kept, Path.Combine(Path.GetDirectoryName(Path.GetFullPath(scenario))!, "recordings",
                Path.GetFileNameWithoutExtension(scenario)));
        }

        Console.WriteLine(failure == null ? $"PASSED in {clock.Elapsed:mm\\:ss}" : "FAILED");
        return failure;
    }

    /// <summary>
    /// A scenario file, every .scn and .coop directly in a folder, or the scenarios a .txt lists one per line (a name
    /// without extension is tried as .coop, then .scn, relative to the list). Null when a listed one is missing.
    /// </summary>
    private static List<string>? Scenarios(string target)
    {
        if (Directory.Exists(target))
        {
            return Directory.EnumerateFiles(target)
                .Where(file => Path.GetExtension(file).ToLowerInvariant() is ".scn" or ".coop")
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        if (!File.Exists(target)) return null;
        if (!Path.GetExtension(target).Equals(".txt", StringComparison.OrdinalIgnoreCase)) return [target];

        string dir = Path.GetDirectoryName(Path.GetFullPath(target))!;
        List<string> scenarios = new();
        foreach (string raw in File.ReadAllLines(target))
        {
            string name = StripComment(raw);
            if (name.Length == 0) continue;

            string path = Path.Combine(dir, name);
            string? found = new[] { path, path + ".coop", path + ".scn" }.FirstOrDefault(File.Exists);
            if (found == null)
            {
                Console.Error.WriteLine($"{target}: no scenario '{name}'");
                return null;
            }

            scenarios.Add(found);
        }

        return scenarios;
    }

    /// <summary>Writes the game's language.txt (read once at start) after remembering what it held.</summary>
    private static void SetLanguage(string language)
    {
        string file = Path.Combine(GamePaths.SaveRoot, "language.txt");
        if (!Directory.Exists(GamePaths.SaveRoot))
        {
            Console.WriteLine($"no save folder at {GamePaths.SaveRoot}; language left alone");
            return;
        }

        _languageBefore = (file, File.Exists(file) ? File.ReadAllText(file) : null);
        File.WriteAllText(file, language);
    }

    private static void RestoreLanguage()
    {
        if (_languageBefore is not { } before) return;
        _languageBefore = null;

        if (before.Content == null) File.Delete(before.File);
        else File.WriteAllText(before.File, before.Content);
    }

    /// <summary>
    /// Copies the newest MelonLoader log of each instance next to the scenario, under failed-logs: the game keeps a
    /// limited number, and a batch of runs rotated the logs of a failure away before anyone read them.
    /// </summary>
    /// <returns>Where they were kept, or null.</returns>
    private static string? KeepLogs(string scenario, int instances)
    {
        if (_gameDir == null) return null;

        try
        {
            string logs = Path.Combine(_gameDir, "MelonLoader", "Logs");
            if (!Directory.Exists(logs) || instances == 0) return null;

            string target = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(scenario))!, "failed-logs",
                $"{DateTime.Now:yyyyMMdd-HHmmss}_{Path.GetFileNameWithoutExtension(scenario)}");
            Directory.CreateDirectory(target);

            foreach (FileInfo log in new DirectoryInfo(logs).GetFiles("*.log").OrderByDescending(file => file.LastWriteTimeUtc).Take(instances))
            {
                log.CopyTo(Path.Combine(target, log.Name), true);
            }

            Console.WriteLine("logs kept in " + target);
            return target;
        }
        catch (Exception e)
        {
            Console.WriteLine("could not keep the logs: " + e.Message);
            return null;
        }
    }

    /// <returns>Null when the step passed, otherwise why it did not.</returns>
    private static string? Run(string step)
    {
        string[] words = step.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        switch (words[0].ToLowerInvariant())
        {
            case "launch":
                return Launch(words[1], string.Join(' ', words.Skip(2)));

            case "attach":
                return Attach(words[1], int.Parse(words[2]));

            case "sleep":
                Thread.Sleep(TimeSpan.FromSeconds(double.Parse(words[1], System.Globalization.CultureInfo.InvariantCulture)));
                return null;

            case "set":
                _variables[words[1]] = string.Join(' ', words.Skip(2));
                return null;

            // "capture <name> <instance> <command>": the answer (without "ok ") kept as $name, so one machine's value can be
            // expected on another - a teammate's max health read off their own player.
            case "capture":
            {
                if (words.Length < 4 || !_instances.TryGetValue(words[2], out Instance? source) || source == null) return "capture needs <name> <instance> <command>";
                string captured = source.Send(string.Join(' ', words.Skip(3)));
                if (!captured.StartsWith("ok ", StringComparison.Ordinal)) return captured;
                _variables[words[1]] = captured.Substring(3).Trim();
                Console.Write("  -> " + Shorten(captured));
                return null;
            }

            // "track <guid>": that actor's ai readout is recorded from every instance (see Recorder).
            case "track":
                Recorder.Track(words[1]);
                return null;

            // Like set, but a -D on the command line wins.
            case "default":
                _variables.TryAdd(words[1], string.Join(' ', words.Skip(2)));
                return null;
        }

        if (!_instances.TryGetValue(words[0], out Instance? target) || target == null)
        {
            return $"no instance or step named '{words[0]}'";
        }

        string rest = step.Substring(words[0].Length).Trim();

        if (words.Length > 1 && words[1].Equals("wait", StringComparison.OrdinalIgnoreCase))
        {
            return Wait(target, rest.Substring(4).Trim());
        }

        if (words.Length > 1 && words[1].Equals("expect", StringComparison.OrdinalIgnoreCase))
        {
            return Expect(target, rest.Substring(6).Trim());
        }

        // "A ?command": sent, answered, never failing - a blow at a wave that may already be gone, a hit on a name
        // that is not always there.
        if (rest.StartsWith('?'))
        {
            Console.Write("  -> " + Shorten(target.Send(rest.Substring(1))));
            return null;
        }

        string answer = target.Send(rest);
        if (rest.StartsWith("state", StringComparison.OrdinalIgnoreCase)) target.RememberState(answer);

        Console.Write("  -> " + Shorten(answer));
        return answer.StartsWith("ok", StringComparison.Ordinal) ? null : answer;
    }

    private static string? Launch(string name, string extra, bool retry = true)
    {
        if (_gameDir == null) return "Drova not found in any Steam library (pass --game <dir>)";
        string? exe = GamePaths.Executable(_gameDir);
        if (exe == null) return "no Drova executable in " + _gameDir + " (pass --game <dir>)";

        // A single letter keeps its own port (A 47101, B 47102, C 47103...), so a scenario on C alone does not collide
        // with an A and B kept running; other names count up from the first.
        int port = name.Length == 1 && char.IsLetter(name[0]) ? FirstPort + char.ToUpperInvariant(name[0]) - 'A' : FirstPort + _instances.Count;

        // A test instance renders windowed at 1280x720 (ConsolePlacer then fits it into its half of the second monitor),
        // not at the screen's native resolution: two or three native instances kept
        // the GPU hot for nothing. Unity's own flags, placed before `extra` so a scenario can still override them. More logs
        // kept than MelonLoader's ten: a run of several scenarios rotated the failing one's logs away before anyone read them.
        Process process = Process.Start(new ProcessStartInfo(exe, $"--profile {name} --agent-port {port} -screen-fullscreen 0 -screen-width 1280 -screen-height 720 --melonloader.maxlogs 60 {extra}")
        {
            WorkingDirectory = _gameDir,
            // Its own console window, not this one: MelonLoader's log would bury the step results.
            UseShellExecute = true,
        })!;

        ConsolePlacer.Place(process.Id, port - FirstPort);

        Instance instance = new(name, port, process);
        _instances[name] = instance;

        // MelonLoader's first start per instance generates nothing new, but loading the game still takes a while.
        if (instance.Connect(TimeSpan.FromMinutes(3))) return null;

        // The game sometimes hangs for good while loading mods, right after the agent starts (about 1 launch in 6):
        // one fresh start, not a failed scenario.
        instance.Dispose();
        if (retry)
        {
            Console.Write($"  (agent for {name} never answered; starting it again)");
            return Launch(name, extra, retry: false);
        }

        return $"agent for {name} never answered on port {port} (is the Drova Modding API installed?)";
    }

    private static string? Attach(string name, int port)
    {
        Instance instance = new(name, port, null);
        _instances[name] = instance;

        return instance.Connect(TimeSpan.FromSeconds(10)) ? null : $"nothing answers on port {port}";
    }

    /// <summary><c>key=value [timeout=s]</c> against the instance's state line.</summary>
    private static string? Wait(Instance target, string condition)
    {
        (string body, TimeSpan timeout) = SplitTimeout(condition);
        string[] pair = body.Split('=', 2);
        if (pair.Length != 2) return "wait needs key=value";

        string last = string.Empty;

        bool held = Poll(timeout, () =>
        {
            last = target.Send("state");
            target.RememberState(last);

            string? value = target.StateValue(pair[0]);
            return value != null && (pair[1] == "*" ? value != "-" : value == pair[1]);
        });

        return held ? null : $"{pair[0]} never became {pair[1]} within {timeout.TotalSeconds:0}s; last: {last}";
    }

    /// <summary><c>&lt;command&gt; == text</c> or <c>&lt;command&gt; contains text</c>, polled until it holds.</summary>
    private static string? Expect(Instance target, string condition)
    {
        (string body, TimeSpan timeout) = SplitTimeout(condition);

        Match match = Regex.Match(body, @"^(?<command>.+?)\s+(?<op>==|!=|!contains|contains|>=|<=)\s+(?<text>.*)$");
        if (!match.Success) return "expect needs '<command> == text', '!= text', 'contains text', '>= number' or '<= number'";

        string command = match.Groups["command"].Value;
        string op = match.Groups["op"].Value;
        string text = match.Groups["text"].Value.Trim('"');
        string last = string.Empty;

        bool held = Poll(timeout, () =>
        {
            last = target.Send(command);
            if (!last.StartsWith("ok ", StringComparison.Ordinal)) return false;

            string value = last.Substring(3);

            // >= and <= compare the answer's first number: "walked >= 300", "teammate <= 500".
            if (op is ">=" or "<=")
            {
                Match number = Regex.Match(value, @"-?\d+(\.\d+)?");
                if (!number.Success) return false;

                double found = double.Parse(number.Value, System.Globalization.CultureInfo.InvariantCulture);
                double wanted = double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
                return op == ">=" ? found >= wanted : found <= wanted;
            }

            if (op == "!contains") return !value.Contains(text, StringComparison.OrdinalIgnoreCase);
            if (op == "!=") return value != text;
            return op == "==" ? value == text : value.Contains(text, StringComparison.OrdinalIgnoreCase);
        });

        return held ? null : $"'{command}' never {op} '{text}' within {timeout.TotalSeconds:0}s; last: {last}";
    }

    private static bool Poll(TimeSpan timeout, Func<bool> condition)
    {
        Stopwatch watch = Stopwatch.StartNew();

        while (true)
        {
            if (condition()) return true;
            if (watch.Elapsed > timeout) return false;

            Thread.Sleep(500);
        }
    }

    private static (string Body, TimeSpan Timeout) SplitTimeout(string text)
    {
        Match match = Regex.Match(text, @"\s+timeout=(?<s>\d+)\s*$");
        if (!match.Success) return (text, TimeSpan.FromSeconds(DefaultTimeoutSeconds));

        return (text.Substring(0, match.Index), TimeSpan.FromSeconds(int.Parse(match.Groups["s"].Value)));
    }

    private static string Substitute(string line)
    {
        return Regex.Replace(line, @"\$(?<name>[A-Za-z0-9_]+)(\.(?<key>[A-Za-z0-9_]+))?", match =>
        {
            string name = match.Groups["name"].Value;

            if (match.Groups["key"].Success && _instances.TryGetValue(name, out Instance? instance) && instance != null)
            {
                return instance.StateValue(match.Groups["key"].Value) ?? match.Value;
            }

            return _variables.TryGetValue(name, out string? value) ? value : match.Value;
        });
    }

    /// <summary>The scenario's lines with every <c>include &lt;file&gt;</c> replaced by that file's, relative to the includer.</summary>
    private static List<string> Expand(string path)
    {
        List<string> lines = new();

        foreach (string raw in File.ReadAllLines(path))
        {
            string line = StripComment(raw);

            if (line.StartsWith("include ", StringComparison.OrdinalIgnoreCase))
            {
                lines.AddRange(Expand(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, line.Substring(8).Trim())));
                continue;
            }

            lines.Add(raw);
        }

        return lines;
    }

    private static string StripComment(string line)
    {
        int hash = line.IndexOf('#');
        return (hash >= 0 ? line.Substring(0, hash) : line).Trim();
    }

    private static string Shorten(string text) => text.Length <= 20000 ? text : text.Substring(0, 19997) + "...";

    /// <summary>One game instance and the connection to its agent.</summary>
    private sealed class Instance : IDisposable
    {
        private readonly string _name;
        private readonly int _port;
        private readonly Process? _process;
        private readonly Dictionary<string, string> _state = new(StringComparer.OrdinalIgnoreCase);

        private TcpClient? _client;
        private StreamReader? _reader;
        private StreamWriter? _writer;

        internal string Name => _name;

        internal int Port => _port;

        internal Instance(string name, int port, Process? process)
        {
            _name = name;
            _port = port;
            _process = process;
        }

        internal bool Connect(TimeSpan timeout)
        {
            return Poll(timeout, () =>
            {
                if (_process is { HasExited: true }) return false;

                try
                {
                    _client = new TcpClient("127.0.0.1", _port);
                    NetworkStream stream = _client.GetStream();
                    _reader = new StreamReader(stream, Encoding.UTF8);
                    _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };

                    return Send("ping") == "ok pong";
                }
                catch (SocketException)
                {
                    return false;
                }
            });
        }

        internal string Send(string line)
        {
            if (_writer == null || _reader == null) return "err not connected";

            try
            {
                _writer.WriteLine(line);
                return _reader.ReadLine() ?? "err connection closed";
            }
            catch (IOException e)
            {
                return $"err {_name} connection lost: {e.Message}";
            }
        }

        internal void RememberState(string answer)
        {
            if (!answer.StartsWith("ok ", StringComparison.Ordinal)) return;

            foreach (string pair in answer.Substring(3).Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = pair.Split('=', 2);
                if (parts.Length == 2) _state[parts[0]] = parts[1];
            }
        }

        internal string? StateValue(string key) => _state.TryGetValue(key, out string? value) ? value : null;

        public void Dispose()
        {
            Dispose(keep: false);
        }

        internal void Dispose(bool keep)
        {
            _client?.Dispose();

            if (keep || _process == null || _process.HasExited) return;

            try
            {
                _process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // Already gone.
            }
        }
    }
}
