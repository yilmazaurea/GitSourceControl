namespace GitSourceControlTool
{
    public class SyncToolParameters
    {
        public string ServerUri { get; set; }

        public string FolderPath { get; set; }

        public string User { get; set; }

        public string Password { get; set; }

        public string Comment { get; set; }

        public string SourceControl { get; set; }
    }
}