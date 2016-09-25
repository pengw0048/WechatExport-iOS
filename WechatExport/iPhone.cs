using System;


namespace iphonebackupbrowser
{

    //
    // backup information retrieved from Info.plist
    //
    class iPhoneBackup
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
        public int index;                   // index in the combobox control

        // delegate to sort backups (newer first)
        public static int SortByDate(iPhoneBackup a, iPhoneBackup b)
        {
            return b.LastBackupDate.CompareTo(a.LastBackupDate);
        }
    }
}
