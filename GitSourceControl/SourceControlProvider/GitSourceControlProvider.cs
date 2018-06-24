using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GitSourceControl.Interfaces;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using static LibGit2Sharp.FileStatus;
using MergeOptions = LibGit2Sharp.MergeOptions;

namespace GitSourceControl.SourceControlProvider
{
    public class GitCredentials
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string ClonePath { get; set; }
        public string GitServerUri { get; set; }
    }

    public class GitSourceControlProvider : ISourceControlProvider
    {
        private readonly GitCredentials _credentials;
        private readonly ILogger _logger;
        private string _currentRepository;

        // ok - tested
        public GitSourceControlProvider(string gitServerUri, string userName, string password, ILogger logger)
        {
            _logger = logger;
            _credentials = new GitCredentials();
            _credentials.GitServerUri = gitServerUri;
            _credentials.Username = userName;
            _credentials.Password = password;
        }

        // ok - tested
        public void InitializeMasterBranch()
        {
            using (var repo = new Repository(_currentRepository))
            {
                if (!repo.Branches.Any())
                {
                    File.WriteAllText(Path.Combine(repo.Info.WorkingDirectory, "file.txt"), "");
                    repo.Stage("file.txt");
                    Signature author = new Signature("name", "mail", DateTime.Now);
                    Signature committer = author;
                    Commit commit = repo.Commit("message", author, committer);
                    repo.Index.Remove("file.txt");
                }
            }
        }

        // ok - tested
        public void LoadWorkspace(string pathToLocalFileOrFolder)
        {
            if (_currentRepository == null)
            {
                _currentRepository = pathToLocalFileOrFolder;
                Repository.Init(_currentRepository);
            }
        }

        // ok - tested
        public void CheckOutFiles(string[] localFilesPathes)
        {
            using (var repo = new Repository(_currentRepository))
            {
                var branch = repo.Branches["origin/master"];
                repo.Checkout(branch, new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });
            }
        }

        // ok - tested
        public void AddFilesToSourceControl(string[] localFilesPathes)
        {
            using (var repo = new Repository(_currentRepository))
            {
                foreach (string filePath in localFilesPathes)
                {
                    // Stage the file
                    repo.Index.Add(filePath);
                }                
            }
        }

        // not available with libgit2sharp
        public void DeleteServerItemsWhichHaveNoLocalItems(){}

        // ok - tested
        public void GetLatestVersion(string localPath)
        {
            using (var repo = new Repository(_currentRepository))
            {
                Branch trackedBranch = repo.Branches["master"];

                // tracked branch name may differ, but accepted as master by default.
                repo.Branches.Update(trackedBranch, b => b.TrackedBranch = "refs/remotes/origin/master");

                PullOptions options = new PullOptions();
                    options.FetchOptions = new FetchOptions();
                    options.FetchOptions.CredentialsProvider = new CredentialsHandler(
                        (url, usernameFromUrl, types) =>
                            new UsernamePasswordCredentials()
                            {
                                Username = _credentials.Username,
                                Password = _credentials.Password
                            });

                    repo.Network.Pull(new LibGit2Sharp.Signature(_credentials.Username, "cagdas@umay.com", new DateTimeOffset(DateTime.Now)), options);
            }
        }

        // ok - tested
        public bool IsFileUnderSourceControl(string pathToLocalFile)
        {
            bool fileExists = false;
            var repo = new Repository(_currentRepository);

            foreach (var file in repo.Index)
            {
                if (file.Path == Path.GetFileName(pathToLocalFile))
                    fileExists = true;
            }

            return fileExists;
        }

        // ok - tested
        public DateTime GetLastCheckedInUtcDate(string pathToLocalFile)
        {
            DateTime ret = new DateTime();
            using (var repo = new Repository(_currentRepository))
            {         
                foreach (Commit c in repo.Commits.Take(1))
                {
                    ret = c.Author.When.DateTime;
                }
            }

            return ret;
        }

        // ok - tested
        public void CheckInCurrnetChangesOverwriteConflicts(string checkInComment)
        {
            using (var repo = new Repository(_currentRepository))
            {
                Signature author = new Signature("Çağdaş", "cagdas@umay.com", DateTime.Now);
                Signature committer = author;

                Commit commit = repo.Commit(checkInComment, author, committer);
                Remote remote = repo.Network.Remotes.Count() != 0 ? repo.Network.Remotes["origin"] : repo.Network.Remotes.Add("origin", _credentials.GitServerUri);

                var options = new PushOptions { CredentialsProvider = (url, user, cred) => new UsernamePasswordCredentials { Username = _credentials.Username, Password = _credentials.Password } };
                repo.Network.Push(remote, @"refs/heads/master", options);
            }
        }

        private void OnNonFatalError(object sender, ExceptionEventArgs e)
        {
            if (e.Exception != null)
            {
                Logger.LogError(e.Exception.Message);
            }
        }

        private void LogMessage(string message, bool isError)
        {
            if (_logger != null)
            {
                if (isError)
                {
                    _logger.LogError(message);
                }
                else
                {
                    _logger.LogInfo(message);
                }
            }
        }

    }
}
