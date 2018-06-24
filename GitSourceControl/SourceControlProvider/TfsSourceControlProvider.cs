using System;
using System.IO;
using System.Linq;
using System.Net;
using GitSourceControl.Interfaces;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace GitSourceControl.SourceControlProvider
{
    public class TfsSourceControlProvider : ISourceControlProvider
    {
        private readonly ILogger _logger;
        private string _localPathToFolder;
        private readonly VersionControlServer _versionControlServer;
        private Workspace _currentWorkspace;

        public TfsSourceControlProvider(string tfsServerUri, string userName, string password, ILogger logger)
        {
            _logger = logger;
            Uri url = new Uri(tfsServerUri);

            NetworkCredential nc = new NetworkCredential(userName, password);

            var coll = new TfsTeamProjectCollection(url, nc);

            coll.EnsureAuthenticated();

            _versionControlServer = coll.GetService<VersionControlServer>();
            _versionControlServer.NonFatalError += OnNonFatalError;
            Microsoft.TeamFoundation.VersionControl.Client.Workstation.Current.EnsureUpdateWorkspaceInfoCache(_versionControlServer, userName);
        }

        public void LoadWorkspace(string pathToLocalFileOrFolder)
        {
            if (_currentWorkspace == null)
            {
                _localPathToFolder = pathToLocalFileOrFolder;
                _currentWorkspace = _versionControlServer.GetWorkspace(pathToLocalFileOrFolder);
            }
        }

        public void CheckOutFiles(string[] localFilesPathes)
        {
            _currentWorkspace.PendEdit(localFilesPathes);
        }

        public void AddFilesToSourceControl(string[] localFilesPathes)
        {
            _currentWorkspace.PendAdd(localFilesPathes);
        }

        public void DeleteServerItemsWhichHaveNoLocalItems()
        {
            string serverFolder = _currentWorkspace.GetServerItemForLocalItem(_localPathToFolder);

            ItemSet serverItems = _versionControlServer.GetItems(serverFolder, RecursionType.Full);

            foreach (Item item in serverItems.Items.Where(item => item.ItemType == ItemType.File))
            {
                string localItemPath = _currentWorkspace.GetLocalItemForServerItem(item.ServerItem);

                if (!File.Exists(localItemPath))
                {
                    _currentWorkspace.PendDelete(localItemPath);
                }
            }
        }

        public void GetLatestVersion(string localPath)
        {
            string[] st = { localPath };

            _currentWorkspace.Get(st, VersionSpec.Latest, RecursionType.Full, GetOptions.GetAll);
        }

        public bool IsFileUnderSourceControl(string pathToLocalFile)
        {
            bool doesExistOnserver = false;
            bool isMapped = _currentWorkspace.IsLocalPathMapped(pathToLocalFile);

            if (isMapped)
            {
                string serverItem = _currentWorkspace.GetServerItemForLocalItem(pathToLocalFile);
                doesExistOnserver = _versionControlServer.ServerItemExists(serverItem, ItemType.Any);
            }

            return isMapped && doesExistOnserver;
        }

        public DateTime GetLastCheckedInUtcDate(string pathToLocalFile)
        {
            string serverItem = _currentWorkspace.GetServerItemForLocalItem(pathToLocalFile);
            Item item = _versionControlServer.GetItem(serverItem, VersionSpec.Latest);

            return item.CheckinDate.ToUniversalTime();
        }

        public void CheckInCurrnetChangesOverwriteConflicts(string checkInComment)
        {
            PendingChange[] pendingChanges = _currentWorkspace.GetPendingChanges(_localPathToFolder, RecursionType.Full);

            if (pendingChanges.Length > 0)
            {
                LogMessage("Found pending changes", false);

                string[] changesFiles = pendingChanges.Select(pC => pC.LocalItem).ToArray();

                Conflict[] conflicts = _currentWorkspace.QueryConflicts(changesFiles, true);

                ResolveConflictsIfExist(conflicts);

                LogMessage("Checking in pending changes", false);

                _currentWorkspace.CheckIn(pendingChanges, checkInComment);
            }
        }

        private void OnNonFatalError(object sender, ExceptionEventArgs e)
        {
            if (e.Exception != null)
            {
                _logger.LogError(e.Exception.Message);
            }
        }

        private void ResolveConflictsIfExist(Conflict[] conflicts)
        {
            foreach (Conflict conflict in conflicts)
            {
                LogMessage(
                        $"{conflict.LocalPath} - conflict with Source Control version. Overriding with local file.",
                        false
                    );

                conflict.Resolution = Resolution.AcceptYours;
                _currentWorkspace.ResolveConflict(conflict);
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