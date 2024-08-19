using System;
using System.Collections.Generic;
using Engine.Scripts.Editor.Resource.BundleBuild;
using Engine.Scripts.Runtime.Global;
using Engine.Scripts.Runtime.Utils;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Engine.Scripts.Editor.Resource.Build
{
    public class Builder
    {
        private static BuildCmdConfig _buildCmdConfig;
        private static GlobalConfig _globalConfig;
        
        /// <summary>
        /// 通过命令打包
        /// </summary>
        /// <returns></returns>
        public static void BuildWithCmd()
        {
            LoadGlobalConfig();

            // 加载命令行参数
            LoadBuildCmdConfig();
            
            // 打包资源
            var bundleRes = BundleBuilder.BuildWithCmd(_buildCmdConfig);

            if (!bundleRes)
                throw new Exception("Build bundle error.");

            string fullVer = $"{_buildCmdConfig.version}.{_buildCmdConfig.time}";

            if (_buildCmdConfig.isApk)
            {
                if (_buildCmdConfig.platform != BuildTarget.Android)
                    throw new Exception("Wrong platform");

                SetAndroidSettings(false);

                // 打包apk
                BuildApk(_buildCmdConfig.isDevBuild, $"{fullVer}/{_buildCmdConfig.name}_{_buildCmdConfig.env}_{fullVer}");
            }
            
            if (_buildCmdConfig.isAAB)
            {
                if (_buildCmdConfig.platform != BuildTarget.Android)
                    throw new Exception("Wrong platform");

                SetAndroidSettings(true);

                // 打包aab
                BuildAAB($"{fullVer}/{_buildCmdConfig.name}_{_buildCmdConfig.env}_{fullVer}");
            }
        }

        static void SetAndroidSettings(bool isAAB)
        {
            PlayerSettings.bundleVersion = _buildCmdConfig.version;
            PlayerSettings.Android.useCustomKeystore = true;

            var conf = AssetDatabase.LoadAssetAtPath<GlobalConfigSO>("Assets/Settings/GlobalConfig.asset");
                    
            PlayerSettings.Android.keystoreName = conf.buildConfig.keystoreName;
            PlayerSettings.Android.keystorePass = conf.buildConfig.keystorePass;
            PlayerSettings.Android.keyaliasName = conf.buildConfig.keyaliasName;
            PlayerSettings.Android.keyaliasPass = conf.buildConfig.keyaliasPass;

            EditorUserBuildSettings.buildAppBundle = isAAB;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
        }
        
        public static void BuildApk(bool isDevBuild, string relPath = "")
        {
            List<string> levels = new List<string>();
            
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled)
                    continue;
                levels.Add(scene.path);
            }

            if (string.IsNullOrEmpty(relPath))
                relPath = TimeUtilBase.GetLocalTimeMS() + "";

            var buildOptions = BuildOptions.None;

            if (isDevBuild)
            {
                buildOptions |= BuildOptions.Development;
                buildOptions |= BuildOptions.AllowDebugging;
                buildOptions |= BuildOptions.ConnectWithProfiler;
            }
            
            var res = BuildPipeline.BuildPlayer(levels.ToArray(),$"BuildOut/{relPath}.apk", 
                BuildTarget.Android, buildOptions);

            if (res.summary.result != BuildResult.Succeeded)
                throw new Exception("Build apk failed.");
        }
        
        public static void BuildAAB(string aabName = "")
        {
            List<string> levels = new List<string>();
            
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled)
                    continue;
                levels.Add(scene.path);
            }

            if (string.IsNullOrEmpty(aabName))
                aabName = TimeUtilBase.GetLocalTimeMS() + "";
            
            var res = BuildPipeline.BuildPlayer(levels.ToArray(),$"BuildOut/{aabName}/{aabName}.aab", 
                BuildTarget.Android, BuildOptions.None);

            if (res.summary.result != BuildResult.Succeeded)
                throw new Exception("Build aab failed.");
        }

        private static void LoadGlobalConfig()
        {
            _globalConfig = GlobalConfigUtil.LoadANewConfEditor();
        }
        
        // 加载命令行参数
        static void LoadBuildCmdConfig()
        {
            // 获取命令行参数
            string[] parameters = Environment.GetCommandLineArgs();
            
            _buildCmdConfig = new BuildCmdConfig();
            for (int i = 0; i < parameters.Length; i++)
            {
                string str = parameters[i];
                string[] paramArr = str.Split('=');
                if (paramArr.Length <= 1)
                    continue;
                string param = paramArr[1];
                if(str.StartsWith("Environment"))
                    _buildCmdConfig.env = (EEnv)Enum.Parse(typeof(EEnv), param);
                if(str.StartsWith("Platform"))
                    _buildCmdConfig.platform = (BuildTarget)Enum.Parse(typeof(BuildTarget), param);
                else if(str.StartsWith("Version"))
                    _buildCmdConfig.version = param;
                else if(str.StartsWith("Name"))
                    _buildCmdConfig.name = param;
                else if(str.StartsWith("IsCompileAllCode"))
                    _buildCmdConfig.isCompileAllCode = param == "true";
                else if(str.StartsWith("IsBuildApk"))
                    _buildCmdConfig.isApk = param == "true";
                else if(str.StartsWith("IsBuildAAB"))
                    _buildCmdConfig.isAAB = param == "true";
                else if(str.StartsWith("IsDevBuild"))
                    _buildCmdConfig.isDevBuild = param == "true";
                else if (str.StartsWith("Time"))
                    _buildCmdConfig.time = long.Parse(param);
            }
        }
    }
}