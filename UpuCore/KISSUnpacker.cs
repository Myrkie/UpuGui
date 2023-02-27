using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UpuGui.tar_cs;

namespace UpuGui.UpuCore
{
    // ReSharper disable once IdentifierTypo
    public class KissUnpacker
    {
        internal static string tempPath;
        private string GetDefaultOutputPathName(string? inputFilepath, string? outputPath = null!)
        {
            var fileInfo = new FileInfo(inputFilepath!);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            // ReSharper disable once ConvertIfStatementToNullCoalescingAssignment
            if (outputPath == null)
            {
                outputPath = fileInfo.Directory!.FullName;
            }
            var output = Path.Combine(outputPath, fileInfo.Name + "_unpacked");

            if (!Directory.Exists(output)) return output;
            Directory.Delete(output,true);
            var path = Path.Combine(outputPath, fileInfo.Name + "_unpacked");
            Directory.CreateDirectory(path);
            output = path;
            return output;
        }

        public string GetTempPath()
        {
            return Path.Combine(Path.Combine(Path.GetTempPath(), "Upu"), Path.GetRandomFileName());
        }

        public Dictionary<string, string> Unpack(string? inputFilepath, string? outputPath, string? tempdir)
        {
            Console.WriteLine($@"Extracting {inputFilepath} to {outputPath}");
            if (!File.Exists(inputFilepath))
            {
                inputFilepath = Path.Combine(Environment.CurrentDirectory, inputFilepath!);
                if (!File.Exists(inputFilepath))
                    throw new FileNotFoundException(inputFilepath);
            }
            // ReSharper disable once StringLiteralTypo
            if (!inputFilepath.ToLower().EndsWith(".unitypackage"))
                // ReSharper disable once StringLiteralTypo
                throw new ArgumentException("File should have unitypackage extension");
            outputPath = GetDefaultOutputPathName(inputFilepath, outputPath);
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);
            tempPath = tempdir ?? GetTempPath();
            var str1 = Path.Combine(tempPath, "_UPU_TAR");
            var tarFileName = DecompressGZip(new FileInfo(inputFilepath), str1);
            var str2 = Path.Combine(tempPath, "content");
            ExtractTar(tarFileName, str2);
            Directory.Delete(str1, true);
            return GenerateRemapInfo(str2, outputPath);
        }

        private Dictionary<string, string> GenerateRemapInfo(string extractedContentPath, string? remapPath)
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var directoryInfo in new DirectoryInfo(extractedContentPath).GetDirectories())
            {
                var path2 = File.ReadAllLines(Path.Combine(directoryInfo.FullName, "pathname"))[0].Replace('/',
                    Path.DirectorySeparatorChar);
                var key = Path.Combine(directoryInfo.FullName, "asset");
                var fileName = Path.Combine(remapPath!, path2);
                dictionary.Add(key, fileName);
            }
            return dictionary;
        }

        public void RemapFiles(Dictionary<string, string> map)
        {
            foreach (var keyValuePair in map)
            {
                var str = keyValuePair.Value;
                var key = keyValuePair.Key;
                var fileInfo = new FileInfo(keyValuePair.Value);
                if (!Directory.Exists(fileInfo.DirectoryName))
                {
                    Console.WriteLine($@"Creating directory {str}...");
                    Directory.CreateDirectory(fileInfo.DirectoryName!);
                }
                if (File.Exists(key))
                {
                    Console.WriteLine($@"Extracting file {str}...");
                    if (File.Exists(str))
                        File.Delete(str);
                    File.Move(key, str);
                }
            }
        }

        private string DecompressGZip(FileInfo fileToDecompress, string outputPath)
        {
            using var fileStream1 = fileToDecompress.OpenRead();
            var path2 = fileToDecompress.Name;
            if (fileToDecompress.Extension.Length > 0)
                path2 = path2.Remove(path2.Length - fileToDecompress.Extension.Length);
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);
            var path = Path.Combine(outputPath, path2);
            using var fileStream2 = File.Create(path);
            using var gzipStream = new GZipStream(fileStream1, CompressionMode.Decompress);
            CopyStreamDotNet20(gzipStream, fileStream2);
            Console.WriteLine(@"Decompressed: {0}", fileToDecompress.Name);

            return path;
        }

        private void CopyStreamDotNet20(Stream input, Stream output)
        {
            var buffer = new byte[32768];
            int count;
            while ((count = input.Read(buffer, 0, buffer.Length)) > 0)
                output.Write(buffer, 0, count);
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private bool ExtractTar(string tarFileName, string destFolder)
        {
            Console.WriteLine($@"Extracting {tarFileName} to {destFolder}...");
            var currentDirectory = Directory.GetCurrentDirectory();
            using (Stream tarredData = File.OpenRead(tarFileName))
            {
                Directory.CreateDirectory(destFolder);
                Directory.SetCurrentDirectory(destFolder);
                new TarReader(tarredData).ReadToEnd(".");
            }
            Directory.SetCurrentDirectory(currentDirectory);
            return true;
        }
    }
}