using System;

namespace iphonebackupbrowser
{
    class IPhoneBackup
    {
        public string DeviceName;
        public string DisplayName;
        public DateTime LastBackupDate;     // originally a string

        public string path;                 // backup path

        public override string ToString()
        {
            string str = DisplayName + " (" + LastBackupDate + ")";
            if (custom) str = str + " *";
            return str;
        }

        public bool custom = false;         // backup loaded from a custom directory
    }
}
