using System;
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
        private static readonly string TEX_DIR = "Assets/Arts";
        private static readonly string FONT_DIR = "Assets/BundleAsses/Font";
        
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

            bool isChanged = false;
            
            foreach (var p in importedAssets)
            {
                // 非文件
                if (!File.Exists(p))
                    continue;
            
                // FGUI导出处理
                if (FGUIExportPost(p))
                    isChanged = true;

                // 图片处理
                if (SpritePost(p))
                    isChanged = true;
            }

            if (isChanged)
                AssetDatabase.Refresh();
        }

        private static bool SpritePost(string p)
        {
            var path = p.Replace("\\", "/");

            if (path.StartsWith(TEX_DIR) || path.StartsWith(FONT_DIR))
                return false;
            
            var extension = Path.GetExtension(path);

            if (extension == ".png" || extension == ".jpg" || extension == ".jpeg")
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    TextureImporterPlatformSettings importPlatformAndroid = importer.GetPlatformTextureSettings("Android");

                    if (importPlatformAndroid.overridden && importPlatformAndroid.format == TextureImporterFormat.ETC2_RGBA8Crunched)
                        return false;
                    
                    importPlatformAndroid.overridden = true;

                    importPlatformAndroid.format = TextureImporterFormat.ETC2_RGBA8Crunched;
                    
                    importer.SetPlatformTextureSettings(importPlatformAndroid);
                    importer.SaveAndReimport();

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// FGUI导出处理
        /// </summary>
        /// <param name="p"></param>
        private static bool FGUIExportPost(string p)
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
                    return true;
                }
                
                if (extension == ".png")
                {
                    MovePng(path, uiDir);
                    return true;
                }
            }

            return false;
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