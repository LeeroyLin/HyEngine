using System;
using Client.Scripts.Runtime.Global;
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
        /// 添加包
        /// </summary>
        /// <param name="pkgName"></param>
        public void AddPackage(string pkgName)
        {
            // 是否是编辑器模式
            if (GlobalConfig.ResLoadMode == EResLoadMode.Editor)
            {
                // 编辑器模式加载
                LoadInEditorMode(pkgName);
            }
            // 包模式
            else if (GlobalConfig.ResLoadMode == EResLoadMode.AB)
            {
                // AB模式加载
                LoadInABMode(pkgName);
            }
        }
        
        // 编辑器模式加载
        void LoadInEditorMode(string pkgName)
        {
            var path = "Assets\\BundleAssets\\UI";
            var descPath = $"{path}\\Desc\\{pkgName}_fui.bytes";
                    
            // 从路径加载描述文件
            TextAsset ta = AssetDatabase.LoadAssetAtPath<TextAsset>(descPath);
            if (ta == null)
            {
                _log.Error("[AddPackage] Can not get desc file at '{0}'", descPath);
                return;
            }

            UIPackage.AddPackage(ta.bytes, pkgName,
                (string name, string extension, Type type, out DestroyMethod method) =>
                {
                    method = DestroyMethod.Unload;

                    var assetName = name;
                    
                    var assetPath = $"{path}\\Res\\{pkgName}\\{assetName}{extension}";
                    
                    var asset = AssetDatabase.LoadAssetAtPath(assetPath, type);

                    return asset;
                });
        }

        // AB模式加载
        void LoadInABMode(string pkgName)
        {
            var descABName = $"UI\\Desc\\{pkgName}_fui";
            var resABName = $"UI\\Res\\{pkgName}";

            var abDesc = LoadABWithABName(descABName);
            var abRes = LoadABWithABName(resABName);

            UIPackage.AddPackage(abDesc, abRes);
        }
    }
}