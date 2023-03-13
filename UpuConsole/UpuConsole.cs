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

        private string? InputFile { get; set; }

        private string? OutputPath { get; set; }

        private bool Register { get; set; }

        private bool Unregister { get; set; }
        
        private bool Metadata { get; set; }

        internal int Start()
        {
            var p = new OptionSet
            {
                // ReSharper disable once StringLiteralTypo
                { "i=|input=", "Unitypackage input file.", i => InputFile = i },
                // ReSharper disable once StringLiteralTypo
                { "o=|output=", "The output path of the extracted unitypackage.", o => OutputPath = o },
                { "m|metadata", "Include metadata in extraction", m =>  Metadata = m != null },
                { "r|register", "Register context menu handler", r => Register = r != null },
                { "u|unregister", "Unregister context menu handler", u => Unregister = u != null }
            };
            
            p.Add("h|help", "Show help", _ => Console.WriteLine(GetUsage(p)));
            p.Parse(Environment.GetCommandLineArgs());
            if (!string.IsNullOrEmpty(InputFile) && !File.Exists(InputFile))
            {
                Console.WriteLine(@"File not found: " + InputFile);
                Console.WriteLine(GetUsage(p));
            }
            if (!string.IsNullOrEmpty(InputFile))
            {
                DoUnpack(InputFile, Metadata);
            }
            return (Register || Unregister) && !RegisterUnregisterShellHandler(Register) ? 1 : 0;
        }

        /// <summary>
        /// Registers or unregisters the shell context menu handler.
        /// </summary>
        /// <param name="register">True to register, false to unregister.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public bool RegisterUnregisterShellHandler(bool register)
        {
            // Check if already registered or unregistered, return true immediately
            if ((register && IsContextMenuHandlerRegistered()) || (!register && !IsContextMenuHandlerRegistered()))
                return true;

            // If not running as admin and not launched with elevated flag, relaunch with elevated privileges
            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator) &&
                !string.Join(" ", Environment.GetCommandLineArgs()).Contains("--elevated"))
            {
                // Store additional command line args to pass on to the elevated process
                if (Environment.GetCommandLineArgs().Length == 1) _mAdditionalCommandLineArgs = !register ? "-u" : "-r";
                return RunElevatedAsAdmin() == 0;
            }

            // Register or unregister the shell context menu handler
            RegisterShellHandler(register);
            return true;
        }

        /// <summary>
        /// Runs the current process with elevated privileges by launching a new instance of the process with the "runas" verb.
        /// </summary>
        /// <returns>0 if the process was started successfully, -1 if there was an error.</returns>
        private int RunElevatedAsAdmin()
        {
            // Combine all command line arguments after the first into a single string.
            var args = "";
            for (var i = 1; i < Environment.GetCommandLineArgs().Length; i++)
            {
                args += Environment.GetCommandLineArgs()[i] + " ";
            }
    
            // Create a new process start info object with the necessary parameters for elevation.
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Environment.ProcessPath,
                Arguments = args + " " + _mAdditionalCommandLineArgs + " --elevated",
                Verb = "runas"
            };
    
            try
            {
                // Start the new process with elevated privileges.
                Process.Start(startInfo);
                return 0;
            }
            catch (Win32Exception ex)
            {
                // If there is an error, print the exception to the console and return -1.
                Console.WriteLine(ex);
                return -1;
            }
        }

        /// <summary>
        /// Registers or unregisters a shell handler for a specific file extension and verb.
        /// </summary>
        /// <param name="register">If true, registers the shell handler; if false, unregisters the shell handler.</param>
        private static void RegisterShellHandler(bool register)
        {
            try
            {
                // Set the file extension and verb for the shell handler
                const string fileExtension = ".UnityPackage";
                const string shellKeyName = "unpack";
                const string shellKeyNameMetadata = "unpack metadata";

                // If registering, call the RegisterShellHandler method to add the context menu entry
                // with the specified text and command
                if (register)
                {
                    const string menuText = "Unpack here";
                    const string menuTextMetadata = "Unpack here with metadata";
                    var command = $"\"{Environment.ProcessPath}\" \"--input=%L\"";
                    var commandMetadata = $"\"{Environment.ProcessPath}\" \"--input=%L\" \"-m\"";
                    RegisterShellHandler(fileExtension, shellKeyName, menuText, command);
                    RegisterShellHandler(fileExtension, shellKeyNameMetadata, menuTextMetadata, commandMetadata);
                }
                // If unregistering, call the UnregisterShellHandler method to remove the context menu entry
                else
                {
                    UnregisterShellHandler(fileExtension, shellKeyName);
                }
            }
            // Catch any UnauthorizedAccessException that may occur when attempting to modify the registry
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine(register
                    ? @"Error: UnauthorizedAccessException. Cannot register explorer context menu handler!"
                    : @"Error: UnauthorizedAccessException. Cannot unregister explorer context menu handler!");
            }
        }

        /// <summary>
        /// Generates a string containing usage information for the command-line options defined by the specified OptionSet object.
        /// </summary>
        /// <param name="p">The OptionSet object containing the command-line options.</param>
        /// <returns>A string containing usage information for the command-line options.</returns>
        private static string GetUsage(OptionSet p)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine();
            // Use a StringWriter to capture the OptionSet's output, and append it to the StringBuilder
            using (var stringWriter = new StringWriter())
            {
                p.WriteOptionDescriptions(stringWriter);
                stringBuilder.AppendLine(stringWriter.ToString());
            }
            // Append additional help text with a link to the software's GitHub repository
            stringBuilder.AppendLine("Help us make to this piece of software even better and contribute!");
            stringBuilder.AppendLine("https://github.com/Myrkie/UpuGui/");
            // Return the final string
            return stringBuilder.ToString();
        }


        /// <summary>
        /// Registers a context menu entry for the specified file type in Windows Explorer.
        /// </summary>
        /// <param name="fileType">The file type to add the context menu entry for.</param>
        /// <param name="shellKeyName">The name of the shell key for the context menu entry.</param>
        /// <param name="menuText">The text to display for the context menu entry.</param>
        /// <param name="menuCommand">The command to execute when the context menu entry is clicked.</param>
        private static void RegisterShellHandler(string fileType, string shellKeyName, string menuText, string menuCommand)
        {
            // Create a subKey for the shell key with the specified name
            using (var subKey = Registry.ClassesRoot.CreateSubKey($"SystemFileAssociations\\{fileType}\\shell\\{shellKeyName}"))
            {
                // Set the default value of the shell key to the specified menu text
                subKey.SetValue(null, menuText);
            }
            // Create a subKey for the icon of the shell key with the specified name
            using (var iconKey = Registry.ClassesRoot.CreateSubKey($"SystemFileAssociations\\{fileType}\\shell\\{shellKeyName}"))
            {
                // Set the "Icon" value of the icon key to the file path of the current process's main module
                iconKey.SetValue("Icon", Process.GetCurrentProcess().MainModule!.FileName);
            }
            // Create a subKey for the command of the shell key with the specified name
            using (var subKey = Registry.ClassesRoot.CreateSubKey($"SystemFileAssociations\\{fileType}\\shell\\{shellKeyName}\\command"))
            {
                // Set the default value of the command key to the specified menu command
                subKey.SetValue(null, menuCommand);
            }
        }


        private static void UnregisterShellHandler(string fileType, string shellKeyName)
        {
            // Check if the file type or shell key name are null or empty, or if the context menu handler is not registered
            if (string.IsNullOrEmpty($"SystemFileAssociations\\{fileType}") || 
                string.IsNullOrEmpty(shellKeyName) ||
                !IsContextMenuHandlerRegistered())
                return;
            // If all checks pass, delete the subKey tree for the given file type and shell key name
            Registry.ClassesRoot.DeleteSubKeyTree($"SystemFileAssociations\\{fileType}");
        }

        public static bool IsContextMenuHandlerRegistered()
        {
            var registryKey = Registry.ClassesRoot.OpenSubKey("SystemFileAssociations\\.Unitypackage\\shell\\Unpack\\command");
            
            return registryKey != null && registryKey.GetValue(null) != null;
        }

        private void DoUnpack(string? fileName, bool metadata)
        {
            try
            {
                KissUnpacker.RemapFiles(KissUnpacker.Unpack(fileName, OutputPath, null), metadata);
                if (KissUnpacker.TempPath != null) Directory.Delete(KissUnpacker.TempPath, true);
            }
            catch (Exception ex)
            {
                var stringbuilder = new StringBuilder();
                stringbuilder.Append(@"==========================================");
                stringbuilder.Append(ex);
                stringbuilder.Append(@"==========================================");
                Console.WriteLine(stringbuilder.ToString());
                if (!Environment.UserInteractive)
                    return;
                Console.WriteLine(@"An error occured (see above)!");
            }
        }
    }
}