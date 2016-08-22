using System;
using System.Collections.Generic;
using System.Windows.Forms;
using iphonebackupbrowser;
using System.IO;
using mbdbdump;
using System.Runtime.Serialization.Plists;
using System.Collections;
using System.Text.RegularExpressions;
using System.Linq;
using System.Data.SQLite;

namespace WechatExport
{
    class WeChatInterface
    {
        public Dictionary<string, iPhoneFile> fileDict = null;
        private iPhoneBackup currentBackup;
        private List<mbdb.MBFileRecord> files92;
        public WeChatInterface(iPhoneBackup currentBackup, List<mbdb.MBFileRecord> files92)
        {
            this.currentBackup = currentBackup;
            this.files92 = files92;
        }

        public bool openMMSqlite(string userBase, out SQLiteConnection conn)
        {
            bool succ = false;
            conn = null;
            try
            {
                conn = new SQLiteConnection();
                conn.ConnectionString = "data source=" + getBackupFilePath(Path.Combine(userBase, "DB", "MM.sqlite")) + ";version=3";
                conn.Open();
                succ = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            return succ;
        }

        public bool getUserBasics(string uid, string userBase, out string userid, out string username)
        {
            userid = uid;
            username = "我";
            bool succ = false;
            try
            {
                var pr = new BinaryPlistReader();
                var mmsetting = getBackupFilePath(Path.Combine(userBase, "mmsetting.archive"));
                using (var sw = new FileStream(mmsetting, FileMode.Open))
                {
                    var dd = pr.ReadObject(sw);
                    var objs = dd["$objects"] as object[];
                    if (objs[2].GetType() == typeof(string) && objs[3].GetType() == typeof(string))
                    {
                        var tuserid = objs[2] as string;
                        var tusername = objs[3] as string;
                        if (tuserid != "" && tusername != "")
                        {
                            userid = tuserid;
                            username = tusername;
                            succ = true;
                        }
                    }
                }
            }
            catch (Exception) { }
            return succ;
        }

        public string getBackupFilePath(string vpath)
        {
            vpath = vpath.Replace('\\', '/');
            if (!fileDict.ContainsKey(vpath)) return null;
            var file = fileDict[vpath];
            var ext = "";
            return Path.Combine(currentBackup.path, file.Key + ext);
        }

        public void buildFilesDictionary(iPhoneApp app)
        {
            var dict = new Dictionary<string, iPhoneFile>();
            foreach (var f in app.Files)
            {
                var x = files92[int.Parse(f)];
                var ff = new iPhoneFile()
                {
                    Key = x.key,
                    Domain = x.Domain,
                    Path = x.Path,
                    ModificationTime = x.aTime,
                    FileLength = x.FileLength
                };
                dict.Add(x.Path, ff);
            }
            this.fileDict = dict;
        }

        public List<string> findUIDs()
        {
            var UIDs = new HashSet<string>();
            foreach (var filename in fileDict)
            {
                var match = Regex.Match(filename.Key, @"Documents\/([0-9a-f]{32})\/");
                if (match.Success) UIDs.Add(match.Groups[1].Value);
            }
            var zeros = new string('0', 32);
            if (UIDs.Contains(zeros)) UIDs.Remove(zeros);
            return UIDs.ToList();
        }

    }
}
