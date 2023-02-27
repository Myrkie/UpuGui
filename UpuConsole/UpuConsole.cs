using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;
using UpuGui.Mono.Options;
using UpuGui.UpuCore;

namespace UpuGui.UpuConsole
{
    public class UpuConsole
    {
        private string? _mAdditionalCommandLineArgs;
        // ReSharper disable once IdentifierTypo
        private readonly KissUnpacker _mUnpacker = new();

        private string? InputFile { get; set; }

        private string? OutputPath { get; set; }

        private bool Register { get; set; }

        private bool Unregister { get; set; }

        internal int Start()
        {
            var p = new OptionSet
            {
                // ReSharper disable once StringLiteralTypo
                { "i=|input=", "Unitypackage input file.", i => InputFile = i },
                // ReSharper disable once StringLiteralTypo
                { "o=|output=", "The output path of the extracted unitypackage.", o => OutputPath = o },
                { "r|register", "Register context menu handler", r => Register = r != null },
                { "u|unregister", "Unregister context menu handler", u => Unregister = u != null },
            };
            
            p.Add("h|help", "Show help", _ => Console.WriteLine(GetUsage(p)));
            p.Parse(Environment.GetCommandLineArgs());
            if (string.IsNullOrEmpty(InputFile) && !Register && !Unregister)
            {
                Console.WriteLine(GetUsage(p));
            }
            if (!string.IsNullOrEmpty(InputFile) && !File.Exists(InputFile))
            {
                Console.WriteLine(@"File not found: " + InputFile);
                Console.WriteLine(GetUsage(p));
            }
            if (!string.IsNullOrEmpty(InputFile))
            {
                DoUnpack(InputFile);
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
                    _mAdditionalCommandLineArgs = !register ? "-u" : "-r";
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
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Process.GetCurrentProcess().MainModule!.FileName,
                Arguments = str + " " + _mAdditionalCommandLineArgs + " --elevated",
                Verb = "runas"
            };
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

        private void RegisterShellHandler(bool register)
        {
            try
            {
                if (register)
                    RegisterShellHandler("Unity package file", "unpack", "Unpack here",
                        $"\"{Process.GetCurrentProcess().MainModule!.FileName}\" \"--input=%L\"");
                else
                    UnregisterShellHandler("Unity package file", "Unpack");
            }
            catch (UnauthorizedAccessException)
            {
                if (Register)
                    Console.WriteLine(@"Error: UnauthorizedAccessException. Cannot register explorer context menu handler!");
                if (Unregister)
                    Console.WriteLine(@"Error: UnauthorizedAccessException. Cannot register explorer context menu handler!");
            }
        }

        private string GetUsage(OptionSet p)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine();
            using (var stringWriter = new StringWriter())
            {
                p.WriteOptionDescriptions(stringWriter);
                stringBuilder.AppendLine(stringWriter.ToString());
            }
            stringBuilder.AppendLine("Help us make to this piece of software even better and contribute!");
            stringBuilder.AppendLine("https://github.com/Myrkie/UpuGui/");
            return stringBuilder.ToString();
        }

        private void RegisterShellHandler(string fileType, string shellKeyName, string menuText, string menuCommand)
        {
            using (var subKey = Registry.ClassesRoot.CreateSubKey($"{fileType}\\shell\\{shellKeyName}"))
            {
                subKey.SetValue(null, menuText);
            }
            using (var iconKey = Registry.ClassesRoot.CreateSubKey($"{fileType}\\shell\\{shellKeyName}"))
            {
                iconKey.SetValue("Icon", Process.GetCurrentProcess().MainModule!.FileName);
            }
            using (var subKey = Registry.ClassesRoot.CreateSubKey($"{fileType}\\shell\\{shellKeyName}\\command"))
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

        private void DoUnpack(string? fileName)
        {
            try
            {
                _mUnpacker.RemapFiles(_mUnpacker.Unpack(fileName, OutputPath));
            }
            catch (Exception ex)
            {
                Console.WriteLine(@"==========================================");
                Console.WriteLine(ex);
                Console.WriteLine(@"==========================================");
                if (!Environment.UserInteractive)
                    return;
                Console.WriteLine(@"An error occured (see above)!");
            }
        }
    }
}