using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace UpdateVendors
{
    public class Program
    {
        private const string AutoGeneratedMessage = @"//------------------------------------------------------------------------------
// <auto-generated />
// This file was automatically generated by the UpdateVendors tool.
//------------------------------------------------------------------------------
";

        private static readonly string DownloadDirectory = Path.Combine(Environment.CurrentDirectory, "downloads");
        private static string _vendorProjectDirectory;

        public static void Main()
        {
            InitializeCleanDirectory(DownloadDirectory);
            var solutionDirectory = GetSolutionDirectory();
            _vendorProjectDirectory = Path.Combine(solutionDirectory, "src", "Datadog.Trace", "Vendors");

            UpdateVendor(
                libraryName: "Serilog",
                downloadUrl: "https://github.com/serilog/serilog/archive/v2.8.0.zip",
                pathToSrc: new[] { "serilog-2.8.0", "src", "Serilog" },
                transform: filePath => RewriteCsFileWithStandardTransform(filePath, originalNamespace: "Serilog"));

            UpdateVendor(
                libraryName: "Serilog.Sinks.File",
                downloadUrl: "https://github.com/serilog/serilog-sinks-file/archive/v4.0.0.zip",
                pathToSrc: new[] { "serilog-sinks-file-4.0.0", "src", "Serilog.Sinks.File" },
                transform: filePath => RewriteCsFileWithStandardTransform(filePath, originalNamespace: "Serilog"));

            UpdateVendor(
                libraryName: "StatsdClient",
                downloadUrl: "https://github.com/DataDog/dogstatsd-csharp-client/archive/6.0.0.zip",
                pathToSrc: new[] { "dogstatsd-csharp-client-6.0.0", "src", "StatsdClient" },
                transform: filePath => RewriteCsFileWithStandardTransform(filePath, originalNamespace: "StatsdClient"));

            UpdateVendor(
                libraryName: "MessagePack",
                downloadUrl: "https://github.com/neuecc/MessagePack-CSharp/archive/v1.9.3.zip",
                pathToSrc: new[] { "MessagePack-CSharp-1.9.3", "src", "MessagePack" },
                transform: filePath => RewriteCsFileWithStandardTransform(filePath, originalNamespace: "MessagePack"));

            UpdateVendor(
                libraryName: "Newtonsoft.Json",
                downloadUrl: "https://github.com/JamesNK/Newtonsoft.Json/archive/12.0.1.zip",
                pathToSrc: new[] { "Newtonsoft.Json-12.0.1", "src", "Newtonsoft.Json" },
                transform: filePath => RewriteCsFileWithStandardTransform(filePath, originalNamespace: "Newtonsoft.Json"));
        }

        private static void RewriteCsFileWithStandardTransform(string filePath, string originalNamespace, Func<string, string, string> extraTransform = null)
        {
            if (string.Equals(Path.GetExtension(filePath), ".cs", StringComparison.OrdinalIgnoreCase))
            {
                RewriteFileWithTransform(
                    filePath,
                    content =>
                    {
                        // Disable analyzer
                        var builder = new StringBuilder(AutoGeneratedMessage, content.Length * 2);

                        builder.Append(content);

                        // Special Newtonsoft.Json processing
                        if (originalNamespace.Equals("Newtonsoft.Json"))
                        {
                            builder.Replace($"using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs", "using ErrorEventArgs = Datadog.Trace.Vendors.Newtonsoft.Json.Serialization.ErrorEventArgs");

                            if (content.Contains("using Newtonsoft.Json.Serialization;"))
                            {
                                builder.Replace($"Func<", $"System.Func<");
                                builder.Replace($"Action<", $"System.Action<");
                            }
                        }

                        // Prevent namespace conflicts
                        builder.Replace($"using {originalNamespace}", $"using Datadog.Trace.Vendors.{originalNamespace}");
                        builder.Replace($"namespace {originalNamespace}", $"namespace Datadog.Trace.Vendors.{originalNamespace}");
                        builder.Replace($"[CLSCompliant(false)]", $"// [CLSCompliant(false)]");

                        // Don't expose anything we don't intend to
                        // by replacing all "public" access modifiers with "internal"
                        return Regex.Replace(
                            builder.ToString(),
                            @"public(\s+((abstract|sealed|static)\s+)?(partial\s+)?(class|readonly\s+struct|struct|interface|enum|delegate))",
                            match => $"internal{match.Groups[1]}");
                    });
            }
        }

        private static void UpdateVendor(
            string libraryName,
            string downloadUrl,
            string[] pathToSrc,
            Action<string> transform = null)
        {
            Console.WriteLine($"Starting {libraryName} upgrade.");

            var zipLocation = Path.Combine(DownloadDirectory, $"{libraryName}.zip");
            var extractLocation = Path.Combine(DownloadDirectory, $"{libraryName}");
            var vendorFinalPath = Path.Combine(_vendorProjectDirectory, libraryName);
            var sourceUrlLocation = Path.Combine(vendorFinalPath, "_last_downloaded_source_url.txt");

            // Ensure the url has changed, or don't bother upgrading
            var currentSource = File.ReadAllText(sourceUrlLocation);
            if (currentSource.Equals(downloadUrl, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"No updates to be made for {libraryName}.");
                return;
            }

            using (var repoDownloadClient = new WebClient())
            {
                repoDownloadClient.DownloadFile(downloadUrl, zipLocation);
            }

            Console.WriteLine($"Downloaded {libraryName} upgrade.");

            ZipFile.ExtractToDirectory(zipLocation, extractLocation);

            Console.WriteLine($"Unzipped {libraryName} upgrade.");

            var sourceLocation = Path.Combine(pathToSrc.Prepend(extractLocation).ToArray());
            var projFile = Path.Combine(sourceLocation, $"{libraryName}.csproj");

            // Rename the proj file to a txt for reference
            File.Copy(projFile, projFile + ".txt");
            File.Delete(projFile);
            Console.WriteLine($"Renamed {libraryName} project file.");

            // Delete the assembly properties
            var assemblyPropertiesFolder = Path.Combine(sourceLocation, @"Properties");
            SafeDeleteDirectory(assemblyPropertiesFolder);
            Console.WriteLine($"Deleted {libraryName} assembly properties file.");

            if (transform != null)
            {
                Console.WriteLine($"Running transforms on files for {libraryName}.");

                var files = Directory.GetFiles(
                    sourceLocation,
                    "*.*",
                    SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    if (ShouldDropFile(file))
                    {
                        File.Delete(file);
                    }
                    else
                    {
                        transform(file);
                    }
                }

                Console.WriteLine($"Finished transforms on files for {libraryName}.");
            }

            // Move it all to the vendors directory
            Console.WriteLine($"Copying source of {libraryName} to vendor project.");
            SafeDeleteDirectory(vendorFinalPath);
            Directory.Move(sourceLocation, vendorFinalPath);
            File.WriteAllText(sourceUrlLocation, downloadUrl);
            Console.WriteLine($"Finished {libraryName} upgrade.");
        }

        private static bool ShouldDropFile(string filePath)
        {
            var drops = new List<string>()
            {
                // No active exclusions
            };

            foreach (var drop in drops)
            {
                if (filePath.Contains(drop, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void RewriteFileWithTransform(string filePath, Func<string, string> transform)
        {
            var fileContent = File.ReadAllText(filePath);
            fileContent = transform(fileContent);
            // Normalize text to use CRLF line endings so we have deterministic results
            fileContent = fileContent.Replace("\r\n", "\n").Replace("\n", "\r\n");
            File.WriteAllText(
                filePath,
                fileContent,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        private static void InitializeCleanDirectory(string directoryPath)
        {
            SafeDeleteDirectory(directoryPath);
            Directory.CreateDirectory(directoryPath);
        }

        private static void SafeDeleteDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }

        private static string GetSolutionDirectory()
        {
            var startDirectory = Environment.CurrentDirectory;
            var currentDirectory = Directory.GetParent(startDirectory);
            const string searchItem = @"Datadog.Trace.sln";

            while (true)
            {
                var slnFile = currentDirectory.GetFiles(searchItem).SingleOrDefault();

                if (slnFile != null)
                {
                    break;
                }

                currentDirectory = currentDirectory.Parent;

                if (currentDirectory == null || !currentDirectory.Exists)
                {
                    throw new Exception($"Unable to find solution directory from: {startDirectory}");
                }
            }

            return currentDirectory.FullName;
        }
    }
}
