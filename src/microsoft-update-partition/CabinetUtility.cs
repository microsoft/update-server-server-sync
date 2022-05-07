// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Compression
{
    /// <summary>
    /// Performs CAB compression and decompression. On Linux, it requires the cabextract; on windows it requires expand.exe
    /// </summary>
    public class CabinetUtility
    {
        /// <summary>
        /// Recompress the given bytes
        /// </summary>
        /// <param name="compressedData">Data to compress</param>
        /// <returns>Compressed data</returns>
        /// <exception cref="NotImplementedException">If not implemented on the current platform</exception>
        public static byte[] RecompressUnicodeData(byte[] compressedData)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return RecompressUnicodeDataWindows(compressedData);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return RecompressUnicodeDataLinux(compressedData);
            }
            else
            {
                throw new NotImplementedException("No decompressor available for the current platform");
            }
        }

        static byte[] RecompressUnicodeDataWindows(byte[] compressedData)
        {
            // We use temporary files to write the in-memory cabinet,
            // run expand on it then read the resulting file back in memory
            var cabTempFile = Path.GetTempFileName();
            var xmlTempFile = Path.GetTempFileName();

            var inMemoryStream = new MemoryStream();
            try
            {
                File.WriteAllBytes(cabTempFile, compressedData);

                var startInfo = new ProcessStartInfo("expand.exe", $"\"{cabTempFile}\" \"{xmlTempFile}\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var expandProcess = Process.Start(startInfo);
                expandProcess.WaitForExit();

                using var decompresedFile = File.OpenRead(xmlTempFile);
                using var recompressor = new GZipStream(inMemoryStream, CompressionLevel.Fastest, true);
                decompresedFile.CopyTo(recompressor);
            }
            catch (Exception)
            {
                inMemoryStream = null;
            }

            if (File.Exists(cabTempFile))
            {
                File.Delete(cabTempFile);
            }

            if (File.Exists(xmlTempFile))
            {
                File.Delete(xmlTempFile);
            }

            if (inMemoryStream != null)
            {
                return inMemoryStream.ToArray();
            }
            else
            {
                return null;
            }
        }

        static byte[] RecompressUnicodeDataLinux(byte[] compressedData)
        {
            // We use temporary files to write the in-memory cabinet,
            // Then run cabextract on it with --pipe output
            var cabTempFile = Path.GetTempFileName();

            var inMemoryStream = new MemoryStream();
            try
            {
                File.WriteAllBytes(cabTempFile, compressedData);

                var startInfo = new ProcessStartInfo("cabextract", $"--pipe \"{cabTempFile}\"")
                {
                    UseShellExecute = false,
                    // The decompressed text is Unicode
                    StandardOutputEncoding = Encoding.Unicode,
                    RedirectStandardOutput = true
                };
                var expandProcess = Process.Start(startInfo);

                // Read the decompressed data from the pipe
                var text = expandProcess.StandardOutput.ReadToEnd();
                expandProcess.WaitForExit();

                // Recompress the XML with GZIP as UTF8
                using var recompressor = new GZipStream(inMemoryStream, CompressionLevel.Fastest, true);
                recompressor.Write(Encoding.UTF8.GetBytes(text));

            }
            catch (Exception)
            {
                inMemoryStream = null;
            }

            if (File.Exists(cabTempFile))
            {
                File.Delete(cabTempFile);
            }

            if (inMemoryStream != null)
            {
                return inMemoryStream.ToArray();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Compress a list of files
        /// </summary>
        /// <param name="filePaths">Files to compress</param>
        /// <param name="outFile">Destination cab file</param>
        /// <returns>True on success, false otherwise</returns>
        public static void CompressFiles(List<string> filePaths, string outFile)
        {
            // When dealing with multiple files, we must use a directive file
            var directiveFile = outFile + ".directive";

            // Create the directive file
            File.WriteAllText(directiveFile, CreateMakeCabDirective(filePaths, outFile));

            int cabExitCode;
            try
            {
                var startInfo = new ProcessStartInfo("makecab.exe", string.Format("/f {0}", directiveFile));
                var expandProcess = Process.Start(startInfo);
                expandProcess.WaitForExit();

                cabExitCode = expandProcess.ExitCode;
            }
            catch (Exception)
            {
                cabExitCode = -1;
            }

            if (File.Exists(directiveFile))
            {
                File.Delete(directiveFile);
            }

            if (cabExitCode != 0)
            {
                throw new Exception("Failed to cab the result");
            }
        }

        /// <summary>
        /// Creates a directive file for compressing multiple files
        /// </summary>
        /// <param name="files">List of files to add to the directive file</param>
        /// <param name="outFile">Ouput file to set in the directive file</param>
        /// <returns></returns>
        private static string CreateMakeCabDirective(List<string> files, string outFile)
        {
            var textWriter = new System.IO.StringWriter();
            textWriter.WriteLine(".OPTION EXPLICIT");
            textWriter.WriteLine(".Set DiskDirectoryTemplate=");

            textWriter.WriteLine(".Set CabinetNameTemplate={0}", outFile);
            textWriter.WriteLine(".Set Cabinet=on");
            textWriter.WriteLine(".Set Compress=on");

            textWriter.WriteLine(".Set CabinetFileCountThreshold=0");
            textWriter.WriteLine(".Set FolderFileCountThreshold=0");
            textWriter.WriteLine(".Set FolderSizeThreshold=0");
            textWriter.WriteLine(".Set MaxCabinetSize=0");
            textWriter.WriteLine(".Set MaxDiskFileCount=0");
            textWriter.WriteLine(".Set MaxDiskSize=0");
            
            foreach(var file in files)
            {
                textWriter.WriteLine("\"{0}\"", file);
            }

            return textWriter.ToString();
        }
    }
}
