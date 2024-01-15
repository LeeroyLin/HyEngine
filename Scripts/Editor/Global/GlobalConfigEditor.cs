using System.IO;
using Engine.Scripts.Runtime.Global;
using Engine.Scripts.Runtime.Log;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Engine.Scripts.Editor.Global
{
    [CustomEditor(typeof(GlobalConfigSO))]
    public class GlobalConfigEditor : UnityEditor.Editor
    {
        private static readonly string GLOBAL_CONFIG_STREAMING_ASSETS_PATH = "Config/GlobalConfig.json";
        
        SerializedProperty _logField;
        SerializedProperty _env;

        private EEnv _lastEnv;
        
        void OnEnable()
        {
            _logField = serializedObject.FindProperty("logConfig");
            _env = serializedObject.FindProperty("env");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var env = (EEnv)_env.enumValueIndex;
            
            if (env != _lastEnv)
            {
                var log = _logField.boxedValue as LogConfig;
                if (env == EEnv.Develop)
                {
                    log.isShowLog = true;
                    log.isShowWarning = true;
                    log.isShowError = true;
                    log.isSaveLog = false;
                    log.isSaveWarning = false;
                    log.isSaveError = false;
                }
                else if (env == EEnv.Release)
                {
                    log.isShowLog = false;
                    log.isShowWarning = false;
                    log.isShowError = true;
                    log.isSaveLog = false;
                    log.isSaveWarning = false;
                    log.isSaveError = false;
                }
                else
                {
                    log.isShowLog = false;
                    log.isShowWarning = false;
                    log.isShowError = true;
                    log.isSaveLog = false;
                    log.isSaveWarning = false;
                    log.isSaveError = false;
                }
                
                _logField.boxedValue = log;
            
                _lastEnv = env;
            }
            
            serializedObject.ApplyModifiedProperties();

            base.OnInspectorGUI();
            
            if (GUILayout.Button("Save"))
            {
                var content = JsonConvert.SerializeObject(target);

                var path = $"{Application.streamingAssetsPath}/{GLOBAL_CONFIG_STREAMING_ASSETS_PATH}";
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                
                File.WriteAllText(path, content);
                
                Debug.Log($"Save global config finished.");
                
                AssetDatabase.Refresh();
            }
        }
    }
}