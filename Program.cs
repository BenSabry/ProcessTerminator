using Shared;
using System.Diagnostics;

#region Main
var o = ProcessTerminatorOptions
    .ParseArguments(args);

HandleArguments();
WaitUntilMainProcessesExits();
DisplayGithubUrl();

var processes = GetProcesesToTerminate();

DelayBeforeTerminate();
SendCloseCommand();
TerminateProcesses();

#if DEBUG
Console.WriteLine("Press any key to Exit.");
Console.ReadKey();
#endif
#endregion

#region Behavior
void HandleArguments()
{
    if (o.Version || o.Help)
    {
        if (o.Version) DisplayVersionMessage();
        if (o.Help)
        {
            DisplayGithubUrl();
            DisplayHelpMessage();
        }

        ExitProgram();
    }
    else if (string.IsNullOrEmpty(o.ProcessName))
    {
        DisplayGithubUrl();
        DisplayHelpMessage();
        ExitProgram();
    }
}

static void DisplayHelpMessage()
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

    Ideal for controlled process termination, ensuring system stability.

Options:
    -h, --help          Show this help message and exit.
    -v, --version       Display the program version and exit.
    -m, --monitor       Use to specify a process to wait for before proceeding.
    -i, --interval      Control how often the monitored process is checked.
    -d, --delay         Set a time buffer before sending a close request.
    -c, --command       Send a custom command before attempting to close the process.
    -w, --wait          Set a time buffer to allow the target process to close naturally.

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

    {programName} -c "custom_command" exif
        Send a custom command to 'exif' before attempting to close it.
""";

    Console.WriteLine(HelpMessage);
}
static void DisplayVersionMessage()
{
    Console.WriteLine(typeof(ProcessTerminator).Assembly.GetName().Version.ToString());
}
static void DisplayGithubUrl()
{
    var color = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.DarkBlue;
    Console.WriteLine("https://github.com/BenSabry/ProcessTerminator");
    Console.ForegroundColor = color;
    Console.WriteLine();
}
static void ExitProgram()
{
    Environment.Exit(0);
}

void WaitUntilMainProcessesExits()
{
    if (string.IsNullOrWhiteSpace(o.MonitorProcess)) return;

    if (Process.GetProcessesByName(o.MonitorProcess).Any())
        Console.WriteLine($"Waiting {o.MonitorProcess} to exit.");

    while (Process.GetProcessesByName(o.MonitorProcess).Any(i => !i.HasExited))
        Task.Delay(o.IntervalTimeSpan).Wait();
}
void DelayBeforeTerminate()
{
    if (o.Delay == default) return;

    Console.WriteLine($"Delaying termination for {o.Delay} seconds.");
    Task.Delay(o.DelayTimeSpan).Wait();
}
Process[] GetProcesesToTerminate()
{
    var process = Process.GetProcessesByName(o.ProcessName);
    if (process.Length == default)
    {
        Console.WriteLine($"No {o.ProcessName} instances found to terminate.");
        ExitProgram();
    }

    return process;
}
void SendCloseCommand()
{
    if (string.IsNullOrWhiteSpace(o.CustomCommand)) return;

    Console.WriteLine($"Sending '{o.CustomCommand}' to all instances running.");
    Parallel.ForEach(Process.GetProcessesByName(o.CustomCommand), process =>
    {
        process.StandardInput.WriteLine(o.CustomCommand);
        process.StandardInput.Flush();
    });
}
void TerminateProcesses()
{
    Console.WriteLine($"Requesting all {o.ProcessName} instances to close.");
    Parallel.ForEach(processes, process =>
    {
        process.CloseMainWindow();
        if (!process.WaitForExit(o.DelayTimeSpan))
        {
            Console.WriteLine($"Terminating {o.ProcessName} ({process.Id})");
            process.Kill();
        }

        process.Close();
    });
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
    public bool Help { get; init; } = false;
    public bool Version { get; init; } = false;

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

        args.AddRange([
            "--interval", Interval.ToString(),
            "--wait", Wait.ToString(),
            "--delay", Delay.ToString(),
        ]);

        if (Help) args.Add($"--help");
        if (Version) args.Add($"--version");

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
        var help = Default.Help;
        var version = Default.Version;

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
            Help = help,
            Version = version
        };
    }
}
public sealed class ProcessTerminator
{
    #region Fields
    private const string ToolName = "ProcessTerminator.exe";
    private const string ToolPath = $"Tools\\{ToolName}";

    public readonly string Version = "0.0.0";
    private readonly string Arguments;

    private object Lock = new object();
    private bool IsRunning;
    #endregion

    #region Constructors
    public ProcessTerminator(ProcessTerminatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Arguments = options.ToString();

        Version = ProcessHelper.RunAndGetOutput(ToolPath,
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
        ProcessHelper
            .RunAsync(ToolPath, Arguments)
            .ConfigureAwait(false);
    }
    #endregion
}
#endregion
