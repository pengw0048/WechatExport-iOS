using System;

namespace iphonebackupbrowser
{
    public class IPhoneBackup
    {
        public string DeviceName;
        public string DisplayName;
        public DateTime LastBackupDate;
        public string path;                 // backup path
        public bool custom = false;         // backup loaded from a custom directory

        public override string ToString()
        {
            string str = DisplayName + " (" + LastBackupDate + ")";
            if (custom) str = str + " *";
            return str;
        }
    }
}
