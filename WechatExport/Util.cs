using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace WechatExport
{
    public class Downloader
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
        public void AddTask(string url, string filename)
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