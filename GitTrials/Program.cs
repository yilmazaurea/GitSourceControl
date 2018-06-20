using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;


namespace GitTrials
{
    class GitCredentials
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string RepoPath { get; set; }
        public string ClonePath { get; set; }
        public string RemoteOrigin { get; set; }
    }

    class Program
    {

        static void Main(string[] args)
        {
            GitCredentials credentials = new GitCredentials();
            credentials.Username = "CagdasTrials";
            credentials.Password = "15feposea";
            credentials.RepoPath = @"D:\GitRepos\Repo3";
            credentials.ClonePath = @"D:\GitRepos\CloneRepo1";
            credentials.RemoteOrigin = "https://github.com/CagdasTrials/Git-Trials.git";

            //GitInit(credentials);
            //GitCommit(credentials);
            //GitPush(credentials);
            //GitClone(credentials);
            //GitFetch(credentials);
            GitPull(credentials);
        }

        public static void GitInit(GitCredentials credentials)
        {
            string rootedPath = Repository.Init(credentials.RepoPath);
        }

        public static void GitCommit(GitCredentials credentials)
        {
            using (var repo = new Repository(credentials.RepoPath))
            {
                // Write content to file system 
                var content = "I am a string in a txt file.";
                File.WriteAllText(Path.Combine(repo.Info.WorkingDirectory, "ExampleFile.txt"), content);

                // Stage the file
                repo.Stage("ExampleFile.txt");

                // Create the committer's signature and commit
                Signature author = new Signature("Çağdaş", "cagdas@umay.com", DateTime.Now);
                Signature committer = author;

                // Commit to the repository
                Commit commit = repo.Commit("This is a test commit", author, committer);
            }
        }

        public static void GitPush(GitCredentials credentials)
        {
            using (var repo = new Repository(credentials.RepoPath))
            {
                Remote remote = null;
                remote = repo.Network.Remotes.Count() != 0 ? repo.Network.Remotes["origin"] : repo.Network.Remotes.Add("origin", credentials.RemoteOrigin);

                var options = new PushOptions { CredentialsProvider = (url, user, cred) => new UsernamePasswordCredentials {Username = credentials.Username, Password = credentials .Password} };
                repo.Network.Push(remote, @"refs/heads/master", options);
            }
        }

        public static void GitClone(GitCredentials credentials)
        {
            var co = new CloneOptions();
            co.CredentialsProvider = (url, user, cred) => new UsernamePasswordCredentials { Username = credentials.Username, Password = credentials.Password };
            Repository.Clone(credentials.RemoteOrigin, credentials.ClonePath, co);
        }

        public static void GitFetch(GitCredentials credentials)
        {
            using (var repo = new Repository(credentials.RepoPath))
            {
                Remote remote = null;
                remote = repo.Network.Remotes.Count() != 0 ? repo.Network.Remotes["origin"] : repo.Network.Remotes.Add("origin", credentials.RemoteOrigin);
                repo.Network.Fetch(remote);
            }
        }

        // still on progress
        public static void GitPull(GitCredentials credentials)
        {
            using (var repo = new Repository(credentials.RepoPath))
            {
                var trackingBranch = repo.Branches["refs/remotes/origin/master"];
                if (trackingBranch.IsRemote)
                {
                    var branch = repo.CreateBranch("SomeLocalBranchName", trackingBranch.Tip);
                    repo.Branches.Update(branch, b => b.TrackedBranch = trackingBranch.CanonicalName);
                    repo.Checkout(branch, new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });
                }

                PullOptions options = new PullOptions();
                options.FetchOptions = new FetchOptions();
                options.FetchOptions.CredentialsProvider = (url, user, cred) =>
                    new UsernamePasswordCredentials { Username = credentials.Username, Password = credentials.Password };
                options.MergeOptions = new MergeOptions();
                options.MergeOptions.FastForwardStrategy = FastForwardStrategy.Default;

                repo.Network.Pull(new Signature("CagdasTrials", "cursedcoder@gmail.com", new DateTimeOffset(DateTime.Now)), options);
            }
        }



    }
}
