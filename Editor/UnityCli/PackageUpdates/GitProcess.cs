#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Elympics.Cysharp.Threading.Tasks;

namespace Elympics.Editor.UnityCli.PackageUpdates
{
    internal interface IGitProcess
    {
        /// <exception cref="InvalidOperationException">git failed, timed out or isn't installed; the message says which.</exception>
        UniTask<string> ListRemoteRefsAsync(string url, TimeSpan timeout);
    }

    /// <summary>Runs git without ever prompting, so a credential window can't block the command, and from the home directory, so the project's repo config doesn't apply.</summary>
    internal sealed class GitProcess : IGitProcess
    {
        public UniTask<string> ListRemoteRefsAsync(string url, TimeSpan timeout) => UniTask.RunOnThreadPool(() =>
        {
            if (!GitDependency.IsSupportedRemote(url))
                throw new InvalidOperationException($"Unsupported git URL: {GitDependency.RedactCredentials(url)}");
            var stopwatch = Stopwatch.StartNew();
            var startInfo = CreateStartInfo("ls-remote", "--tags", "--heads", "--", url);
            // BatchMode keeps ssh from prompting, but only where the user has no ssh command of their own to override.
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GIT_SSH_COMMAND")) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GIT_SSH"))
                && !HasConfiguredSshCommand(timeout))
                startInfo.Environment["GIT_SSH_COMMAND"] = "ssh -o BatchMode=yes";

            using var process = Start(startInfo);
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            var timedOut = new InvalidOperationException($"git ls-remote didn't finish within {timeout.TotalSeconds:0} s.");
            if (!WaitOrKill(process, timeout - stopwatch.Elapsed))
                throw timedOut;
            // A helper that inherited the pipes can keep them open after git exits.
            if (!Task.WaitAll(new Task[] { output, error }, (int)Math.Max(0, (timeout - stopwatch.Elapsed).TotalMilliseconds)))
                throw timedOut;
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"git ls-remote exited with {process.ExitCode}: {GitDependency.RedactCredentials(error.Result.Trim())}");
            return output.Result;
        });

        private bool? _hasConfiguredSshCommand;

        private bool HasConfiguredSshCommand(TimeSpan timeout) =>
            _hasConfiguredSshCommand ??= !string.IsNullOrWhiteSpace(ReadConfig("core.sshCommand", timeout));

        // On Windows, Process.Start looks in the current directory (often the project root) before PATH.
        private static string ResolveGit()
        {
            var fileName = Environment.OSVersion.Platform == PlatformID.Win32NT ? "git.exe" : "git";
            foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator))
            {
                var trimmed = directory.Trim().Trim('"');
                if (trimmed.Length == 0 || !Path.IsPathRooted(trimmed))
                    continue;
                var candidate = Path.Combine(trimmed, fileName);
                if (File.Exists(candidate))
                    return candidate;
            }
            throw new InvalidOperationException("git isn't available: it isn't on PATH.");
        }

        private static ProcessStartInfo CreateStartInfo(params string[] arguments)
        {
            var startInfo = new ProcessStartInfo(ResolveGit())
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            };
            foreach (var argument in arguments)
                startInfo.ArgumentList.Add(argument);
            startInfo.Environment["GIT_ALLOW_PROTOCOL"] = "https:ssh";
            startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";
            startInfo.Environment["GCM_INTERACTIVE"] = "never";
            startInfo.Environment["GIT_ASKPASS"] = string.Empty;
            startInfo.Environment["SSH_ASKPASS"] = string.Empty;
            startInfo.Environment["NoDefaultCurrentDirectoryInExePath"] = "1";
            return startInfo;
        }

        private static Process Start(ProcessStartInfo startInfo)
        {
            try
            {
                return Process.Start(startInfo) ?? throw new InvalidOperationException("git didn't start.");
            }
            catch (System.ComponentModel.Win32Exception e)
            {
                throw new InvalidOperationException($"git isn't available: {e.Message}");
            }
        }

        private static string? ReadConfig(string key, TimeSpan timeout)
        {
            using var process = Start(CreateStartInfo("config", "--get", key));
            var output = process.StandardOutput.ReadToEndAsync();
            _ = process.StandardError.ReadToEndAsync();
            if (!WaitOrKill(process, timeout))
                return null;
            return process.ExitCode == 0 ? output.Result : null;
        }

        private static bool WaitOrKill(Process process, TimeSpan timeout)
        {
            if (process.WaitForExit((int)Math.Max(0, timeout.TotalMilliseconds)))
                return true;
            try
            {
                process.Kill();
            }
            catch (InvalidOperationException)
            { }
            return false;
        }
    }
}
