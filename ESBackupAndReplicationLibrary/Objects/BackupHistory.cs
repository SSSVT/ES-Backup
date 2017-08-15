using System;

namespace ESBackupAndReplication.Objects
{
    public class BackupHistory
    {
        public BackupHistory(string source, string destination, DateTime UtcStart, DateTime UtcEnd, bool compressed)
        {
            this.Source = source;
            this.Destination = destination;
            this.UTCStart = UtcStart;
            this.UTCEnd = UtcEnd;
            this.Compressed = compressed;
        }

        public string Source { get; set; }
        public string Destination { get; set; }
        public DateTime UTCStart { get; set; }
        public DateTime UTCEnd { get; set; }
        public bool Compressed { get; set; }
    }
}
