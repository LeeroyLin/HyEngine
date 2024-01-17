using System;
using System.Collections.Generic;
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
            return absolutePath.Substring(absolutePath.IndexOf("Assets")).Replace("\\", "/");
        }
        
        /// <summary>
        /// Assets开头的路径 转 绝对路径 
        /// </summary>
        /// <param name="assetsPath">Assets开头的路径</param>
        /// <returns>绝对路径</returns>
        public static string AssetsPath2AbsolutePath(string assetsPath)
        {
            var path = assetsPath.Substring(7);

            return Path.Combine(Application.dataPath, path).Replace("\\", "/");
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

        /// <summary>
        /// 清空目录
        /// </summary>
        /// <param name="dir"></param>
        public static void ClearDir(string dir)
        {
            if (!Directory.Exists(dir))
                return;

            var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
            foreach (var file in files)
                File.Delete(file);

            var dirs = Directory.GetDirectories(dir, "*", SearchOption.AllDirectories);
            foreach (var d in dirs)
                Directory.Delete(d);
        }

        /// <summary>
        /// 获得目录下所有非meta文件
        /// </summary>
        /// <param name="absPath"></param>
        /// <returns></returns>
        public static List<string> GetAllFilesAtPath(string absPath)
        {
            List<string> list = new List<string>();
            
            DirectoryInfo dir = new DirectoryInfo(absPath);
            
            //获取目标路径下的所有文件
            FileInfo[] allFiles = dir.GetFiles("*",SearchOption.AllDirectories);

            foreach (var info in allFiles)
            {
                if (info.Extension == ".meta")
                    continue;
                
                list.Add(info.FullName);
            }

            return list;
        }

        /// <summary>
        /// 遍历子目录
        /// </summary>
        /// <param name="absPath"></param>
        /// <param name="isTopOnly"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static void ForeachSubDirectoriesAtPath(string absPath, bool isTopOnly, Action<DirectoryInfo> handler)
        {
            if (handler == null)
                return;
            
            DirectoryInfo dir = new DirectoryInfo(absPath);

            var dirList = dir.GetDirectories("*", isTopOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);
            foreach (var info in dirList)
            {
                handler(info);
            }
        }
    }
}