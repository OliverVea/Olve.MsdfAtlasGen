using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Olve.MsdfAtlasGen.Internal
{
    internal static class NativeBinaryLocator
    {
        public static string GetBinaryPath()
        {
            var rid = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win-x64"
                    : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux-x64"
                    : throw new PlatformNotSupportedException("Only Windows and Linux are supported");

            var binaryName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "msdf-atlas-gen.exe"
                : "msdf-atlas-gen";

            var basePath = AppContext.BaseDirectory;
            var binaryPath = Path.Combine(basePath, "runtimes", rid, "native", binaryName);

            if (!File.Exists(binaryPath))
                throw new FileNotFoundException($"Native binary not found: {binaryPath}");

            // On Linux/Mac, ensure it's executable
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    var chmod = Process.Start(new ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = $"+x \"{binaryPath}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                    chmod?.WaitForExit();
                }
                catch
                {
                    // If chmod fails, the binary might already be executable
                    // or we might not have permissions to change it
                }
            }

            return binaryPath;
        }
    }
}
