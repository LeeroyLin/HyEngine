using System.IO;
using UnityEngine;

namespace Engine.Scripts.Runtime.Utils
{
    public static partial class PathUtil
    {
        /// <summary>
        /// 绝对路径 转 Assets开头的路径 
        /// </summary>
        /// <param name="absolutePath">绝对路径</param>
        /// <returns>Assets开头的路径</returns>
        public static string AbsolutePath2AssetsPath(string absolutePath)
        {
            return absolutePath.Substring(absolutePath.IndexOf("Assets"));
        }
        
        /// <summary>
        /// Assets开头的路径 转 绝对路径 
        /// </summary>
        /// <param name="assetsPath">Assets开头的路径</param>
        /// <returns>绝对路径</returns>
        public static string AssetsPath2AbsolutePath(string assetsPath)
        {
            var path = assetsPath.Substring(7);

            return Path.Combine(Application.dataPath, path).Replace("/", "\\");
        }

        /// <summary>
        /// 确保有目录
        /// </summary>
        /// <param name="dir">路径或目录</param>
        public static void MakeSureDir(string dir)
        {
            if (Directory.Exists(dir))
                return;

            Directory.CreateDirectory(dir);
        }
    }
}