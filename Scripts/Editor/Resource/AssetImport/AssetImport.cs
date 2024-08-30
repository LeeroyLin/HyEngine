using System.IO;
using System.Threading.Tasks;
using Engine.Scripts.Runtime.Global;
using Engine.Scripts.Runtime.Resource;
using UnityEditor;
using UnityEngine;

namespace Engine.Scripts.Editor.Resource.AssetImport
{
    public class AssetImport : AssetPostprocessor
    {
        private static readonly string DEFAULT_OUT_UI_DIR = "Assets/BundleAssets/UI";
        private static readonly string RESOURCES_OUT_UI_DIR = "Assets/Resources/UI";
        
        // 定义文件和图片文件分离
        private static readonly bool IS_SEPARATE = false;

        private static GlobalConfig _config;

        private static async Task LoadConfig()
        {
            _config = await GlobalConfigUtil.LoadANewConfRuntime();
        }
        
        private static async void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            await LoadConfig();
            
            if (_config == null)
                return;
            
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
            var path = p.Replace("\\", "/");

            var dir = Path.GetDirectoryName(path).Replace("\\", "/");

            var extension = Path.GetExtension(path);

            var fileName = Path.GetFileName(path);
            var pkgName = fileName.Split("_")[0];
            
            var uiDir = DEFAULT_OUT_UI_DIR;
            if (_config.resLoadMode == EResLoadMode.Resource || pkgName == "HotUpdate")
                uiDir = RESOURCES_OUT_UI_DIR;

            if (dir == DEFAULT_OUT_UI_DIR)
            {
                if (extension == ".bytes")
                {
                    MoveDesc(path, uiDir);
                }
                else if (extension == ".png")
                {
                    MovePng(path, uiDir);
                }
            }
        }

        private static void MoveDesc(string path, string dir)
        {
            var fileName = Path.GetFileName(path);
            var pkgName = fileName.Split("_")[0];
            var newDir = IS_SEPARATE ? $"{dir}/Desc" : $"{dir}/{pkgName}";
            
            MakeSureDir(newDir);

            var newPath = $"{newDir}/{fileName}";

            if (File.Exists(newPath))
                File.Delete(newPath);
            
            File.Move(path, newPath);
        }

        private static void MovePng(string path, string dir)
        {
            var fileName = Path.GetFileName(path);
            var pkgName = fileName.Split("_")[0];
            var newDir = IS_SEPARATE ? $"{dir}/Res/{pkgName}" : $"{dir}/{pkgName}";
            
            MakeSureDir(newDir);
            
            var newPath = $"{newDir}/{fileName}";
            
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