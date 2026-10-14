using System;
using System.IO;
using System.Runtime.InteropServices;
namespace Kadaya
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                using (var g = new Game1()) g.Run();
            }
            catch (Exception exception)
            {
                string crashPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kadaya", "crash.log");
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(crashPath));
                    File.WriteAllText(crashPath, DateTimeOffset.Now.ToString("O") + Environment.NewLine + exception);
                }
                catch { crashPath = "the Kadaya crash log could not be written"; }

                if (OperatingSystem.IsWindows())
                    NativeMessageBox(IntPtr.Zero, "KADAYA stopped unexpectedly. Error details: " + crashPath, "KADAYA error", 0x10);
                Environment.ExitCode = 1;
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "MessageBoxW")]
        static extern int NativeMessageBox(IntPtr window, string message, string caption, uint type);
    }
}
