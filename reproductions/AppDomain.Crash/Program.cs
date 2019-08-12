using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Threading;
using AppDomain.Instance;
using Datadog.Trace.TestHelpers;

namespace AppDomain.Crash
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting AppDomain Crash Test");

                var workers = new List<Thread>();
                var unloads = new List<Thread>();

                string commonFriendlyAppDomainName = "crash-dummy";
                int index = 1;

                var appPool = EnvironmentHelper.NonProfiledHelper(typeof(Program), "AppDomain.Crash", "reproductions");
                var appInstance = EnvironmentHelper.NonProfiledHelper(typeof(Program), "AppDomain.Instance", "reproduction-dependencies");

                var appPoolBin = appPool.GetSampleApplicationOutputDirectory();
                var instanceBin = appInstance.GetSampleApplicationOutputDirectory();

                var deployDirectory = Path.Combine(appPool.GetSampleProjectDirectory(), "ApplicationInstance", "AppDomain.Instance");

                var securityInfo = new Evidence();

                var currentAssembly = Assembly.GetExecutingAssembly();

                var instanceType = typeof(AppDomainInstanceProgram);
                var instanceName = instanceType.FullName;

                System.AppDomain previousDomain = null;
                AppDomainInstanceProgram previousInstance = null;

                var domainsToInstantiate = 3;

                while (domainsToInstantiate-- > 0)
                {
                    if (previousDomain != null)
                    {
                        var unloadTask = new Thread(
                            () =>
                            {
                                var domainToUnload = previousDomain;
                                var instanceToUnload = previousInstance;

                                while (true)
                                {
                                    if (instanceToUnload?.WorkerProgram != null)
                                    {
                                        lock (instanceToUnload.WorkerProgram.CallLock)
                                        {
                                            if (instanceToUnload.WorkerProgram.CurrentCallCount > 0)
                                            {
                                                instanceToUnload.WorkerProgram.DenyAllCalls = true;
                                                break;
                                            }
                                        }

                                        Thread.Sleep(100);
                                        continue;
                                    }

                                    break;
                                }

                                System.AppDomain.Unload(domainToUnload);
                            });

                        unloads.Add(unloadTask);

                        Console.WriteLine($"Beginning deploy over instance {index - 1}");
                        unloadTask.Start();
                    }

                    XCopy(instanceBin, deployDirectory);

                    var currentAppDomain =
                        System.AppDomain.CreateDomain(
                            friendlyName: commonFriendlyAppDomainName,
                            securityInfo: securityInfo,
                            appBasePath: deployDirectory,
                            appRelativeSearchPath: appPoolBin,
                            shadowCopyFiles: true);

                    Console.WriteLine($"Created AppDomain root for #{index} - {commonFriendlyAppDomainName}");

                    var instanceOfProgram =
                        currentAppDomain.CreateInstanceAndUnwrap(
                            instanceType.Assembly.FullName,
                            instanceName) as AppDomainInstanceProgram;

                    Console.WriteLine($"Created AppDomain instance for #{index} - {instanceName}");

                    var argsToPass = new string[] { commonFriendlyAppDomainName, index.ToString() };

                    Console.WriteLine($"Starting instance #{index} - {instanceName}");

                    var domainWorker = new Thread(
                        () =>
                        {
                            instanceOfProgram.Main(argsToPass);
                        });

                    domainWorker.Start();

                    workers.Add(domainWorker);

                    // Give the domain some time to enjoy life
                    while (instanceOfProgram?.WorkerProgram == null || instanceOfProgram.WorkerProgram.TotalCallCount < 3)
                    {
                        Thread.Sleep(3000);
                    }

                    previousDomain = currentAppDomain;
                    previousInstance = instanceOfProgram;
                    index++;
                }

                while (workers.Any(w => w.IsAlive))
                {
                    Thread.Sleep(2000);
                }

                Console.WriteLine("No crashes! All is well!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"We have encountered an exception, the smoke test fails: {ex.Message}");
                Console.Error.WriteLine(ex);
                return -10;
            }

            return 0;
        }

        private static void XCopy(string sourceDirectory, string targetDirectory)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "xcopy";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = "\"" + sourceDirectory + "\"" + " " + "\"" + targetDirectory + "\"" + @" /e /y /I";

            Process xCopy = null;

            try
            {
                xCopy = Process.Start(startInfo);
                xCopy.WaitForExit(10_000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"XCopy has failed: {ex.Message}");
                throw;
            }
            finally
            {
                xCopy?.Dispose();
            }
        }
    }
}
