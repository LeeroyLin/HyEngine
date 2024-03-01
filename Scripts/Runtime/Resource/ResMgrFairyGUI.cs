using System;
using FairyGUI;
using UnityEditor;
using UnityEngine;

namespace Engine.Scripts.Runtime.Resource
{
    public partial class ResMgr
    {
        /// <summary>
        /// 创建对象
        /// </summary>
        /// <param name="pkg"></param>
        /// <param name="name"></param>
        public GObject CreateUIObject(string pkg, string name)
        {
            return UIPackage.CreateObject(pkg, name);
        }
        
        /// <summary>
        /// 通过Url创建对象
        /// </summary>
        /// <param name="url"></param>
        public GObject CreateUIObjectFromUrl(string url)
        {
            return UIPackage.CreateObjectFromURL(url);
        }
        
        /// <summary>
        /// 获得UI的Url
        /// </summary>
        /// <param name="pkgName"></param>
        /// <param name="resName"></param>
        /// <returns></returns>
        public string GetUIItemURL(string pkgName, string resName)
        {
            return UIPackage.GetItemURL(pkgName, resName);
        }
        
        /// <summary>
        /// 添加包
        /// </summary>
        /// <param name="pkgName"></param>
        public void AddPackage(string pkgName)
        {
            // 是否是编辑器模式
            if (_resLoadMode == EResLoadMode.Editor)
            {
                // 编辑器模式加载
                LoadInEditorMode(pkgName);
            }
            // Resource模式
            else if (_resLoadMode == EResLoadMode.Resource)
            {
                // Resource模式加载
                LoadInResourceMode(pkgName);
            }
            // 包模式
            else if (_resLoadMode == EResLoadMode.AB || _resLoadMode == EResLoadMode.PackageAB)
            {
                // AB模式加载
                LoadInABMode(pkgName);
            }
        }
        
        // 编辑器模式加载
        void LoadInEditorMode(string pkgName)
        {
            #if UNITY_EDITOR
            var path = $"{BUNDLE_ASSETS_PATH}UI";
            var descPath = $"{path}/{pkgName}/{pkgName}_fui.bytes";
                    
            // 从路径加载描述文件
            TextAsset ta = AssetDatabase.LoadAssetAtPath<TextAsset>(descPath);
            if (ta == null)
            {
                _log.Error("[LoadInEditorMode] Can not get desc file at '{0}'", descPath);
                return;
            }

            UIPackage.AddPackage(ta.bytes, pkgName,
                (string name, string extension, Type type, out DestroyMethod method) =>
                {
                    method = DestroyMethod.Unload;
                    
                    if (!IsCouldLoad(name))
                        return null;
                    
                    var assetPath = $"{path}/{pkgName}/{name}{extension}";
                    
                    var asset = AssetDatabase.LoadAssetAtPath(assetPath, type);

                    return asset;
                });
            #endif
        }

        void LoadInResourceMode(string pkgName)
        {
            var path = "UI";
            var descPath = $"UI/{pkgName}/{pkgName}_fui.bytes";
                    
            // 从路径加载描述文件
            TextAsset ta = GetAssetFromResource<TextAsset>(descPath);
            if (ta == null)
            {
                _log.Error("[LoadInResourceMode] Can not get desc file at '{0}'", descPath);
                return;
            }

            UIPackage.AddPackage(ta.bytes, pkgName,
                (string name, string extension, Type type, out DestroyMethod method) =>
                {
                    method = DestroyMethod.Unload;

                    if (!IsCouldLoad(name))
                        return null;
                    
                    var assetPath = $"UI/{pkgName}/{name}{extension}";

                    var asset = GetAssetFromResource(assetPath, type);

                    return asset;
                });
        }

        // AB模式加载
        void LoadInABMode(string pkgName)
        {
            var descRelPath = $"UI/{pkgName}/{pkgName}_fui.bytes";
            
            var ta = GetAsset<TextAsset>(descRelPath);
            
            UIPackage.AddPackage(ta.bytes, pkgName,
                (string name, string extension, Type type, out DestroyMethod method) =>
                {
                    method = DestroyMethod.Unload;

                    if (!IsCouldLoad(name))
                        return null;
                    
                    var relPath = $"UI/{pkgName}/{name}{extension}";
                    
                    var tex = GetAsset<Texture2D>(relPath);

                    return tex;
                });
        }

        bool IsCouldLoad(string name)
        {
            return !name.Contains("!");
        }
    }
}