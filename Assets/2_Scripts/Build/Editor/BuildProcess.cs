using System;
using System.Diagnostics;
using System.IO;
using System.Text;

/// <summary>Runs command-line tools (git, gh) from the project folder and captures what they print.</summary>
internal static class BuildProcess
{
    public readonly struct Result
    {
        public readonly int ExitCode;
        public readonly string Output;
        public readonly string Error;

        public Result(int exitCode, string output, string error)
        {
            ExitCode = exitCode;
            Output = output;
            Error = error;
        }

        public bool Succeeded => ExitCode == 0;

        /// <summary>Whatever the tool printed, preferring the error stream, for showing in a failure message.</summary>
        public string Message => string.IsNullOrWhiteSpace(Error) ? Output.Trim() : Error.Trim();
    }

    public static string ProjectFolder => Directory.GetParent(UnityEngine.Application.dataPath)?.FullName ?? ".";

    /// <summary>Environment entries are for secrets such as passwords, so they never appear on a command line.</summary>
    public static Result Run(string executable, string arguments, int timeoutSeconds = 60, params (string name, string value)[] environment)
    {
        var info = new ProcessStartInfo(executable, arguments)
        {
            WorkingDirectory = ProjectFolder,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var (name, value) in environment) info.Environment[name] = value;

        try
        {
            using var process = new Process { StartInfo = info };
            var output = new StringBuilder();
            var error = new StringBuilder();
            process.OutputDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) error.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (!process.WaitForExit(timeoutSeconds * 1000))
            {
                try { process.Kill(); } catch (InvalidOperationException) { }
                return new Result(-1, output.ToString(), $"{Path.GetFileName(executable)} timed out after {timeoutSeconds}s");
            }

            process.WaitForExit();
            return new Result(process.ExitCode, output.ToString(), error.ToString());
        }
        catch (Exception e)
        {
            // Most often the tool is not installed, which Process reports as a Win32Exception
            return new Result(-1, string.Empty, $"Could not run {executable}: {e.Message}");
        }
    }

    public static Result Git(string arguments, int timeoutSeconds = 60) => Run("git", arguments, timeoutSeconds);

    /// <summary>
    /// gh is found on PATH, or at its default install location, since an editor that was already open when gh was
    /// installed still has the old PATH.
    /// </summary>
    public static string GhExecutable
    {
        get
        {
            const string defaultInstall = @"C:\Program Files\GitHub CLI\gh.exe";
            return File.Exists(defaultInstall) ? defaultInstall : "gh";
        }
    }

    public static Result Gh(string arguments, int timeoutSeconds = 60) => Run(GhExecutable, arguments, timeoutSeconds);

    /// <summary>Quotes a value for a command line, escaping embedded quotes.</summary>
    public static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";
}
