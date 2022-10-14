using System;

namespace DiagnosticsMonitor.ConsoleApp
{
    [Flags]
    public enum ConsoleExitCode
    {
        Success = 0,
        SignToolNotInPath = 1,
        AssemblyDirectoryBad = 2,
        PFXFilePathBad = 4,
        PasswordMissing = 8,
        SignFailed = 16,
        UnknownError = 32,
        TimedOut = 64
    }
}