﻿using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// ReSharper disable once CheckNamespace
namespace UpuGui
{
    internal static class Program
    {
        // ReSharper disable once InconsistentNaming
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
                if (!AttachConsole(ATTACH_PARENT_PROCESS)) AllocConsole();
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