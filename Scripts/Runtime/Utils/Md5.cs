using System;
using System.IO;
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
        /// 加密文件16位
        /// </summary>
        /// <param name="filePath">加密的内容</param>
        /// <returns></returns>
        public static string EncryptFileMD5_16(string filePath)
        {
            var fs = new FileStream(filePath, FileMode.Open);
            
            MD5 md5 = MD5.Create();
            byte[] s = md5.ComputeHash(fs);
            fs.Close();

            string t2 = BitConverter.ToString(s, 4, 8);
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
            string contentEncrypt = "";
            MD5 md5 = MD5.Create();

            byte[] s = md5.ComputeHash(Encoding.UTF8.GetBytes(encryptContent));

            for (int i = 0; i < s.Length; i++)
                contentEncrypt += s[i].ToString("X2");
            
            return contentEncrypt;
        }
        
        /// <summary>
        /// 加密文件32位
        /// </summary>
        /// <param name="filePath">加密的内容</param>
        /// <returns></returns>
        public static string EncryptFileMD5_32(string filePath)
        {
            var fs = new FileStream(filePath, FileMode.Open);
            
            MD5 md5 = MD5.Create();
            byte[] s = md5.ComputeHash(fs);
            fs.Close();
            
            string contentEncrypt = "";

            for (int i = 0; i < s.Length; i++)
                contentEncrypt += s[i].ToString("X2");
            
            return contentEncrypt;
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
        
        /// <summary>
        /// 加密文件64位
        /// </summary>
        /// <param name="filePath">加密的内容</param>
        /// <returns></returns>
        public static string EncryptFileMD5_64(string filePath)
        {
            var fs = new FileStream(filePath, FileMode.Open);
            
            MD5 md5 = MD5.Create();
            byte[] s = md5.ComputeHash(fs);
            fs.Close();
            
            return Convert.ToBase64String(s);
        }
    }
}