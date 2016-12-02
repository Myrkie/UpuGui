// Decompiled with JetBrains decompiler
// Type: UpuGui.Program
// Assembly: UpuGui, Version=1.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DD1D21B2-102B-4937-9736-F13C7AB91F14
// Assembly location: C:\Users\veyvin\Desktop\UpuGui.exe

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UpuGui
{
    internal static class Program
    {
        private const int ATTACH_PARENT_PROCESS = -1;

        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [STAThread]
        private static void Main(string[] args)
        {
            var upu = new UpuConsole.UpuConsole();
            if (args.Length > 0)
            {
                if (!AttachConsole(-1))
                    AllocConsole();
                var exitCode = 0;
                try
                {
                    exitCode = upu.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                FreeConsole();
                if (!string.Join(" ", Environment.GetCommandLineArgs()).Contains("--elevated"))
                    SendKeys.SendWait("{ENTER}");
                Environment.Exit(exitCode);
            }
            else
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new UpuGui(upu));
            }
        }
    }
}