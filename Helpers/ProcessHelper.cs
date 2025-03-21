﻿using System.Diagnostics;

namespace Shared
{
    public static class ProcessHelper
    {
        const string NoArgs = "";

        //public static void RunShell(string path, string args = NoArgs) => RunAsync(path, args, true, false).Wait();
        //public static async Task RunShellAsync(string path, string args = NoArgs) => await RunAsync(path, args, true, false);

        //public static void Run(string path, string args = NoArgs, bool useShell = false) => RunAsync(path, args, useShell, false).Wait();
        //public static async Task RunAsync(string path, string args = NoArgs, bool useShell = false) => await RunAsync(path, args, useShell, false);

        //public static string RunAndGetOutput(string path, string args = NoArgs) => RunAsync(path, args, false, true).GetAwaiter().GetResult();
        //public static async Task<string> RunAndGetOutputAsync(string path, string args = NoArgs) => await RunAsync(path, args, false, true);

        //private static async Task<string> RunAsync(string path, string args, bool useShell, bool readOutput)
        //{
        //    var p = Process.Start(new ProcessStartInfo
        //    {
        //        FileName = path,
        //        Arguments = args,
        //        UseShellExecute = useShell,
        //        RedirectStandardOutput = !useShell,
        //        CreateNoWindow = !useShell
        //    });

        //    var output = string.Empty;
        //    if (p is null)
        //        return output;

        //    if (readOutput && !useShell)
        //        output = await p.StandardOutput.ReadToEndAsync();

        //    p.WaitForExit();
        //    p.Dispose();

        //    return output;
        //}

        public static void Run(string path, string args = NoArgs)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }

        public static string RunAndReadOutput(string path, string args = NoArgs) => RunAndReadOutputAsync(path, args).GetAwaiter().GetResult();
        public static async Task<string> RunAndReadOutputAsync(string path, string args = NoArgs)
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = path,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            });

            if (process is null || process.HasExited)
                throw new ApplicationException($"Failed to run {path} {args}");

            var output = await process.StandardOutput.ReadToEndAsync();

            if (!process.CloseMainWindow())
                process.Kill();

            process.Close();
            return output;
        }
    }
}
