// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace Microsoft.UpdateServices.Compression
{
    /// <summary>
    /// Performs CAB compression and decompression. Works on Windows only
    /// </summary>
    internal class CabinetUtility
    {
        /// <summary>
        /// Decompress an in-memory cabinet archive
        /// </summary>
        /// <param name="compressedData">The compressed data to decompress</param>
        /// <returns>The decompressed data as string</returns>
        public static string DecompressData(byte[] compressedData)
        {
            // We use temporary files to write the in-memory cabinet,
            // run expand on it then read the resulting file back in memory
            var cabTempFile = Path.GetTempFileName();
            var xmlTempFile = Path.GetTempFileName();

            string decompressedString;

            try
            {
                File.WriteAllBytes(cabTempFile, compressedData);

                var startInfo = new ProcessStartInfo("expand.exe", $"{cabTempFile} {xmlTempFile}");
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                var expandProcess = Process.Start(startInfo);
                expandProcess.WaitForExit();

                decompressedString = File.ReadAllText(xmlTempFile, System.Text.Encoding.Unicode);
            }
            catch(Exception)
            {
                decompressedString = null;
            }

            if (File.Exists(cabTempFile))
            {
                File.Delete(cabTempFile);
            }

            if (File.Exists(xmlTempFile))
            {
                File.Delete(xmlTempFile);
            }

            return decompressedString;
        }

        /// <summary>
        /// Compress a list of files
        /// </summary>
        /// <param name="filePaths">Files to compress</param>
        /// <param name="outFile">Destination cab file</param>
        /// <returns>True on success, false otherwise</returns>
        public static bool CompressFiles(List<string> filePaths, string outFile)
        {
            // When dealing with multiple files, we must use a directive file
            var directiveFile = outFile + ".directive";

            // Create the directive file
            File.WriteAllText(directiveFile, CreateMakeCabDirective(filePaths, outFile));

            try
            {
                var startInfo = new ProcessStartInfo("makecab.exe", string.Format("/f {0}", directiveFile));
                var expandProcess = Process.Start(startInfo);
                expandProcess.WaitForExit();

                var exitCode = expandProcess.ExitCode;

                return exitCode == 0;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                if (File.Exists(directiveFile))
                {
                    File.Delete(directiveFile);
                }
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
