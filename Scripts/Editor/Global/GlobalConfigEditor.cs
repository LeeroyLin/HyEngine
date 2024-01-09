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

        private int _lastEnv;
        
        void OnEnable()
        {
            _logField = serializedObject.FindProperty("logConfig");
            _env = serializedObject.FindProperty("env");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            if (_env.enumValueIndex != _lastEnv)
            {
                var log = _logField.boxedValue as LogConfig;
                if (_env.enumValueIndex == 0)
                {
                    log.isShowLog = true;
                    log.isShowWarning = true;
                    log.isShowError = true;
                    log.isSaveLog = false;
                    log.isSaveWarning = false;
                    log.isSaveError = false;
                }
                else if (_env.enumValueIndex == 1)
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
                
                _lastEnv = _env.enumValueIndex;
            }
            
            serializedObject.ApplyModifiedProperties();
            
            base.OnInspectorGUI();
        }
    }
}