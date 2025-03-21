using Shared;
using System.Diagnostics;

#region Main
var WriteLock = new object();
const string log = "ProcessTerminator.log";
var o = new ProcessTerminatorOptions();
var processes = Array.Empty<Process>();

try
{
    o = ProcessTerminatorOptions.ParseArguments(args);

    Initialize();
    WaitUntilMainProcessesExits();

    processes = GetProcesesToTerminate();

    DelayBeforeTerminate();
    SendCloseCommand();
    TerminateProcesses();
    RemoveLeftovers();
    DisplayGithubUrl();
}
catch (Exception ex)
{
    WriteLine($"{ex.Message} {ex.StackTrace} {ex.InnerException?.Message} {ex.InnerException?.StackTrace}");
    throw;
}
#endregion

#region Behavior
void Initialize()
{
    if (o.Version || o.Help)
    {
        if (o.Version) DisplayVersionMessage();
        if (o.Help)
        {
            DisplayHelpMessage();
            DisplayGithubUrl();
        }

        ExitProgram();
    }
    else if (string.IsNullOrEmpty(o.ProcessName))
    {
        DisplayHelpMessage();
        DisplayGithubUrl();
        ExitProgram();
    }

    if (o.Log)
    {
        if (!File.Exists(log))
            File.Create(log).Close();

        WriteLine(o.ToString());
    }
}

void DisplayHelpMessage()
{
    var programName = $"{typeof(ProcessTerminatorOptions).Assembly.GetName().Name}";
    var HelpMessage = $"""
Usage: {programName} [OPTIONS] process_name

Description:
    This program terminates processes with configurable options for graceful handling. 
    It allows you to:
        - Wait for another process to exit before proceeding.
        - Specify a delay before sending a close request.
        - Forcefully terminate if the process doesn't exit within the wait time.
        - Cleanup leftover files and directories after termination.

    Ideal for controlled process termination, ensuring system stability.

Options:
    -h, --help          Show this help message and exit.
    -v, --version       Display the program version and exit.
    -m, --monitor       Use to specify a process to wait for before proceeding.
    -i, --interval      Control how often the monitored process is checked.
    -d, --delay         Set a time buffer before sending a close request.
    -c, --command       Send a custom command before attempting to close the process.
    -w, --wait          Set a time buffer to allow the target process to close naturally.
    -r, --remove        Paths (comma-separated) to remove after process termination.
    -l, --log           Enables logging to a file, useful when running without a window.

Arguments:
    process_name        The name of the process to terminate (required)

Note:
    All time-related options (such as delay intervals) are specified in seconds.

Examples:
    {programName} whatsapp
        Attept to exit 'whatsapp' process immediately.

    {programName} -m chrome firefox
        Wait for the 'chrome' process to exit before attempting to exit 'firefox'.

    {programName} -d 10 spotify
        Delay for 10 seconds before attempting to exit 'spotify'.

    {programName} -c "custom_command" -r "D:\temp1,D:\temp2" exif
        Send a custom command to 'exif' before attempting to close it.
        Then delete 'D:\temp1' and 'D:\temp2' after termination.

""";

    WriteLine(HelpMessage);
}
void DisplayVersionMessage()
{
    WriteLine(typeof(ProcessTerminatorWrapper).Assembly.GetName().Version.ToString());
}
void DisplayGithubUrl()
{
    var color = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.DarkBlue;
    WriteLine("https://github.com/BenSabry/ProcessTerminator");
    Console.ForegroundColor = color;
    WriteLine();
}
void ExitProgram()
{
    Environment.Exit(0);
}

void WaitUntilMainProcessesExits()
{
    if (string.IsNullOrWhiteSpace(o.MonitorProcess)) return;

    if (Process.GetProcessesByName(o.MonitorProcess).Any())
        WriteLine($"Waiting {o.MonitorProcess} to exit.");

    while (Process.GetProcessesByName(o.MonitorProcess).Any(i => !i.HasExited))
        Task.Delay(o.IntervalTimeSpan).Wait();
}
void DelayBeforeTerminate()
{
    if (o.Delay == default || processes.Length == default) return;

    WriteLine($"Delaying termination for {o.Delay} seconds.");
    Task.Delay(o.DelayTimeSpan).Wait();
}
Process[] GetProcesesToTerminate()
{
    var processes = Process.GetProcessesByName(o.ProcessName);
    if (processes.Length == default)
        WriteLine($"No {o.ProcessName} instances found to terminate.");

    return processes;
}
void SendCloseCommand()
{
    if (string.IsNullOrWhiteSpace(o.CustomCommand) || processes.Length == default) return;

    WriteLine($"Sending '{o.CustomCommand}' to all instances running.");
    Parallel.ForEach(Process.GetProcessesByName(o.CustomCommand), process =>
    {
        process.StandardInput.WriteLine(o.CustomCommand);
        process.StandardInput.Flush();
    });

    if (o.Wait != default)
    {
        WriteLine($"Waiting for {o.Wait} seconds.");
        Task.Delay(o.WaitTimeSpan).Wait();
    }
}
void TerminateProcesses()
{
    if (processes.Length == default) return;

    WriteLine($"Requesting all {o.ProcessName} instances to close.");
    Parallel.ForEach(processes, process =>
    {
        if (!process.CloseMainWindow() ||
            !process.WaitForExit(o.WaitTimeSpan))
        {
            WriteLine($"Terminating {o.ProcessName} ({process.Id})");
            process.Kill();
        }

        process.Close();
    });
}
void RemoveLeftovers()
{
    if (o.PathesToRemove.Length == default) return;

    foreach (var path in o.PathesToRemove)
    {
        if (!File.Exists(path)) continue;
        WriteLine($"Removing {path}");

        var attributes = File.GetAttributes(path);
        if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
            Directory.Delete(path, true);
        else
            File.Delete(path);
    }
}

void WriteLine(string message = null)
{
    Console.WriteLine(message);
    if (o.Log)
        lock (WriteLock)
            File.AppendAllLines(log, [$"{DateTime.Now.ToString("yyyy.MM.dd-HH:mm:ss")} {message}"]);
}
#endregion

#region Types
public sealed class ProcessTerminatorOptions
{
    public string ProcessName { get; init; }
    public string CustomCommand { get; init; } = string.Empty;
    public string MonitorProcess { get; init; } = string.Empty;
    public int Interval { get; init; } = 1;
    public int Wait { get; init; } = 0;
    public int Delay { get; init; } = 0;
    public string[] PathesToRemove { get; init; } = [];
    public bool Help { get; init; } = false;
    public bool Version { get; init; } = false;
    public bool Log { get; init; } = false;

    public TimeSpan IntervalTimeSpan => TimeSpan.FromSeconds(Interval);
    public TimeSpan WaitTimeSpan => TimeSpan.FromSeconds(Wait);
    public TimeSpan DelayTimeSpan => TimeSpan.FromSeconds(Delay);

    public override string ToString()
    {
        return string.Join(' ', ToArguments());
    }
    public string[] ToArguments()
    {
        var args = new List<string>();

        if (!string.IsNullOrWhiteSpace(CustomCommand))
        {
            args.Add("--command");
            args.Add(CustomCommand);
        }

        if (!string.IsNullOrWhiteSpace(MonitorProcess))
        {
            args.Add("--monitor");
            args.Add(MonitorProcess);
        }

        if (PathesToRemove.Length != default)
        {
            args.Add("--remove");
            args.Add(string.Join(',', PathesToRemove));
        }

        args.AddRange([
            "--interval", Interval.ToString(),
            "--wait", Wait.ToString(),
            "--delay", Delay.ToString(),
        ]);

        if (Help) args.Add($"--help");
        if (Version) args.Add($"--version");
        if (Log) args.Add($"--log");

        args.Add(ProcessName);
        return args.ToArray();
    }

    public static ProcessTerminatorOptions ParseArguments(string[] args)
    {
        var Default = new ProcessTerminatorOptions();

        var processName = Default.ProcessName;
        var customCommand = Default.CustomCommand;
        var monitorProcess = Default.MonitorProcess;
        var interval = Default.Interval;
        var wait = Default.Wait;
        var delay = Default.Delay;
        var remove = Default.PathesToRemove;
        var help = Default.Help;
        var version = Default.Version;
        var log = Default.Log;

        var i = 0;
        while (i < args.Length)
        {
            switch (args[i])
            {
                case "-h":
                case "--help":
                    help = true;
                    break;
                case "-v":
                case "--version":
                    version = true;
                    break;
                case "-l":
                case "--log":
                    log = true;
                    break;
                case "-m":
                case "--monitor":
                    if (i + 1 < args.Length)
                        monitorProcess = args[++i];
                    else
                        throw new ArgumentException("Missing value for --monitor option.");
                    break;
                case "-i":
                case "--interval":
                    if (i + 1 < args.Length)
                    {
                        if (int.TryParse(args[++i], out int _interval))
                            interval = _interval;
                        else
                            throw new ArgumentException("Invalid interval value.");
                    }
                    else
                        throw new ArgumentException("Missing value for --interval option.");
                    break;
                case "-d":
                case "--delay":
                    if (i + 1 < args.Length)
                    {
                        if (int.TryParse(args[++i], out int _delay))
                            delay = _delay;
                        else
                            throw new ArgumentException("Invalid delay value.");
                    }
                    else
                        throw new ArgumentException("Missing value for --delay option.");
                    break;
                case "-c":
                case "--command":
                    if (i + 1 < args.Length)
                        customCommand = args[++i];
                    else
                        throw new ArgumentException("Missing value for --command option.");
                    break;
                case "-w":
                case "--wait":
                    if (i + 1 < args.Length)
                    {
                        if (int.TryParse(args[++i], out int _wait))
                            wait = _wait;
                        else
                            throw new ArgumentException("Invalid wait value.");
                    }
                    else
                        throw new ArgumentException("Missing value for --wait option.");
                    break;
                case "-r":
                case "--remove":
                    if (i + 1 < args.Length)
                        remove = args[++i].Split(",");
                    else
                        throw new ArgumentException("Missing value for --remove option.");
                    break;
                default:
                    if (string.IsNullOrEmpty(processName))
                        processName = args[i];
                    else
                        throw new ArgumentException("Unexpected argument: " + args[i]);
                    break;
            }
            i++;
        }

        return new ProcessTerminatorOptions
        {
            ProcessName = processName,
            CustomCommand = customCommand,
            MonitorProcess = monitorProcess,
            Interval = interval,
            Wait = wait,
            Delay = delay,
            PathesToRemove = remove,
            Help = help,
            Version = version,
            Log = log
        };
    }
}
public sealed class ProcessTerminatorWrapper
{
    #region Fields
    private const string ToolName = "ProcessTerminator.exe";
    private const string ToolPath = $"{ToolName}";

    public readonly string Version = "0.0.0";
    private readonly string Arguments;

    private object Lock = new object();
    private bool IsRunning;
    #endregion

    #region Constructors
    static ProcessTerminatorWrapper()
    {
        if (!File.Exists(ToolPath))
            throw new FileNotFoundException($"{ToolName} is missing!");
    }
    public ProcessTerminatorWrapper(ProcessTerminatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Arguments = options.ToString();

        Version = ProcessHelper.RunAndReadOutput(ToolPath,
            new ProcessTerminatorOptions() { Version = true }.ToString()
            ).Trim();
    }
    #endregion

    #region Behavior
    public void StartWatching()
    {
        // double lock
        if (IsRunning) return;
        lock (Lock)
        {
            if (IsRunning) return;
            IsRunning = true;
        }

        // run
        ProcessHelper.Run(ToolPath, Arguments);
    }
    #endregion
}
#endregion
