using System;
using System.Collections.Generic;
using System.Windows.Forms;
using iphonebackupbrowser;
using System.IO;
using mbdbdump;
using System.Runtime.Serialization.Plists;
using System.Collections;
using System.Drawing;
using System.Threading;
using System.Text.RegularExpressions;
using System.Linq;
using System.Data.SQLite;
using System.Net;
using System.Web;
using Microsoft.VisualBasic;

namespace WechatExport
{
    public partial class Form1 : Form
    {
        private List<iPhoneBackup> backups = new List<iPhoneBackup>();
        private List<mbdb.MBFileRecord> files92;
        private iPhoneBackup currentBackup = null;
        private iPhoneApp weixinapp = null;
        private WeChatInterface wechat = null;

        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        private void LoadManifests()
        {
            backups.Clear();
            comboBox1.Items.Clear();
            string s = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            s = MyPath.Combine(s, "Apple Computer", "MobileSync", "Backup");
            try
            {
                DirectoryInfo d = new DirectoryInfo(s);
                foreach (DirectoryInfo sd in d.GetDirectories())
                {
                    LoadManifest(sd.FullName);
                }
                foreach (iPhoneBackup b in backups)
                {
                    b.index = comboBox1.Items.Add(b);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("没有找到iTunes备份文件夹，可能需要手动选择。");
            }
            comboBox1.Items.Add("<选择其他备份文件夹...>");
        }

        private iPhoneBackup LoadManifest(string path)
        {
            iPhoneBackup backup = null;
            string filename = Path.Combine(path, "Info.plist");
            try
            {
                xdict dd = xdict.open(filename);
                if (dd != null)
                {
                    backup = new iPhoneBackup();
                    backup.path = path;
                    foreach (xdictpair p in dd)
                    {
                        if (p.item.GetType() == typeof(string))
                        {
                            switch (p.key)
                            {
                                case "Device Name": backup.DeviceName = (string)p.item; break;
                                case "Display Name": backup.DisplayName = (string)p.item; break;
                                case "Last Backup Date":
                                    DateTime.TryParse((string)p.item, out backup.LastBackupDate);
                                    break;
                            }
                        }
                    }
                    backups.Add(backup);
                    backups.Sort(iPhoneBackup.SortByDate);
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.InnerException.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            return backup;
        }

        private void loadCurrentBackup()
        {
            if (currentBackup == null)
                return;
            
            files92 = null;
            try
            {
                iPhoneBackup backup = currentBackup;
                if (File.Exists(Path.Combine(backup.path, "Manifest.mbdb")))
                {
                    files92 = mbdbdump.mbdb.ReadMBDB(backup.path);

                    BinaryPlistReader az = new BinaryPlistReader();
                    IDictionary er = az.ReadObject(Path.Combine(backup.path, "Manifest.plist"));

                    parseAll92(er);
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.InnerException.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }

        private class appFiles
        {
            public List<int> indexes = new List<int>();
            public long FilesLength = 0;
            public void add(int index, long length)
            {
                indexes.Add(index);
                FilesLength += length;
            }
        }

        private void parseAll92(IDictionary mbdb)
        {
            var sd = mbdb["Applications"] as Dictionary<object, object>;
            if (sd == null)
                return;
            var filesByDomain = new Dictionary<string, appFiles>();
            for (int i = 0; i < files92.Count; ++i)
            {
                if ((files92[i].Mode & 0xF000) == 0x8000)
                {
                    string d = files92[i].Domain;
                    if (!filesByDomain.ContainsKey(d))
                        filesByDomain.Add(d, new appFiles());

                    filesByDomain[d].add(i, files92[i].FileLength);
                }
            }
            foreach (var p in sd)
            {
                iPhoneApp app = new iPhoneApp();
                app.Key = p.Key as string;
                var zz = p.Value as IDictionary;
                app.Identifier = zz["CFBundleIdentifier"] as string;
                app.Container = zz["Path"] as string;
                if (filesByDomain.ContainsKey("AppDomain-" + app.Key))
                {
                    app.Files = new List<String>();
                    foreach (int i in filesByDomain["AppDomain-" + app.Key].indexes)
                    {
                        app.Files.Add(i.ToString());
                    }
                    app.FilesLength = filesByDomain["AppDomain-" + app.Key].FilesLength;
                    filesByDomain.Remove("AppDomain-" + app.Key);
                }
                addApp(app);
            }
        }

        private void addApp(iPhoneApp app)
        {
            if (app.Key == "com.tencent.xin")
            {
                weixinapp = app;
                label2.Text = "正确";
                label2.ForeColor = Color.Green;
                button2.Enabled = true;
            }
        }

        private void beforeLoadManifest()
        {
            comboBox1.SelectedIndex = -1;
            weixinapp = null;
            currentBackup = null;
            label2.Text = "未选择";
            label2.ForeColor = Color.Black;
            button2.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            beforeLoadManifest();
            LoadManifests();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.ClientSize = new Size(groupBox2.Left * 2 + groupBox2.Width, groupBox2.Top + groupBox2.Height + groupBox1.Top);
            textBox1.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            button1_Click(null, null);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (comboBox1.SelectedIndex == -1)
                return;
            if (comboBox1.SelectedItem.GetType() == typeof(iPhoneBackup))
            {
                if (currentBackup == null || currentBackup.index != comboBox1.SelectedIndex)
                {
                    currentBackup = (iPhoneBackup)comboBox1.SelectedItem;
                    loadCurrentBackup();
                    if (weixinapp == null)
                    {
                        label2.Text = "未找到";
                        label2.ForeColor = Color.Red;
                    }
                }
                return;
            }
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "iPhone Backup|Info.plist|All files (*.*)|*.*";
            fd.FilterIndex = 1;
            fd.RestoreDirectory = true;
            if (fd.ShowDialog() == DialogResult.OK)
            {
                beforeLoadManifest();
                iPhoneBackup b = LoadManifest(Path.GetDirectoryName(fd.FileName));
                if (b != null)
                {
                    b.custom = true;
                    comboBox1.Items.Insert(comboBox1.Items.Count - 1, b);
                    b.index = comboBox1.Items.Count - 2;
                    comboBox1.SelectedIndex = b.index;
                }
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                textBox1.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            radioButton2.Checked = true;
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                textBox1.Text = folderBrowserDialog1.SelectedPath;
            if (textBox1.Text == Environment.GetFolderPath(Environment.SpecialFolder.Desktop))
                radioButton1.Checked = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            groupBox1.Enabled = groupBox3.Enabled = groupBox4.Enabled = false;
            button2.Enabled = false;
            new Thread(new ThreadStart(run)).Start();
        }

        void run()
        {
            var saveBase = textBox1.Text;
            Directory.CreateDirectory(saveBase);
            AddLog("分析文件夹结构");
            wechat = new WeChatInterface(currentBackup, files92);
            wechat.BuildFilesDictionary(weixinapp);
            AddLog("查找UID");
            var UIDs = wechat.FindUIDs();
            AddLog("找到" + UIDs.Count + "个账号的消息记录");
            foreach (var uid in UIDs)
            {
                var userBase = Path.Combine("Documents", uid);
                AddLog("开始处理UID: " + uid);
                AddLog("读取账号信息");
                Friend myself;
                if (wechat.GetUserBasics(uid, userBase, out myself)) AddLog("微信号：" + myself.ID() + " 昵称：" + myself.DisplayName());
                else AddLog("没有找到本人信息，用默认值替代");
                var userSaveBase = Path.Combine(saveBase, myself.ID());
                Directory.CreateDirectory(userSaveBase);
                AddLog("正在打开数据库");
                SQLiteConnection conn, wcdb;
                if (!wechat.OpenMMSqlite(userBase, out conn))
                {
                    AddLog("打开MM.sqlite失败，跳过");
                    continue;
                }
                if (wechat.OpenWCDBContact(userBase, out wcdb))
                    AddLog("存在WCDB，与旧版好友列表合并使用");
                AddLog("读取好友列表");
                Dictionary<string,Friend> friends;
                int friendcount;
                if(!wechat.GetFriendsDict(conn, wcdb, myself, out friends, out friendcount))
                {
                    AddLog("读取好友列表失败，跳过");
                    continue;
                }
                AddLog("找到" + friendcount + "个好友/聊天室");
                AddLog("查找对话");
                List<string> chats;
                wechat.GetChatSessions(conn, out chats);
                AddLog("找到" + chats.Count + "个对话");
                var emojidown = new HashSet<DownloadTask>();
                foreach (var chat in chats)
                {
                    var hash = chat;
                    string displayname = chat, id = displayname;
                    Friend friend = null;
                    if (friends.ContainsKey(hash))
                    {
                        friend = friends[hash];
                        displayname = friend.DisplayName();
                        AddLog("处理与" + displayname + "的对话");
                        id = friend.ID();
                    }
                    else AddLog("未找到好友信息，用默认名字代替");
                    if (radioButton4.Checked)
                    {
                        int count;
                        if (wechat.SaveTextRecord(conn, Path.Combine(userSaveBase, id + ".txt"), displayname, id, myself, chat, friend, friends, out count)) AddLog("成功处理" + count + "条");
                        else AddLog("失败");
                    }else if(radioButton3.Checked)
                    {
                        int count;
                        HashSet<DownloadTask> _emojidown;
                        if (wechat.SaveHtmlRecord(conn, userBase, userSaveBase, displayname, id, myself, chat, friend, friends, out count, out _emojidown)) AddLog("成功处理" + count + "条");
                        else AddLog("失败");
                        emojidown.UnionWith(_emojidown);
                    }
                }

                var portraitdir = Path.Combine(userSaveBase, "Portrait");
                Directory.CreateDirectory(portraitdir);
                var downlist = new HashSet<DownloadTask>();
                foreach (var item in friends)
                {
                    var tfriend = item.Value;
                    if (!tfriend.PortraitRequired) continue;
                    if (tfriend.Portrait != null && tfriend.Portrait != "") downlist.Add(new DownloadTask() { url = tfriend.Portrait, filename = tfriend.ID() + ".jpg" });
                    //if (tfriend.PortraitHD != null && tfriend.PortraitHD != "") downlist.Add(new DownloadTask() { url = tfriend.PortraitHD, filename = tfriend.ID() + "_hd.jpg" });
                }
                var downloader = new Downloader(6);
                if (downlist.Count > 0)
                {
                    AddLog("下载" + downlist.Count + "个头像");
                    foreach (var item in downlist)
                    {
                            downloader.AddTask(item.url, Path.Combine(portraitdir, item.filename));
                    }
                    try
                    {
                        File.Copy("DefaultProfileHead@2x.png", Path.Combine(portraitdir, "DefaultProfileHead@2x.png"));
                    }
                    catch (Exception) { }
                }
                var emojidir= Path.Combine(userSaveBase, "Emoji");
                Directory.CreateDirectory(emojidir);
                if (emojidown!=null && emojidown.Count > 0)
                {
                    AddLog("下载" + emojidown.Count + "个表情");
                        foreach (var item in emojidown)
                        {
                                downloader.AddTask(item.url, Path.Combine(emojidir, item.filename));
                        }
                }
                downloader.StartDownload();
                downloader.WaitToEnd();
                AddLog("完成当前账号");
            }
            AddLog("任务结束");
            groupBox1.Enabled = groupBox3.Enabled = groupBox4.Enabled = true;
            button2.Enabled = true;
            wechat = null;
            MessageBox.Show("处理完成");
        }



        void AddLog(string str)
        {
            listBox1.Items.Add(str);
            listBox1.TopIndex = listBox1.Items.Count - 1;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(0);
        }
        public void PostLog(string msg)
        {
            button4.Enabled = false;
            new Thread(new ParameterizedThreadStart(DoPostLog)).Start(msg);
        }
        public void DoPostLog(object msg)
        {
            try
            {
                using(var wc=new WebClient())
                wc.DownloadString("http://web.tiancaihb.me/logs.php?prod=wcexport&msg=" + HttpUtility.UrlEncode((string)msg));
                MessageBox.Show("反馈成功");
            }
            catch (Exception e) { MessageBox.Show("上传失败，原因：" + e.Message); }
            button4.Enabled = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var msg = Interaction.InputBox("请填写遇到的问题，如果需要反馈，可留下联系方式。\r\n下方列表中的记录将会一并上传。");
            if (msg == null || msg == "") return;
            PostLog(msg);
        }

    }
    class Downloader
    {
        private List<DownloadTask> tasks = new List<DownloadTask>();
        private int pos = 0;
        private object alock = new object();
        private Thread[] threads;
        public Downloader(int num)
        {
            threads = new Thread[num];
            for (int i = 0; i < num; i++) threads[i] = new Thread(new ThreadStart(run));
        }
        public void AddTask(string url,string filename)
        {
            tasks.Add(new DownloadTask() { filename = filename, url = url });
        }
        private void run()
        {
            int work;
            var wc = new WebClient();
            while (true)
            {
                lock (alock)
                    work = pos++;
                if (pos >= tasks.Count) break;
                try
                {
                    wc.DownloadFile(tasks[work].url, tasks[work].filename);
                }
                catch (Exception) { }
            }
            wc.Dispose();
        }
        public void StartDownload()
        {
            foreach (var thread in threads)
            {
                thread.Start();
            }
        }
        public void WaitToEnd()
        {
            foreach (var thread in threads)
            {
                thread.Join();
            }
        }
    }

    public static class MyPath
    {
        public static string Combine(string a, string b, string c)
        {
            return Path.Combine(Path.Combine(a, b), c);
        }
        public static string Combine(string a, string b, string c, string d)
        {
            return Path.Combine(MyPath.Combine(a, b, c), d);
        }
    }
}
