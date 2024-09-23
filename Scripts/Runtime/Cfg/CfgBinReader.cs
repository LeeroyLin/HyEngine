using System;
using System.Text;
using Engine.Scripts.Runtime.Utils;
using UnityEngine;

namespace Engine.Scripts.Runtime.Cfg
{
    public class CfgBinReader : SingletonClass<CfgBinReader>
    {
        private static readonly int SHORT_LEN = 2;
        private static readonly int INT_LEN = 4;
        
        public int StartIdx { get; private set; }

        public bool ReadFinished => _bytes == null || StartIdx >= _bytes.Length;

        private byte[] _bytes;
        
        public void Init(byte[] bytes, int startIdx = 0)
        {
            _bytes = bytes;
            StartIdx = startIdx;
        }
        
        public void Dispose()
        {
            _bytes = null;
            StartIdx = 0;
        }

        public int ReadInt()
        {
            if (_bytes.Length - StartIdx < INT_LEN)
            {
                Debug.LogError("[CfgBinReader] byte length not enough.");
                return 0;
            }
            
            int val = BitConverter.ToInt32(_bytes, StartIdx);

            StartIdx += INT_LEN;

            return val;
        }

        public IntW ReadIntW()
        {
            var v = ReadInt();
            return new IntW(v);
        }

        public short ReadShort()
        {
            if (_bytes.Length - StartIdx < SHORT_LEN)
            {
                Debug.LogError("[CfgBinReader] byte length not enough.");
                return 0;
            }
            
            short val = BitConverter.ToInt16(_bytes, StartIdx);

            StartIdx += SHORT_LEN;

            return val;
        }

        public string ReadString()
        {
            // 获得字符串二进制长度
            short strLen = ReadShort();

            if (strLen < 0)
            {
                Debug.LogError("[CfgBinReader] wrong str length.");
                return string.Empty;
            }

            if (strLen == 0)
                return string.Empty;
            
            if (_bytes.Length - StartIdx < strLen)
            {
                Debug.LogError("[CfgBinReader] str length not enough.");
                return string.Empty;
            }
            
            var str = Encoding.UTF8.GetString(_bytes, StartIdx, strLen);
            
            StartIdx += strLen;

            return str;
        }

        public int[] ReadIntArr()
        {
            // 获得数组二进制长度
            short arrLen = ReadShort();

            if (arrLen < 0)
            {
                Debug.LogError("[CfgBinReader] wrong arr length.");
                return Array.Empty<int>();
            }

            if (arrLen == 0)
                return Array.Empty<int>();

            int[] arr = new int[arrLen];
            
            for (int i = 0; i < arrLen; i++)
                arr[i] = ReadInt();

            return arr;
        }

        public IntW[] ReadIntWArr()
        {
            // 获得数组二进制长度
            short arrLen = ReadShort();

            if (arrLen < 0)
            {
                Debug.LogError("[CfgBinReader] wrong arr length.");
                return Array.Empty<IntW>();
            }

            if (arrLen == 0)
                return Array.Empty<IntW>();

            IntW[] arr = new IntW[arrLen];
            
            for (int i = 0; i < arrLen; i++)
                arr[i] = ReadIntW();

            return arr;
        }

        public string[] ReadStringArr()
        {
            // 获得数组二进制长度
            short arrLen = ReadShort();

            if (arrLen < 0)
            {
                Debug.LogError("[CfgBinReader] wrong arr length.");
                return Array.Empty<string>();
            }

            if (arrLen == 0)
                return Array.Empty<string>();

            string[] arr = new string[arrLen];
            
            for (int i = 0; i < arrLen; i++)
                arr[i] = ReadString();

            return arr;
        }

        public int[][] ReadIntArr2()
        {
            // 获得二维数组二进制长度
            short arrLen = ReadShort();

            if (arrLen < 0)
            {
                Debug.LogError("[CfgBinReader] wrong arr length.");
                return Array.Empty<int[]>();
            }

            if (arrLen == 0)
                return Array.Empty<int[]>();

            int[][] arr = new int[arrLen][];
            
            for (int i = 0; i < arrLen; i++)
                arr[i] = ReadIntArr();

            return arr;
        }

        public IntW[][] ReadIntWArr2()
        {
            // 获得二维数组二进制长度
            short arrLen = ReadShort();

            if (arrLen < 0)
            {
                Debug.LogError("[CfgBinReader] wrong arr length.");
                return Array.Empty<IntW[]>();
            }

            if (arrLen == 0)
                return Array.Empty<IntW[]>();

            IntW[][] arr = new IntW[arrLen][];
            
            for (int i = 0; i < arrLen; i++)
                arr[i] = ReadIntWArr();

            return arr;
        }

        public string[][] ReadStringArr2()
        {
            // 获得二维数组二进制长度
            short arrLen = ReadShort();

            if (arrLen < 0)
            {
                Debug.LogError("[CfgBinReader] wrong arr length.");
                return Array.Empty<string[]>();
            }

            if (arrLen == 0)
                return Array.Empty<string[]>();

            string[][] arr = new string[arrLen][];
            
            for (int i = 0; i < arrLen; i++)
                arr[i] = ReadStringArr();

            return arr;
        }
    }
}