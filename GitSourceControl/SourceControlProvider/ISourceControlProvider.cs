using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitSourceControl.SourceControlProvider
{
    public interface ISourceControlProvider
    {
        void AddFilesToSourceControl(string[] localFilesPathes);

        void CheckInCurrnetChangesOverwriteConflicts(string checkInComment);

        void CheckOutFiles(string[] localFilesPathes);

        bool IsFileUnderSourceControl(string pathToLocalFile);

        DateTime GetLastCheckedInUtcDate(string pathToLocalFile);

        void GetLatestVersion(string localPath);

        void DeleteServerItemsWhichHaveNoLocalItems();

        void LoadWorkspace(string pathToLocalFileOrFolder);
    }
}
