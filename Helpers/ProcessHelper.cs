using System.Diagnostics;

namespace Shared
{
    public static class ProcessHelper
    {
        public static void RunShell(string path, string? args = default)
            => RunAsync(path, args, true, false).ConfigureAwait(false);
        public static async Task RunShellAsync(string path, string? args = default)
            => await RunAsync(path, args, true, false);

        public static void Run(string path, string? args = default, bool useShell = false)
            => RunAsync(path, args, useShell, false).ConfigureAwait(false);
        public static async Task RunAsync(string path, string? args = default, bool useShell = false)
            => await RunAsync(path, args, useShell, false);

        public static string RunAndReadOutput(string path, string? args = default)
            => RunAsync(path, args, false, true).GetAwaiter().GetResult();
        public static async Task<string> RunAndReadOutputAsync(string path, string? args = default)
            => await RunAsync(path, args, false, true);

        private static async Task<string> RunAsync(string path, string? args, bool useShell, bool readOutput)
        {
            var redirectOutput = !useShell && readOutput;
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = path,
                Arguments = args,
                UseShellExecute = useShell,
                RedirectStandardOutput = redirectOutput,
                CreateNoWindow = !useShell
            });

            if (process is null || process.HasExited)
                throw new InvalidOperationException($"Failed to run {path} {args}");

            if (redirectOutput)
            {
                var output = await process
                    .StandardOutput.ReadToEndAsync();

                await process.WaitForExitAsync();
                process.Close();

                return output;
            }

            return string.Empty;
        }
    }
}
