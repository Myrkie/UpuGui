// Decompiled with JetBrains decompiler
// Type: UpuConsole.UpuConsole
// Assembly: UpuGui, Version=1.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DD1D21B2-102B-4937-9736-F13C7AB91F14
// Assembly location: C:\Users\veyvin\Desktop\UpuGui.exe

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;
using Mono.Options;
using UpuCore;

namespace UpuConsole
{
    public class UpuConsole
    {
        private string m_additionalCommandLineArgs;
        private readonly KISSUnpacker m_unpacker = new KISSUnpacker();

        public string InputFile { get; private set; }

        public string OutputPath { get; private set; }

        public bool Register { get; private set; }

        public bool Unregister { get; private set; }

        internal int Start()
        {
            var p = new OptionSet
                {
                    {
                        "i=|input=",
                        "Unitypackage input file.",
                        i => InputFile = i
                    }
                }.Add("o:|output:", "The output path of the extracted unitypackage.", o => OutputPath = o)
                .Add("r|register", "Register context menu handler", r => Register = r != null)
                .Add("u|unregister", "Unregister context menu handler", u => Unregister = u != null);
            p.Add("h|help", "Show help", h => Console.WriteLine(GetUsage(p)));
            p.Parse(Environment.GetCommandLineArgs());
            if (string.IsNullOrEmpty(InputFile) && !Register && !Unregister)
            {
                Console.WriteLine(GetUsage(p));
                return 1;
            }
            if (!string.IsNullOrEmpty(InputFile) && !File.Exists(InputFile))
            {
                Console.WriteLine("File not found: " + InputFile);
                Console.WriteLine(GetUsage(p));
                return 2;
            }
            if (!string.IsNullOrEmpty(InputFile))
            {
                DoUnpack(InputFile);
                return 0;
            }
            return (Register || Unregister) && !RegisterUnregisterShellHandler(Register) ? 1 : 0;
        }

        public bool RegisterUnregisterShellHandler(bool register)
        {
            if ((register && IsContextMenuHandlerRegistered()) || (!register && !IsContextMenuHandlerRegistered()))
                return true;
            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator) &&
                !string.Join(" ", Environment.GetCommandLineArgs()).Contains("--elevated"))
            {
                if (Environment.GetCommandLineArgs().Length == 1)
                    m_additionalCommandLineArgs = !register ? "-u" : "-r";
                return RunElevatedAsAdmin() == 0;
            }
            RegisterShellHandler(Register);
            return true;
        }

        private int RunElevatedAsAdmin()
        {
            var str = "";
            for (var index = 1; index < Environment.GetCommandLineArgs().Length; ++index)
                str = str + Environment.GetCommandLineArgs()[index] + " ";
            var startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
            startInfo.Arguments = str + " " + m_additionalCommandLineArgs + " --elevated";
            startInfo.Verb = "runas";
            try
            {
                Process.Start(startInfo);
                return 0;
            }
            catch (Win32Exception ex)
            {
                Console.WriteLine(ex);
                return -1;
            }
        }

        private bool RegisterShellHandler(bool register)
        {
            try
            {
                if (register)
                    RegisterShellHandler("Unity package file", "Unpack", "Unpack here",
                        string.Format("\"{0}\" \"--input=%L\"", System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName));
                else
                    UnregisterShellHandler("Unity package file", "Unpack");
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                if (Register)
                    Console.WriteLine(
                        "Error: UnauthorizedAccessException. Cannot register explorer context menu handler!");
                if (Unregister)
                    Console.WriteLine(
                        "Error: UnauthorizedAccessException. Cannot register explorer context menu handler!");
            }
            return false;
        }

        public string GetUsage(OptionSet p)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine();
            using (var stringWriter = new StringWriter())
            {
                p.WriteOptionDescriptions(stringWriter);
                stringBuilder.AppendLine(stringWriter.ToString());
            }
            stringBuilder.AppendLine("Help us make to this piece of software even better and contribute!");
            stringBuilder.AppendLine("https://github.com/ChimeraEntertainment/UPU");
            return stringBuilder.ToString();
        }

        private void RegisterShellHandler(string fileType, string shellKeyName, string menuText, string menuCommand)
        {
            using (var subKey = Registry.ClassesRoot.CreateSubKey("Unity package file\\shell\\Unpack"))
            {
                subKey.SetValue(null, menuText);
            }
            using (var subKey = Registry.ClassesRoot.CreateSubKey("Unity package file\\shell\\Unpack\\command"))
            {
                subKey.SetValue(null, menuCommand);
            }
        }

        private void UnregisterShellHandler(string fileType, string shellKeyName)
        {
            if (string.IsNullOrEmpty(fileType) || string.IsNullOrEmpty(shellKeyName) ||
                !IsContextMenuHandlerRegistered())
                return;
            Registry.ClassesRoot.DeleteSubKeyTree("Unity package file\\shell\\Unpack");
        }

        public bool IsContextMenuHandlerRegistered()
        {
            var registryKey = Registry.ClassesRoot.OpenSubKey("Unity package file\\shell\\Unpack\\command");
            return (registryKey != null) && (registryKey.GetValue(null) != null);
        }

        internal void DoUnpack(string fileName)
        {
            try
            {
                m_unpacker.RemapFiles(m_unpacker.Unpack(InputFile, OutputPath));
            }
            catch (Exception ex)
            {
                Console.WriteLine("==========================================");
                Console.WriteLine(ex);
                Console.WriteLine("==========================================");
                if (!Environment.UserInteractive)
                    return;
                Console.WriteLine("An error occured (see above)!");
            }
        }
    }
}