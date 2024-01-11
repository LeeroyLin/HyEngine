using Engine.Scripts.Runtime.Global;
using Engine.Scripts.Runtime.Log;
using UnityEditor;
using UnityEngine;

namespace Engine.Scripts.Editor.Global
{
    [CustomEditor(typeof(NetConfig))]
    public class NetConfigEditor : UnityEditor.Editor
    {
        SerializedProperty _nets;

        void OnEnable()
        {
            _nets = serializedObject.FindProperty("nets");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.TextField(new GUIContent("Login Server"), "");

            serializedObject.ApplyModifiedProperties();
            
            base.OnInspectorGUI();
        }
    }
}