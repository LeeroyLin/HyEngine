using System.Text;

namespace Engine.Scripts.Runtime.Encrypt
{
    public class RC4
    {
        /// <summary>
        /// 配置的键
        /// </summary>
        static byte[] _key = Encoding.UTF8.GetBytes("LeeroyLin");

        /// <summary>
        /// 设置密钥文本
        /// </summary>
        /// <param name="keyStr"></param>
        public static void SetKey(string keyStr)
        {
            _key = Encoding.UTF8.GetBytes(keyStr);
        }
        
        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="data"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        public static byte[] Encrypt(byte[] data, int start = 0)
        {
            return Encrypt(_key, data, start);
        }

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="pwd"></param>
        /// <param name="data"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        public static byte[] Encrypt(byte[] pwd, byte[] data, int start = 0)
        {
            int a, i, j;
            int tmp;
            var key = new int[256];
            var box = new int[256];
            var cipher = new byte[data.Length];
            for (i = start; i < 256; i++)
            {
                key[i] = pwd[i % pwd.Length];
                box[i] = i;
            }

            for (j = i = start; i < 256; i++)
            {
                j = (j + box[i] + key[i]) % 256;
                tmp = box[i];
                box[i] = box[j];
                box[j] = tmp;
            }

            for (a = j = i = start; i < data.Length; i++)
            {
                a++;
                a %= 256;
                j += box[a];
                j %= 256;
                tmp = box[a];
                box[a] = box[j];
                box[j] = tmp;
                var k = box[(box[a] + box[j]) % 256];
                cipher[i] = (byte)(data[i] ^ k);
            }

            return cipher;
        }

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="data"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        public static byte[] Decrypt(byte[] data, int start = 0)
        {
            return Decrypt(_key, data, start);
        }

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="pwd"></param>
        /// <param name="data"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        public static byte[] Decrypt(byte[] pwd, byte[] data, int start = 0)
        {
            return Encrypt(pwd, data, start);
        }
    }
}