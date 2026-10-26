using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Elympics.Editor.UnityCli.PackageUpdates
{
    internal interface IGitProcess
    {
        /// <exception cref="InvalidOperationException">git failed, timed out or isn't installed; the message says which.</exception>
        Task<string> LsRemoteAsync(string url, TimeSpan timeout);
    }

    /// <summary>
    /// Runs git without ever prompting: a credential window would block the command for its whole timeout.
    /// git runs from the home directory with current-directory lookup off, so a project can't plant its own git executable.
    /// </summary>
    internal sealed class GitProcess : IGitProcess
    {
        public Task<string> LsRemoteAsync(string url, TimeSpan timeout) => Task.Run(() =>
        {
            if (!GitDependency.IsSupportedRemote(url))
                throw new InvalidOperationException($"Unsupported git URL: {GitDependency.RedactCredentials(url)}");
            var startInfo = CreateStartInfo($"ls-remote --tags --heads -- \"{url}\"");
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GIT_SSH_COMMAND")) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GIT_SSH"))
                && string.IsNullOrWhiteSpace(ReadConfig("core.sshCommand")))
                startInfo.Environment["GIT_SSH_COMMAND"] = "ssh -o BatchMode=yes";

            var process = Start(startInfo);

            using (process)
            {
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit((int)timeout.TotalMilliseconds))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (InvalidOperationException)
                    { }
                    throw new InvalidOperationException($"git ls-remote didn't finish within {timeout.TotalSeconds:0} s.");
                }
                if (process.ExitCode != 0)
                    throw new InvalidOperationException($"git ls-remote exited with {process.ExitCode}: {GitDependency.RedactCredentials(error.Result.Trim())}");
                return output.Result;
            }
        });

        private static ProcessStartInfo CreateStartInfo(string arguments)
        {
            var startInfo = new ProcessStartInfo("git", arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            };
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

        private static string ReadConfig(string key)
        {
            using var process = Start(CreateStartInfo($"config --get {key}"));
            var output = process.StandardOutput.ReadToEndAsync();
            _ = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(5000))
            {
                try
                {
                    process.Kill();
                }
                catch (InvalidOperationException)
                { }
                return null;
            }
            return process.ExitCode == 0 ? output.Result : null;
        }
    }
}
