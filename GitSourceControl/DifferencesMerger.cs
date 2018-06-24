using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GitSourceControl.Interfaces;
using GitSourceControl.SourceControlProvider;

namespace GitSourceControl
{
    public class DifferencesMerger
    {
        private readonly ISourceControlProvider _sourceControlProvider;
        private readonly ILogger _logger;
        private List<string> _filesToAddInSourceControl;
        private List<string> _filesToUpdateInSourceControl;

        public DifferencesMerger(ISourceControlProvider sourceControlProvider, ILogger logger)
        {
            _sourceControlProvider = sourceControlProvider;
            _logger = logger;
        }

        public void MergeDifferencesInFolder(string folderToMerge, string checkInMessage, StringBuilder checkinInfoStringBuilder)
        {
            ClearCurrentFiles();

            string[] files = Directory.GetFiles(folderToMerge, "*.*", SearchOption.AllDirectories);

            _logger.LogInfo("Comparing files with source control...");

            foreach (string file in files)
            {
                if (_sourceControlProvider.IsFileUnderSourceControl(file))
                {
                    _filesToUpdateInSourceControl.Add(file);
                }
                else
                {
                    _filesToAddInSourceControl.Add(file);
                }
            }

            checkinInfoStringBuilder.AppendLine($"<p> Files to update in source control: {string.Join("; ", _filesToUpdateInSourceControl)}. </p>");
            checkinInfoStringBuilder.AppendLine($"<p> Files to add to source control: {string.Join("; ", _filesToAddInSourceControl)}. </p>");
            checkinInfoStringBuilder.AppendLine($"<p> All files in forlder to merge: {string.Join("; ", files)}. </p>");

            CheckInCurrentChanges(checkInMessage);
        }

        private void ClearCurrentFiles()
        {
            _filesToAddInSourceControl = new List<string>();
            _filesToUpdateInSourceControl = new List<string>();
        }

        private bool DoesFileHaveLocalChanges(string file)
        {
            DateTime lastModifiedUtcDate = File.GetLastWriteTimeUtc(file);
            DateTime lastCheckedInUtcDate = _sourceControlProvider.GetLastCheckedInUtcDate(file);

            return lastModifiedUtcDate > lastCheckedInUtcDate;
        }

        private void CheckInCurrentChanges(string checkInMessage)
        {
            if (_filesToUpdateInSourceControl.Any())
            {
                _sourceControlProvider.CheckOutFiles(_filesToUpdateInSourceControl.ToArray());
            }

            if (_filesToAddInSourceControl.Any())
            {
                _sourceControlProvider.AddFilesToSourceControl(_filesToAddInSourceControl.ToArray());
            }

            _sourceControlProvider.DeleteServerItemsWhichHaveNoLocalItems();

            _sourceControlProvider.CheckInCurrnetChangesOverwriteConflicts(checkInMessage);
        }
    }
}