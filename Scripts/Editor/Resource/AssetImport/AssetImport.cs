using System.IO;
using Client.Scripts.Runtime.Global;
using Engine.Scripts.Runtime.Resource;
using UnityEditor;

namespace Engine.Scripts.Editor.Resource.AssetImport
{
    public class AssetImport : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var p in importedAssets)
            {
                // 非文件
                if (!File.Exists(p))
                    continue;

                // FGUI导出处理
                FGUIExportPost(p);
            }
            
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// FGUI导出处理
        /// </summary>
        /// <param name="p"></param>
        private static void FGUIExportPost(string p)
        {
            var path = p.Replace("/", "\\");

            var dir = Path.GetDirectoryName(path);

            var extension = Path.GetExtension(path);

            var uiDir = "Assets\\BundleAssets\\UI";
            if (GlobalConfig.ResLoadMode == EResLoadMode.Resource)
                uiDir = "Resources\\UI";
            
            if (dir == uiDir)
            {
                if (extension == ".bytes")
                {
                    MoveDesc(path, dir);
                }
                else if (extension == ".png")
                {
                    MovePng(path, dir);
                }
            }
        }
        
        private static void MoveDesc(string path, string dir)
        {
            var fileName = Path.GetFileName(path);
            var newDir = $"{dir}\\Desc";
            
            MakeSureDir(newDir);

            var newPath = $"{newDir}\\{fileName}";

            if (File.Exists(newPath))
                File.Delete(newPath);
            
            File.Move(path, newPath);
        }
        
        private static void MovePng(string path, string dir)
        {
            var fileName = Path.GetFileName(path);
            var pkgName = fileName.Split("_")[0];
            var newDir = $"{dir}\\Res\\{pkgName}";
            
            MakeSureDir(newDir);
            
            var newPath = $"{newDir}\\{fileName}";
            
            if (File.Exists(newPath))
                File.Delete(newPath);

            File.Move(path, newPath);
        }

        private static void MakeSureDir(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
    }
}