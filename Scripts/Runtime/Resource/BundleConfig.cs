using System.Collections.Generic;
using UnityEngine;

namespace Engine.Scripts.Runtime.Resource
{
    public class BundleConfig
    {
        public List<BundleConfigData> dataList = new List<BundleConfigData>();
    }
    
    public class BundleConfigData
    {
        // 路径
        public string path;
        
        // AB打包目录类型
        public EABPackDir packDirType;
        
        // AB压缩策略
        public EABCompress packCompressType;
        
        // 是否md5命名
        public bool md5;
    }

    /// <summary>
    /// AB打包目录类型
    /// </summary>
    public enum EABPackDir
    {
        // 该目录下打包成一个AB
        Single,
        // 该目录下的一级子目录为一个AB
        SubSingle,
        // 该目录下的每个文件为一个AB
        File,
    }

    /// <summary>
    /// AB压缩方式
    /// </summary>
    public enum EABCompress
    {
        LZ4,
        LZMA,
        Uncompressed,
    }
}