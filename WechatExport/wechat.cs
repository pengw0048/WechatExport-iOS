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

        public bool OpenMMSqlite(string userBase, out SQLiteConnection conn)
        {
            bool succ = false;
            conn = null;
            try
            {
                conn = new SQLiteConnection();
                conn.ConnectionString = "data source=" + GetBackupFilePath(Path.Combine(userBase, "DB", "MM.sqlite")) + ";version=3";
                conn.Open();
                succ = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            return succ;
        }

        public bool GetUserBasics(string uid, string userBase, out string userid, out string username)
        {
            userid = uid;
            username = "我";
            bool succ = false;
            try
            {
                var pr = new BinaryPlistReader();
                var mmsetting = GetBackupFilePath(Path.Combine(userBase, "mmsetting.archive"));
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

        public bool GetFriends(SQLiteConnection conn, out List<Friend> friends)
        {
            bool succ = false;
            friends = new List<Friend>();
            try
            {
                using (var cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = "SELECT Friend.UsrName,NickName,Sex,ConRemark,ConChatRoomMem,ConStrRes2 FROM Friend JOIN Friend_Ext ON Friend.UsrName=Friend_Ext.UsrName";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            try
                            {
                                var friend = new Friend();
                                friend.UsrName = reader.GetString(0);
                                friend.NickName = reader.GetString(1);
                                friend.Sex = reader.GetInt32(2);
                                friend.ConRemark = reader.GetString(3);
                                friend.ConChatRoomMem = reader.GetString(4);
                                friend.ConStrRes2 = reader.GetString(5);
                                friend.ProcessFields();
                                friends.Add(friend);
                            }
                            catch (Exception) { }
                    }
                }
                succ = true;
            }
            catch (Exception) { }
            return succ;
        }

        public bool GetChatSessions(SQLiteConnection conn, out List<string> sessions)
        {
            bool succ = false;
            sessions = new List<string>();
            try
            {
                using(var cmd=new SQLiteCommand(conn))
                {
                    cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
                    using(var reader = cmd.ExecuteReader())
                    {
                        while(reader.Read())
                            try
                            {
                                var name = reader.GetString(0);
                                var match = Regex.Match(name, @"^Chat_[0-9a-f]{32}$");
                                if (match.Success) sessions.Add(match.Groups[1].Value);
                            }
                            catch (Exception) { }
                    }
                }
                succ = true;
            }
            catch (Exception) { }
            return succ;
        }

        public string GetBackupFilePath(string vpath)
        {
            vpath = vpath.Replace('\\', '/');
            if (!fileDict.ContainsKey(vpath)) return null;
            var file = fileDict[vpath];
            var ext = "";
            return Path.Combine(currentBackup.path, file.Key + ext);
        }

        public void BuildFilesDictionary(iPhoneApp app)
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

        public List<string> FindUIDs()
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

    public class Friend
    {
        public string UsrName;
        public string NickName;
        public int Sex;
        public string ConRemark;
        public string ConChatRoomMem;
        public string ConStrRes2;

        public string alias;
        public void ProcessFields()
        {
            var match = Regex.Match(ConStrRes2, @"<alias>(.*?)<\/alias>");
            alias = match.Success ? match.Groups[1].Value : "";
        }
    }
}
