using System;
using System.Security.Cryptography;
using System.Text;

namespace Engine.Scripts.Runtime.Utils
{
    public class Md5
    {
        /// <summary>
        /// 加密16位
        /// </summary>
        /// <param name="encryptContent">加密的内容</param>
        /// <returns></returns>
        public static string EncryptMD5_16(string encryptContent)
        {
            var md5 = new MD5CryptoServiceProvider();
            string t2 = BitConverter.ToString(md5.ComputeHash(Encoding.Default.GetBytes(encryptContent)), 4, 8);
            t2 = t2.Replace("-", "");
            return t2;
        }
        
        /// <summary>
        /// 加密32位
        /// </summary>
        /// <param name="encryptContent">加密的内容</param>
        /// <returns></returns>
        public static string EncryptMD5_32(string encryptContent)
        {
            string content_Normal = encryptContent;
            string content_Encrypt = "";
            MD5 md5 = MD5.Create();

            byte[] s = md5.ComputeHash(Encoding.UTF8.GetBytes(content_Normal));

            for (int i = 0; i < s.Length; i++)
            {
                content_Encrypt = content_Encrypt + s[i].ToString("X2");
            }
            return content_Encrypt;
        }
        
        /// <summary>
        /// 加密64位
        /// </summary>
        /// <param name="encryptContent">加密的内容</param>
        /// <returns></returns>
        public static string EncryptMD5_64(string encryptContent)
        {
            string content = encryptContent;
            MD5 md5 = MD5.Create();
            byte[] s = md5.ComputeHash(Encoding.UTF8.GetBytes(content));
            return Convert.ToBase64String(s);
        }
    }
}