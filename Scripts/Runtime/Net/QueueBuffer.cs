using System.Collections.Generic;
using UnityEngine;

namespace Engine.Scripts.Runtime.Net
{
    public class QueueBuffer
    {
        public int WritePosition { get; private set; }
        public int Length { get; private set; }
        
        List<byte> _list = new List<byte>();

        public void Read(byte[] data, int len)
        {
            if (Length < len)
            {
                Debug.Log($"[QueueBuffer] Read failed. Curr buffer length less than {len}");
                
                return;
            }
            
            if (data.Length < len)
            {
                Debug.Log($"[QueueBuffer] Read failed. Target byte array length less than {len}");
                
                return;
            }
            
            for (int i = 0; i < len; i++)
                data[i] = _list[i];
            
            _list.RemoveRange(0, len);

            Length -= len;
            WritePosition -= len;
        }

        public void Write(byte[] data, int len = -1)
        {
            if (len < 0)
                len = data.Length;
            
            int delta = WritePosition + len - _list.Count;
            
            if (delta > 0)
            {
                for (int i = 0; i < delta; i++)
                    _list.Add(0); 
            }
            
            for (int i = 0; i < len; i++)
                _list[i + WritePosition] = data[i];

            WritePosition += len;
            Length += len;
        }

        public void Reset()
        {
            Length = 0;
            WritePosition = 0;
        }
    }
}