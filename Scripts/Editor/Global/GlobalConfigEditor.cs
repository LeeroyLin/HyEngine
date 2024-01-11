using Engine.Scripts.Runtime.Global;
using Engine.Scripts.Runtime.Log;
using UnityEditor;

namespace Engine.Scripts.Editor.Global
{
    [CustomEditor(typeof(GlobalConfigSO))]
    public class GlobalConfigEditor : UnityEditor.Editor
    {
        SerializedProperty _logField;
        SerializedProperty _env;
        SerializedProperty _netConfig;

        private EEnv _lastEnv;
        
        void OnEnable()
        {
            _logField = serializedObject.FindProperty("logConfig");
            _netConfig = serializedObject.FindProperty("netConfig");
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
        }
    }
}