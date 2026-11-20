using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("Pip")]
[assembly: AssemblyProduct("Pip - Taskbar fox")]
[assembly: AssemblyDescription("A little red fox that lives on the Windows taskbar.")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace Pip
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Native.SetProcessDpiAwarenessContext(new IntPtr(-4));
            bool first;
            using (var single = new Mutex(true, "Local\\Pip.TaskbarFox.DesktopPet", out first))
            {
                if (!first) return;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                using (var pet = new Pet()) Application.Run(pet);
            }
        }
    }
}
