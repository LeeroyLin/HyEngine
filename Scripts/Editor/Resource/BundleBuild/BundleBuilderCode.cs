using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Engine.Scripts.Runtime.Resource;
using Engine.Scripts.Runtime.Utils;
using HybridCLR.Editor;
using HybridCLR.Editor.AOT;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.Meta;
using UnityEditor;
using UnityEngine;

namespace Engine.Scripts.Editor.Resource.BundleBuild
{
    public partial class BundleBuilder
    {
        private static readonly string HOT_UPDATE_CODE_SUB_PATH = "Logic/HotUpdate";
        private static readonly string AOT_CODE_SUB_PATH = "Logic/Aot";

        // 编译热更dlls
        static bool CompileHybridDlls(BuildTarget target)
        {
            Log("Start compile hybrid dlls.");

            // 重置目录
            ResetDir();

            AssetDatabase.Refresh();

            string errorMsg = null;
            List<string> aotRefs = null;            
            
            if (_buildCmdConfig.isCompileAllCode)
            {
                CompileDllCommand.CompileDll(target);
                Il2CppDefGeneratorCommand.GenerateIl2CppDef();

                // 这几个生成依赖HotUpdateDlls
                LinkGeneratorCommand.GenerateLinkXml(target);

                // 生成裁剪后的aot dll
                StripAOTDllCommand.GenerateStripedAOTDlls(target);

                // 桥接函数生成依赖于AOT dll，必须保证已经build过，生成AOT dll
                MethodBridgeGeneratorCommand.GenerateMethodBridge(target);
                ReversePInvokeWrapperGeneratorCommand.GenerateReversePInvokeWrapper(target);
                (errorMsg, aotRefs) = GenerateAOTGenericReference(target);
            }
            else
            {
                CompileDllCommand.CompileDll(target);
                
                (errorMsg, aotRefs) = GenerateAOTGenericReference(target);
                
                if (!string.IsNullOrEmpty(errorMsg))
                {
                    StripAOTDllCommand.GenerateStripedAOTDlls(target);
                    Log("Aot ref needs update.");
                }
            
                (errorMsg, aotRefs) = GenerateAOTGenericReference(target);
                
                if (!string.IsNullOrEmpty(errorMsg))
                {
                    LogError($"Generate AOT reference failed.  err: {errorMsg}");
                    return false;
                }
            }
            
            var defDir = $"{Application.dataPath}/../{SettingsUtil.HybridCLRSettings.hotUpdateDllCompileOutputRootDir}/{PlatformInfo.Platform}";
            var aotDir = $"{Application.dataPath}/../{SettingsUtil.HybridCLRSettings.strippedAOTDllOutputRootDir}/{PlatformInfo.Platform}";
            
            // 移动热更dlls
            foreach (var def in SettingsUtil.HybridCLRSettings.hotUpdateAssemblyDefinitions)
            {
                var oriPath = $"{defDir}/{def.name}.dll";
                var savePath = $"{ResMgr.BUNDLE_ASSETS_PATH}{HOT_UPDATE_CODE_SUB_PATH}/{def.name}.dll.bytes";
                savePath = PathUtil.AssetsPath2AbsolutePath(savePath);

                MakeSureDir(savePath);

                try
                {
                    File.Move(oriPath, savePath);
                }
                catch (Exception e)
                {
                    LogError($"Move hot update dll failed. oriPath: {oriPath} savePath: {savePath} err: {e.Message}");
                    return false;
                }

                Log($"Move hot update dll success. oriPath: {oriPath} savePath: {savePath}");
            }
            
            // 移动元dlls
            foreach (var aotRef in aotRefs)
            {
                var oriPath = $"{aotDir}/{aotRef}";
                var savePath = $"{ResMgr.BUNDLE_ASSETS_PATH}{AOT_CODE_SUB_PATH}/{aotRef}.bytes";
                savePath = PathUtil.AssetsPath2AbsolutePath(savePath);
                
                MakeSureDir(savePath);

                try
                {
                    File.Move(oriPath, savePath);
                }
                catch (Exception e)
                {
                    LogError($"Move aot ref dll failed. oriPath: {oriPath} savePath: {savePath} err: {e.Message}");
                    return false;
                }

                Log($"Move aot ref dll success. oriPath: {oriPath} savePath: {savePath}");
            }
            
            AssetDatabase.Refresh();
            
            Log("Compile hybrid dlls finished.");
            
            return true;
        }
        
        // 重置目录
        static void ResetDir()
        {
            var hotUpdateCodePath = $"{ResMgr.BUNDLE_ASSETS_PATH}{HOT_UPDATE_CODE_SUB_PATH}";
            hotUpdateCodePath = PathUtil.AssetsPath2AbsolutePath(hotUpdateCodePath);
            
            var aotCodePath = $"{ResMgr.BUNDLE_ASSETS_PATH}{AOT_CODE_SUB_PATH}";
            aotCodePath = PathUtil.AssetsPath2AbsolutePath(aotCodePath);

            DeleteDir(hotUpdateCodePath);
            DeleteDir(aotCodePath);

            Directory.CreateDirectory(hotUpdateCodePath);
            Directory.CreateDirectory(aotCodePath);
        }

        static void DeleteDir(string dir)
        {
            if (!Directory.Exists(dir))
                return;
            
            var files = Directory.GetFiles(dir);
            foreach (var file in files)
                File.Delete(file);
            
            Directory.Delete(dir, true);
        }

        static void MakeSureDir(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        /// <summary>
        /// 生成aot引用
        /// </summary>
        /// <param name="target"></param>
        /// <returns>错误信息，引用列表</returns>
        static (string, List<string>) GenerateAOTGenericReference(BuildTarget target)
        {
            List<string> aotRefs = new List<string>();
            
            var gs = SettingsUtil.HybridCLRSettings;
            List<string> hotUpdateDllNames = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;

            var assemblyResolver = MetaUtil.CreateHotUpdateAndAOTAssemblyResolver(target, hotUpdateDllNames);
            AssemblyReferenceDeepCollector collector = null;

            try
            {
                collector = new AssemblyReferenceDeepCollector(assemblyResolver, hotUpdateDllNames);
            }
            catch (Exception e)
            {
                collector?.Dispose();
                
                return (e.Message, null);
            }
            
            var analyzer = new Analyzer(new Analyzer.Options
            {
                MaxIterationCount = Math.Min(20, gs.maxGenericReferenceIteration),
                Collector = collector,
            });

            analyzer.Run();

            var types = analyzer.AotGenericTypes.ToList();
            var methods = analyzer.AotGenericMethods.ToList();
            
            List<dnlib.DotNet.ModuleDef> modules = new HashSet<dnlib.DotNet.ModuleDef>(
                types.Select(t => t.Type.Module).Concat(methods.Select(m => m.Method.Module))).ToList();
            modules.Sort((a, b) => a.Name.CompareTo(b.Name));
            foreach (dnlib.DotNet.ModuleDef module in modules)
                aotRefs.Add(module.Name);
            
            collector.Dispose();

            return (null, aotRefs);
        }
    }
}