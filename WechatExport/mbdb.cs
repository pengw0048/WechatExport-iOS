// Manifest.mbdb decoder
// Ren?DEVICHI 2010-2011

using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;


namespace mbdbdump
{

    #region BigEndianBitConverter class
    class BigEndianBitConverter
    {
        private static byte[] ReverseBytes(byte[] inArray, int offset, int count)
        {
            int j = count;
            byte[] ret = new byte[count];

            for (int i = offset; i < offset + count; ++i)
                ret[--j] = inArray[i];
            return ret;
        }

        public static short ToInt16(byte[] value, int startIndex)
        {
            if (BitConverter.IsLittleEndian)
            {
                return BitConverter.ToInt16(ReverseBytes(value, startIndex, 2), 0);
            }
            else
            {
                return BitConverter.ToInt16(value, startIndex);
            }
        }

        public static ushort ToUInt16(byte[] value, int startIndex)
        {
            if (BitConverter.IsLittleEndian)
            {
                return BitConverter.ToUInt16(ReverseBytes(value, startIndex, 2), 0);
            }
            else
            {
                return BitConverter.ToUInt16(value, startIndex);
            }
        }

        public static int ToInt32(byte[] value, int startIndex)
        {
            if (BitConverter.IsLittleEndian)
            {
                return BitConverter.ToInt32(ReverseBytes(value, startIndex, 4), 0);
            }
            else
            {
                return BitConverter.ToInt32(value, startIndex);
            }
        }

        public static uint ToUInt32(byte[] value, int startIndex)
        {
            if (BitConverter.IsLittleEndian)
            {
                return BitConverter.ToUInt32(ReverseBytes(value, startIndex, 4), 0);
            }
            else
            {
                return BitConverter.ToUInt32(value, startIndex);
            }
        }

        public static long ToInt64(byte[] value, int startIndex)
        {
            if (BitConverter.IsLittleEndian)
            {
                return BitConverter.ToInt64(ReverseBytes(value, startIndex, 8), 0);
            }
            return BitConverter.ToInt64(value, startIndex);

        }
    }
    #endregion


    class mbdb
    {
        private static string getS(Stream fs)
        {
            int b0 = fs.ReadByte();
            int b1 = fs.ReadByte();

            if (b0 == 255 && b1 == 255)
                return null;

            int length = b0 * 256 + b1;

            byte[] buf = new byte[length];
            fs.Read(buf, 0, length);

            // We need to do a "Unicode normalization form C" (see Unicode 4.0 TR#15)
            // since some applications don't like the canonical decomposition (NormalizationD)...

            // More information: http://msdn.microsoft.com/en-us/library/dd319093(VS.85).aspx
            // or http://msdn.microsoft.com/en-us/library/8eaxk1x2.aspx

            string s = Encoding.UTF8.GetString(buf, 0, length);

            return s.Normalize(NormalizationForm.FormC);
        }


        private static char toHex(int value)
        {
            value &= 0xF;
            if (value >= 0 && value <= 9) return (char)('0' + value);
            else return (char)('A' + (value - 10));
        }


        private static char toHexLow(int value)
        {
            value &= 0xF;
            if (value >= 0 && value <= 9) return (char)('0' + value);
            else return (char)('a' + (value - 10));
        }


        private static string toHex(byte[] data, params int[] spaces)
        {
            StringBuilder sb = new StringBuilder(data.Length * 3);

            int n = 0;
            int p = 0;

            for (int i = 0; i < data.Length; ++i)
            {
                if (n < spaces.Length && i == p + spaces[n])
                {
                    sb.Append(' ');
                    p += spaces[n];
                    n++;
                }
                sb.Append(toHex(data[i] >> 4));
                sb.Append(toHex(data[i] & 15));
            }

            return sb.ToString();
        }


        private static int fromHex(char c)
        {
            if (c >= '0' && c <= '9')
                return (int)(c - '0');

            if (c >= 'A' && c <= 'F')
                return (int)(c - 'A' + 10);

            if (c >= 'a' && c <= 'f')
                return (int)(c - 'a' + 10);

            return 0;
        }


        private static string getD(Stream fs)
        {
            int b0 = fs.ReadByte();
            int b1 = fs.ReadByte();

            if (b0 == 255 && b1 == 255)
                return null;

            int length = b0 * 256 + b1;

            byte[] buf = new byte[length];
            fs.Read(buf, 0, length);

            // if we have only ASCII printable characters, we return the string
            int i;
            for (i = 0; i < length; ++i)
            {
                if (buf[i] < 32 || buf[i] >= 128)
                    break;
            }
            if (i == length)
                return Encoding.ASCII.GetString(buf, 0, length);

            // otherwise the hexadecimal dump
            StringBuilder sb = new StringBuilder(length * 2);

            for (i = 0; i < length; ++i)
            {
                sb.Append(toHex(buf[i] >> 4));
                sb.Append(toHex(buf[i] & 15));
            }

            return sb.ToString();
        }




        public static List<MBFileRecord> ReadMBDB(string BackupPath)
        {
            try
            {
                List<MBFileRecord> files;
                byte[] signature = new byte[6];                     // buffer signature
                byte[] buf = new byte[26];                          // buffer for .mbdx record
                StringBuilder sb = new StringBuilder(40);           // stringbuilder for the Key
                byte[] data = new byte[40];                         // buffer for the fixed part of .mbdb record
                SHA1CryptoServiceProvider hasher = new SHA1CryptoServiceProvider();

                System.DateTime unixEpoch = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);


                // open the database
                FileStream mbdb = new FileStream(Path.Combine(BackupPath, "Manifest.mbdb"), FileMode.Open, FileAccess.Read);

                // skip signature
                mbdb.Read(signature, 0, 6);
                if (BitConverter.ToString(signature, 0) != "6D-62-64-62-05-00")     // "mbdb\5\0"
                {
                    throw new Exception("bad .mbdb file");
                }

                files = new List<MBFileRecord>();

                // loop through the records
                for (int i = 0; mbdb.Position < mbdb.Length; ++i)
                {
                    MBFileRecord rec = new MBFileRecord();

                    rec.Domain = getS(mbdb);
                    rec.Path = getS(mbdb);
                    rec.LinkTarget = getS(mbdb);
                    rec.DataHash = getD(mbdb);
                    rec.alwaysNull = getD(mbdb);

                    mbdb.Read(data, 0, 40);

                    rec.data = toHex(data, 2, 4, 4, 4, 4, 4, 4, 4, 8, 1, 1);

                    rec.Mode = BigEndianBitConverter.ToUInt16(data, 0);
                    rec.alwaysZero = BigEndianBitConverter.ToInt32(data, 2);
                    rec.inode = BigEndianBitConverter.ToUInt32(data, 6);
                    rec.UserId = BigEndianBitConverter.ToUInt32(data, 10);      // or maybe GroupId (don't care...)
                    rec.GroupId = BigEndianBitConverter.ToUInt32(data, 14);     // or maybe UserId

                    rec.aTime = unixEpoch.AddSeconds(BigEndianBitConverter.ToUInt32(data, 18));
                    rec.bTime = unixEpoch.AddSeconds(BigEndianBitConverter.ToUInt32(data, 22));
                    rec.cTime = unixEpoch.AddSeconds(BigEndianBitConverter.ToUInt32(data, 26));

                    rec.FileLength = BigEndianBitConverter.ToInt64(data, 30);

                    rec.flag = data[38];
                    rec.PropertyCount = data[39];

                    rec.Properties = new MBFileRecord.Property[rec.PropertyCount];
                    for (int j = 0; j < rec.PropertyCount; ++j)
                    {
                        rec.Properties[j].Name = getS(mbdb);
                        rec.Properties[j].Value = getD(mbdb);
                    }

                    StringBuilder fileName = new StringBuilder();
                    byte[] fb = hasher.ComputeHash(ASCIIEncoding.UTF8.GetBytes(rec.Domain + "-" + rec.Path));
                    for (int k = 0; k < fb.Length; k++)
                    {
                        fileName.Append(fb[k].ToString("x2"));
                    }

                    rec.key = fileName.ToString();

                    if(rec.Domain.EndsWith("com.tencent.xin") && (rec.Mode & 0xF000) == 0x8000) files.Add(rec);
                }

                return files;
            }
            catch (Exception e)
            {
                Console.WriteLine("exception: {0}", e.Message);
            }

            return null;
        }

    }

    public struct MBFileRecord
    {
        public string key;              // filename in the backup directory: SHA.1 of Domain + "-" + Path

        public string Domain;
        public string Path;
        public string LinkTarget;
        public string DataHash;         // SHA.1 for 'important' files
        public string alwaysNull;

        public string data;             // the 40-byte block (some fields still need to be explained)

        public ushort Mode;             // 4xxx=dir, 8xxx=file, Axxx=symlink
        public int alwaysZero;
        public uint inode;              // without any doubt
        public uint UserId;             // 501/501 for apps
        public uint GroupId;
        public DateTime aTime;          // aTime or bTime is the former ModificationTime
        public DateTime bTime;
        public DateTime cTime;
        public long FileLength;         // always 0 for link or directory
        public byte flag;               // 0 for link, 4 for directory, otherwise values unknown (4 3 1)
        public byte PropertyCount;

        public struct Property
        {
            public string Name;
            public string Value;
        };
        public Property[] Properties;
    }
}
