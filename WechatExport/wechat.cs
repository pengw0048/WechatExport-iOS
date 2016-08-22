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
using System.Text;

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

        public bool GetFriendsDict(SQLiteConnection conn, out Dictionary<string,Friend> friends)
        {
            List<Friend> _friends;
            friends = new Dictionary<string, Friend>();
            bool succ = GetFriends(conn, out _friends);
            if (succ)
            {
                foreach (var friend in _friends)
                {
                    friends.Add(friend.UsrName, friend);
                    friends.Add(CreateMD5(friend.UsrName), friend);
                    if (friend.alias != null && friend.alias != "" && !friends.ContainsKey(friend.alias))
                    {
                        friends.Add(friend.alias, friend);
                        friends.Add(CreateMD5(friend.alias), friend);
                    }
                }
            }
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
                                var match = Regex.Match(name, @"^Chat_([0-9a-f]{32})$");
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

        public bool SaveTextRecord(SQLiteConnection conn, string path, string displayname, string myname, string id, string table, out int count)
        {
            bool succ = false;
            count = 0;
            try
            {
                if (id.EndsWith("@chatroom")) return false;
                using (var cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = "SELECT CreateTime,Message,Des,Type FROM Chat_" + table;
                    using (var reader = cmd.ExecuteReader())
                    using (var sw = new StreamWriter(path))
                    {
                        while (reader.Read())
                            try
                            {
                                var unixtime = reader.GetInt32(0);
                                var message = reader.GetString(1);
                                var des = reader.GetInt32(2);
                                var type = reader.GetInt32(3);
                                var txtsender = (type == 10000 ? "[系统消息]" : (des == 1 ? displayname : myname));
                                if (type == 34) message = "[语音]";
                                else if (type == 47) message = "[表情]";
                                else if (type == 62) message = "[小视频]";
                                else if (type == 50) message = "[视频/语音通话]";
                                else if (type == 3) message = "[图片]";
                                else if (type == 49)
                                {
                                    if (message.Contains("微信红包")) message = "[红包]";
                                    else if (message.Contains("微信转账")) message = "[转账]";
                                    else if (message.Contains("我发起了位置共享")) message = "[位置共享]";
                                    else if (message.Contains("<appattach>")) message = "[文件]";
                                    else message = "链接";
                                }
                                else if (type == 42) message = "[名片]";

                                sw.WriteLine(txtsender + "(" + FromUnixTime(unixtime).ToLongTimeString() + ")" + ": " + message);
                                count++;

                            }
                            catch (Exception) { }
                    }
                    succ = true;
                }
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

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public static DateTime FromUnixTime(long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
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
        public string DisplayName()
        {
            if (ConRemark != null && ConRemark != "") return ConRemark;
            if (NickName != null && NickName != "") return NickName;
            if (UsrName != null && UsrName != "") return UsrName;
            return null;
        }
        public string ID()
        {
            if (alias != null && alias != "") return alias;
            if (UsrName != null && UsrName != "") return UsrName;
            return null;
        }
    }
}
