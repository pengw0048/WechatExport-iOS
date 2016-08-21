using System;
using System.Collections.Generic;


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


    class iPhoneApp
    {
        public string Key;
        //public string DisplayName;          // CFBundleDisplayName
        //public string Name;                 // CFBundleName
        public string Identifier;           // CFBundleIdentifier
        public string Container;            // le chemin d'install sur l'iPhone
        public List<String> Files;
        public long FilesLength;            // taille totale des fichiers
    }


    class iPhoneFile
    {
        public string Key;
        public string Domain;
        public long FileLength;
        public DateTime ModificationTime;   // initialement: string
        public string Path;                 // information issue de .mdinfo
    }




    class iPhoneIPA
    {
        public string softwareVersionBundleId;      // identifier
        public string itemName;                     // name of the app
        public string fileName;                     // .ipa archive name
        public uint totalSize = 0;                  // uncompressed size
    }
}
