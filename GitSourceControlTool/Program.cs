using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitSourceControl;
using GitSourceControl.SourceControlProvider;
using Microsoft.TeamFoundation;
using GitSourceControl;
using GitSourceControl.Interfaces;

namespace GitSourceControlTool
{
    class Program
    {
        static void Main(string[] args)
        {
            ILogger logger = new ConsoleLogger();
            GitSourceControlProvider pro = new GitSourceControlProvider("https://github.com/CagdasTrials/Trials.git", "CagdasTrials","15feposea", logger);
            pro.LoadWorkspace(@"D:\GitRepos\RepoX");
            pro.DeleteServerItemsWhichHaveNoLocalItems();

            string xx = "";
        }

        static void Main2(string[] args)
        {
            var logger = new ConsoleLogger();

            if (args.Length < 1)
            {
                logger.LogError("Invalid arguments - at least path to folder is required");

                return;
            }

            if (args[0] == "?")
            {
                WriteHelpInfo();

                return;
            }

            SyncToolParameters parameters = GetParameters(args);

            if (string.IsNullOrEmpty(parameters.FolderPath))
            {
                logger.LogError("Folder parameter (f) should be specified.");

                return;
            }

            if (Directory.Exists(parameters.FolderPath) == false)
            {
                logger.LogError($"Folder {parameters.FolderPath} does not exist");

                return;
            }

            ISourceControlProvider provider;

            try
            {
                provider = new TfsSourceControlProvider(
                parameters.ServerUri,
                parameters.User,
                parameters.Password,
                logger
             );

            }
            catch (TeamFoundationServerUnauthorizedException)
            {
                logger.LogError("Could not connect to Source Control with providere credentials");

                return;
            }

            var merger = new DifferencesMerger(provider, logger);

            try
            {
                StringBuilder log = new StringBuilder();
                merger.MergeDifferencesInFolder(parameters.FolderPath, parameters.Comment, log);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
        }

        private static SyncToolParameters GetParameters(string[] arguments)
        {
            var result = GetParametersFromArguments(arguments);

            WriteDefaultValuesForEmptyParameters(result);

            return result;
        }

        private static SyncToolParameters GetParametersFromArguments(string[] arguments)
        {
            var result = new SyncToolParameters();

            for (int i = 0; i < arguments.Length; i++)
            {
                switch (arguments[i].ToLower())
                {
                    case "f":
                        i++;
                        result.FolderPath = arguments[i];
                        break;
                    case "u":
                        i++;
                        result.User = arguments[i];

                        if (!result.User.StartsWith("OLIVESOFT"))
                        {
                            result.User = ($"OLIVESOFT\\{result.User}");
                        }

                        break;
                    case "p":
                        i++;
                        result.Password = arguments[i];
                        break;
                    case "c":
                        i++;
                        result.Comment = arguments[i];
                        break;
                }
            }

            return result;
        }

        private static void WriteDefaultValuesForEmptyParameters(SyncToolParameters result)
        {
            if (string.IsNullOrEmpty(result.FolderPath))
            {
                return;
            }

            result.ServerUri = @"http://212.143.31.81:8080/tfs";

            if (string.IsNullOrEmpty(result.User) || string.IsNullOrEmpty(result.Password))
            {
                result.User = @"OLIVESOFT\Automation";
                result.Password = "4olive";
            }

            if (string.IsNullOrEmpty(result.Comment))
            {
                result.Comment = $"Source Control Update -{Path.GetFileName(result.FolderPath)} - {DateTime.Now.ToString("D", new CultureInfo("en-US"))}";
            }
        }

        private static void WriteHelpInfo()
        {
            Console.WriteLine("Allowed parameters:");
            Console.WriteLine("f - Folder. Required parameter. Full path to folder for synchronization with source control.");
            Console.WriteLine("u - User. Login name for account in source control. If not set - built-in account will be used.");
            Console.WriteLine("p - Password. Password for account in source control. If not set - built-in account will be used.");
            Console.WriteLine("c - Comment. This comment will be used as check-in message. If not set - default value will be used.");
            Console.WriteLine("---------------------------------");
            Console.WriteLine("Example: u Admin p 1111 f \"C:\\New Folder\\Sync\"");
        }
    }
}
