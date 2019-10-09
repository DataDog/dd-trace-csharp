using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Datadog.Trace.TestHelpers;
using Newtonsoft.Json;
using Directory = System.IO.Directory;

namespace UpdateVendors
{
    public class Program
    {
        private static readonly string CurrentDirectory = Environment.CurrentDirectory;
        private static readonly string DownloadDirectory = Path.Combine(CurrentDirectory, "downloads");
        private static string _vendorProjectDirectory;

        public static void Main(string[] args)
        {
            InitializeCleanDirectory(DownloadDirectory);
            var solutionDirectory = EnvironmentHelper.GetSolutionDirectory();
            _vendorProjectDirectory = Path.Combine(solutionDirectory, "src", "Datadog.Trace.Vendors");

            UpdateVendor(
                libraryName: "Serilog",
                masterBranchDownload: "https://github.com/serilog/serilog/archive/master.zip",
                latestCommitUrl: "https://api.github.com/repos/serilog/serilog/commits/master",
                pathToSrc: new[] { "serilog-master", "src", "Serilog" },
                transform: TransformSerilog);

            UpdateVendor(
                libraryName: "Serilog.Sinks.File",
                masterBranchDownload: "https://github.com/serilog/serilog-sinks-file/archive/master.zip",
                latestCommitUrl: "https://api.github.com/repos/serilog/serilog-sinks-file/commits/master",
                pathToSrc: new[] { "serilog-sinks-file-master", "src", "Serilog.Sinks.File" },
                transform: TransformSerilog);
        }

        private static void TransformSerilog(string filePath)
        {
            var extension = Path.GetExtension(filePath);

            if (extension == ".cs")
            {
                RewriteFileWithTransform(
                    filePath,
                    content =>
                    {
                        content = content.Replace("using Serilog", "using Datadog.Trace.Vendors.Serilog");
                        content = content.Replace("namespace Serilog", "namespace Datadog.Trace.Vendors.Serilog");
                        content = content.Replace("class NullSink ", "public class NullSink ");
                        return content;
                    });
            }
        }

        private static void UpdateVendor(
            string libraryName,
            string masterBranchDownload,
            string latestCommitUrl,
            string[] pathToSrc,
            Action<string> transform = null)
        {
            Console.WriteLine($"Starting {libraryName} upgrade.");

            var zipLocation = Path.Combine(DownloadDirectory, $"{libraryName}.zip");
            var extractLocation = Path.Combine(DownloadDirectory, $"{libraryName}");

            using (var repoDownloadClient = new WebClient())
            {
                repoDownloadClient.DownloadFile(masterBranchDownload, zipLocation);
            }

            Console.WriteLine($"Downloaded {libraryName} upgrade.");

            ZipFile.ExtractToDirectory(zipLocation, extractLocation);

            Console.WriteLine($"Unzipped {libraryName} upgrade.");

            var sourceLocation = extractLocation;

            foreach (var pathPart in pathToSrc)
            {
                sourceLocation = Path.Combine(sourceLocation, pathPart);
            }

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
                    transform(file);
                }

                Console.WriteLine($"Finished transforms on files for {libraryName}.");
            }

            // Add information about the commit we are downloading
            var githubToken = Environment.GetEnvironmentVariable("DD_VENDOR_TOOL_TOKEN");
            if (githubToken == null)
            {
                throw new ArgumentException("You must specify a valid OAuth token for the github API.");
            }

            string commitInformation;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("AppName", "1.0"));
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Token", githubToken);
                var responseTask = client.GetStringAsync(latestCommitUrl);
                responseTask.Wait();
                commitInformation = FormatJson(responseTask.Result);
            }

            var commitJsonPath = Path.Combine(sourceLocation, "commit-info.json");
            File.WriteAllText(commitJsonPath, commitInformation);

            // Move it all to the vendors directory
            var vendorFinalPath = Path.Combine(_vendorProjectDirectory, libraryName);
            SafeDeleteDirectory(vendorFinalPath);
            Directory.Move(sourceLocation, vendorFinalPath);
            Console.WriteLine($"Copying source of {libraryName} to vendor project.");

            Console.WriteLine($"Finished {libraryName} upgrade.");
        }

        private static void RewriteFileWithTransform(string filePath, Func<string, string> transform)
        {
            var fileContent = File.ReadAllText(filePath);
            fileContent = transform(fileContent);
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

        private static string FormatJson(string json)
        {
            dynamic parsedJson = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        }
    }
}
