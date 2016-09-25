using System.Collections.Generic;
using System.Data.SQLite;
using mbdbdump;
using System;
using System.IO;

namespace WechatExport
{
    public static class v10db
    {
        public static List<MBFileRecord> ReadMBDB(string BackupDB)
        {
            try
            {
                var files = new List<MBFileRecord>();
                using (var conn = new SQLiteConnection())
                {
                    conn.ConnectionString = "data source=" + BackupDB + ";version=3";
                    conn.Open();
                    using (var cmd = new SQLiteCommand(conn))
                    {
                        cmd.CommandText = "SELECT fileID,relativePath FROM Files WHERE domain='AppDomain-com.tencent.xin'";
                        using (var reader = cmd.ExecuteReader())
                            while (reader.Read())
                            {
                                var key = reader.GetString(0);
                                key = Path.Combine(key.Substring(0, 2), key);
                                var path = reader.GetString(1);
                                files.Add(new MBFileRecord()
                                {
                                    Path = path,
                                    key = key
                                });
                            }
                    }
                }
                return files;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
