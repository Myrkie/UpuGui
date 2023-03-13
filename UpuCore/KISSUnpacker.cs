using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;

namespace UpuGui.UpuCore
{
    // ReSharper disable once IdentifierTypo
    public class KissUnpacker
    {
        internal static string? TempPath;
        /// <summary>
        /// Generates default output path for the unpacked files.
        /// </summary>
        /// <param name="inputFilePath">The input file path.</param>
        /// <param name="outputPath">The output path. If null, the parent directory of the input file is used.</param>
        /// <returns>The default output path name.</returns>
        private static string GetDefaultOutputPathName(string? inputFilePath, string? outputPath = null!)
        {
            var inputFile = new FileInfo(inputFilePath!);
            // If no output path is provided, use the parent directory of the input file
            var defaultOutputPath = outputPath ?? inputFile.Directory!.FullName;

            // Create the output path by combining the output directory and the input file name with "_unpacked" suffix
            var output = Path.Combine(defaultOutputPath, inputFile.Name + "_unpacked");

            // If the output directory already exists, delete it and create a new one with a different name
            if (!Directory.Exists(output)) return output;
            Directory.Delete(output, true);
            var newPath = Path.Combine(defaultOutputPath, inputFile.Name + "_unpacked");
            Directory.CreateDirectory(newPath);
            output = newPath;

            return output;
        }


        /// <summary>
        /// Returns a randomly generated file path in the user's temporary folder, with an "Upu" subdirectory.
        /// </summary>
        /// <returns>A string representing the full path to the file.</returns>
        public static string GetTempPath()
        {
            // Combine the user's temporary folder path, "Upu" subdirectory, and a randomly generated file name to create the full path.
            return Path.Combine(Path.Combine(Path.GetTempPath(), "Upu"), Path.GetRandomFileName());
        }

        /// <summary>
        /// Unpacks a Unity package at the specified input file path to the specified output path.
        /// </summary>
        /// <param name="inputFilepath">The input file path.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="tempdir">The temporary directory path. If not specified, a system-provided temporary directory will be used.</param>
        /// <returns>A dictionary containing remap information for the extracted files.</returns>
        public static Dictionary<string, string> Unpack(string? inputFilepath, string? outputPath, string? tempdir)
        {
            // Print a message indicating that the package is being extracted
            Console.WriteLine($@"Extracting {inputFilepath} to {outputPath}");

            // If the input file path doesn't exist, try to construct a path based on the current directory
            if (!File.Exists(inputFilepath))
            {
                inputFilepath = Path.Combine(Environment.CurrentDirectory, inputFilepath!);
                if (!File.Exists(inputFilepath)) throw new FileNotFoundException(inputFilepath);
            }

            // Check that the input file has a ".unitypackage" extension
            if (!inputFilepath.ToLower().EndsWith(".unitypackage"))
                throw new ArgumentException("File should have unitypackage extension");

            // If the output directory doesn't exist, create it
            outputPath = GetDefaultOutputPathName(inputFilepath, outputPath);
            if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

            // Determine the temporary directory path
            TempPath = tempdir ?? GetTempPath();

            // Create a path for the temporary tar file and decompress the input file to it
            var tempTarPath = Path.Combine(TempPath, "_UPU_TAR");
            var tarFilePath = DecompressGZip(new FileInfo(inputFilepath), tempTarPath);

            // Create a path for the extracted content and extract the tar file to it
            var extractedContentPath = Path.Combine(TempPath, "content");
            ExtractTar(tarFilePath, extractedContentPath);

            // Delete the temporary tar file
            Directory.Delete(tempTarPath, true);

            // Generate remap information for the extracted files and return it
            return GenerateRemapInfo(extractedContentPath, outputPath);
        }

        /// <summary>
        /// Generates remap information for the extracted files in the specified directory.
        /// </summary>
        /// <param name="extractedContentPath">The path to the directory containing the extracted files.</param>
        /// <param name="remapPath">The path to the directory to which the files will be remapped.</param>
        /// <returns>A dictionary containing remap information for the extracted files.</returns>
        private static Dictionary<string, string> GenerateRemapInfo(string extractedContentPath, string? remapPath)
        {
            // Create an empty dictionary to hold the remap information
            var remapInfo = new Dictionary<string, string>();

            // Loop through each subdirectory in the extracted content directory
            foreach (var directory in new DirectoryInfo(extractedContentPath).GetDirectories())
            {
                // Read the "pathname" file to get the path to the file within the Unity project
                var path = File.ReadAllLines(Path.Combine(directory.FullName, "pathname"))[0].Replace('/',
                    Path.DirectorySeparatorChar);

                // Create paths for the asset and its remapped location, and add them to the dictionary
                var assetPath = Path.Combine(directory.FullName, "asset");
                var remappedPath = Path.Combine(remapPath!, path);
                remapInfo.Add(assetPath, remappedPath);
            }

            // Return the dictionary containing the remap information
            return remapInfo;
        }

        public static void RemapFiles(Dictionary<string, string> map, bool metadata)
        {
            // creates temp dictionary
            var tempdict = new Dictionary<string, string>();
            
            // if metadata extract is enabled iterate the original dictionary and add meta files 
            if (metadata)
            {
                foreach (var (sourcePath, destinationPath) in map)
                {
                    tempdict.Add(sourcePath, destinationPath);
                    tempdict.Add(sourcePath + ".meta", destinationPath + ".meta");
                }
            }
            else
            {
                tempdict = map;
            }
            
            // Extract values from the dictionary
            foreach (var (sourcePath, destinationPath) in tempdict)
            {

                // Create a FileInfo object to get the directory path
                var fileInfo = new FileInfo(destinationPath);

                // Create the directory if it doesn't already exist
                if (!Directory.Exists(fileInfo.DirectoryName))
                {
                    Console.WriteLine($@"Creating directory {destinationPath}...");
                    Directory.CreateDirectory(fileInfo.DirectoryName!);
                }

                // Check if the source file exists
                if (!File.Exists(sourcePath))
                {
                    continue;
                }

                // Move the file to the destination
                Console.WriteLine($@"Extracting file {destinationPath}...");
                if (File.Exists(destinationPath))
                {
                    File.Delete(destinationPath);
                }
                File.Move(sourcePath, destinationPath);
            }
        }


        /// <summary>
        /// Decompresses a file with GZip compression and writes the decompressed data to a file.
        /// </summary>
        /// <param name="fileToDecompress">The file to decompress.</param>
        /// <param name="outputDirectory">The directory in which to write the decompressed file.</param>
        /// <returns>The path to the decompressed file.</returns>
        private static string DecompressGZip(FileInfo fileToDecompress, string outputDirectory)
        {
            // Open the input file stream
            using var inputFileStream = fileToDecompress.OpenRead();

            // Get the name of the file to be decompressed
            var fileName = fileToDecompress.Name;
            if (fileToDecompress.Extension.Length > 0)
                fileName = fileName.Remove(fileName.Length - fileToDecompress.Extension.Length);

            // Create the output directory if it does not already exist
            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            // Create the output file path
            var outputPath = Path.Combine(outputDirectory, fileName);

            // Open the output file stream and create a GZip stream to decompress the input data
            using var outputFileStream = File.Create(outputPath);
            using var gzipStream = new GZipStream(inputFileStream, CompressionMode.Decompress);

            // Copy the decompressed data to the output file stream
            CopyStream(gzipStream, outputFileStream);

            // Print a message indicating that the file has been decompressed
            Console.WriteLine(@"Decompressed: {0}", fileToDecompress.Name);

            // Return the path to the decompressed file
            return outputPath;
        }

        /// <summary>
        /// Copies data from one stream to another.
        /// </summary>
        /// <param name="source">The stream to copy from.</param>
        /// <param name="destination">The stream to copy to.</param>
        private static void CopyStream(Stream source, Stream destination)
        {
            // Create a buffer to hold the data being copied.
            var buffer = new byte[32768];

            // Keep reading from the source stream and writing to the destination stream
            // until there's no more data to be read.
            int bytesRead;
            while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                // Write the data from the buffer to the destination stream.
                destination.Write(buffer, 0, bytesRead);
            }
        }


        /// <summary>
        /// Extracts a .tar file to the specified destination folder.
        /// </summary>
        /// <param name="tarFileName">The path to the .tar file to extract.</param>
        /// <param name="destinationFolder">The path to the folder where the extracted files should be placed.</param>
        /// <returns>True if extraction was successful, false otherwise.</returns>
        private static void ExtractTar(string tarFileName, string destinationFolder)
        {
            Console.WriteLine($@"Extracting {tarFileName} to {destinationFolder}...");
    
            // Store the current working directory so we can change it temporarily.
            var currentDirectory = Directory.GetCurrentDirectory();

            // Open the .tar file as a stream.
            using (Stream tarredData = File.OpenRead(tarFileName))
            {
                // Create the destination folder if it doesn't exist.
                Directory.CreateDirectory(destinationFolder);

                // Change the current working directory to the destination folder.
                Directory.SetCurrentDirectory(destinationFolder);

                // Extract the .tar file to the current directory.
                TarFile.ExtractToDirectory(tarredData, ".", true);
            }

            // Reset the current working directory to its original value.
            Directory.SetCurrentDirectory(currentDirectory);
        }

    }
}