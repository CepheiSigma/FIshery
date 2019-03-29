using System;
using System.IO;
using System.Threading;
using Sapphire.Universal.Utils;
using Encoding = System.Text.Encoding;

namespace Fishery.Core.Utils
{
    public class Common
    {
        public static string UrlEncode(string content)
        {
            return EncoderUtils.Url.UrlEncode(content, Encoding.UTF8);
        }
        public static string UrlEncode(string content, Encoding encoding)
        {
            return EncoderUtils.Url.UrlEncode(content, encoding);
        }

        public static byte[] GetStringBytes(string s)
        {
            return GetStringBytes(s, Encoding.UTF8);
        }
        public static byte[] GetStringBytes(string s,Encoding encoding)
        {
            return encoding.GetBytes(s);
        }

        public static string MD5(string s)
        {
            return MD5(s, Encoding.UTF8);
        }

        public static string MD5(string s, Encoding encoding)
        {
            encoding = encoding ?? Encoding.UTF8;
            return EncoderUtils.ToMD5String(s, encoding);
        }

        public static bool IsFileInUse(string filePath)
        {
            FileStream stream = null;

            try
            {
                if (!File.Exists(filePath))
                    return false;
                stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                stream?.Close();
            }

            return false;
        }

        public static void WaitForFile(string filePath, int waitTimes = -1)
        {
            while (waitTimes != 0 && IsFileInUse(filePath))
            {
                waitTimes--;
                Thread.Sleep(100);
            }
        }
    }
}