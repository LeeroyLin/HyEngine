using System;
using System.Collections.Generic;
using System.IO;
using Engine.Scripts.Runtime.Resource;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Engine.Scripts.Editor.Resource.BundleConfigWindow
{
    public class BundleConfigWindow : EditorWindow
    {
        [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;

        private static BundleConfig _config;
        private static List<BundleConfigData> _list;

        [MenuItem("Window/UI Toolkit/BundleConfigWindow")]
        public static void ShowExample()
        {
            // 加载配置
            LoadConfig();
            
            BundleConfigWindow wnd = GetWindow<BundleConfigWindow>();
            wnd.titleContent = new GUIContent("Bundle Config");
        }

        static void LoadConfig()
        {
            if (!_config)
                _config = AssetDatabase.LoadAssetAtPath<BundleConfig>("Assets/BundleAssets/BundleConfig/BundleConfigData.asset");

            if (_config == null)
            {
                Debug.LogError("【Bundle Config】 There is no BundleConfigData.asset at Assets/BundleAssets/BundleConfig");
                return;
            }

            _list = DeepCopyList(_config.dataList);
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Instantiate UXML
            VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
            root.Add(labelFromUXML);

            if (_config == null)
                return;
            
            var btnAdd = labelFromUXML.Q<Button>("btn-add");
            var btnSave = labelFromUXML.Q<Button>("btn-save");
            var btnRemove = labelFromUXML.Q<Button>("btn-remove");
            var listView = labelFromUXML.Q<MultiColumnListView>();
            
            btnAdd.RegisterCallback<ClickEvent>((evt) =>
            {
                if (_config == null)
                    return;
                
                _list.Add(new BundleConfigData());
                
                listView.RefreshItems();
            });
            btnRemove.RegisterCallback<ClickEvent>((evt) =>
            {
                if (_config == null)
                    return;

                int idx = listView.selectedIndex;

                if (idx < 0 || idx >= _list.Count)
                    return;
                
                _list.RemoveAt(idx);
                
                listView.RefreshItems();
            });
            btnSave.RegisterCallback<ClickEvent>((evt) =>
            {
                if (_config == null)
                    return;

                // 剔除无路径的项
                FormatList();
                
                _config.dataList = DeepCopyList(_list);

                EditorUtility.SetDirty(_config);
                
                Debug.Log("【Bundle Config】 Save success.");
                
                listView.RefreshItems();
            });

            listView.itemsSource = _list;

            listView.columns["path"].makeCell = () =>
            {
                VisualElement ve = new VisualElement();
                ve.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);

                var selector = new Button();
                selector.text = "+";
                ve.Add(selector);
                
                var textField = new TextField();
                textField.style.flexGrow = new StyleFloat(1);
                ve.Add(textField);

                return ve;
            };
            listView.columns["dirType"].makeCell = () => new EnumField(EABPackDir.File);
            listView.columns["compressType"].makeCell = () => new EnumField(EABCompress.LZ4);
            listView.columns["md5"].makeCell = () => new Toggle();

            listView.columns["path"].bindCell = (element, i) =>
            {
                var textField = element.Q<TextField>();
                var selector = element.Q<Button>();
                
                void OnPathChanged(ChangeEvent<string> evt)
                {
                    _list[i].path = evt.newValue;
                }
                
                textField.UnregisterCallback<ChangeEvent<string>>(OnPathChanged);
                textField.RegisterCallback<ChangeEvent<string>>(OnPathChanged);

                void OnSelectPath(ClickEvent evt)
                {
                    var folder = Path.Combine(Application.dataPath, "BundleAssets").Replace("/", "\\");
                    string path = EditorUtility.OpenFolderPanel("SelectFolder", folder, "").Replace("/", "\\");
                    
                    Debug.Log($"CCC {folder}");
                    Debug.Log($"CCC {path}");
                    
                    // 目录是否没在BundleAssets下
                    if (!path.Contains(folder))
                    {
                        Debug.LogError($"【Bundle Config】 Path must under Assets/BundleAssets");
                        
                        return;
                    }
                    
                    // 获得相对路径
                    var relPath = path.Substring(path.IndexOf("BundleAssets") + 13);
                    
                    textField.value = relPath;
                    _list[i].path = relPath;
                }
                
                selector.UnregisterCallback<ClickEvent>(OnSelectPath);
                selector.RegisterCallback<ClickEvent>(OnSelectPath);
                
                textField.value = _list[i].path;
                textField.isReadOnly = true;
            };
            listView.columns["dirType"].bindCell = (element, i) =>
            {
                var ele = (EnumField) element;

                void OnEnumChanged(ChangeEvent<Enum> evt)
                {
                    _list[i].packDirType = (EABPackDir) evt.newValue;
                }
                
                ele.UnregisterCallback<ChangeEvent<Enum>>(OnEnumChanged);
                ele.RegisterCallback<ChangeEvent<Enum>>(OnEnumChanged);

                ele.value = _list[i].packDirType;
            };
            listView.columns["compressType"].bindCell = (element, i) =>
            {
                var ele = (EnumField) element;

                void OnEnumChanged(ChangeEvent<Enum> evt)
                {
                    _list[i].packCompressType = (EABCompress) evt.newValue;
                }
                
                ele.UnregisterCallback<ChangeEvent<Enum>>(OnEnumChanged);
                ele.RegisterCallback<ChangeEvent<Enum>>(OnEnumChanged);

                ele.value = _list[i].packCompressType;
            };
            listView.columns["md5"].bindCell = (element, i) =>
            {
                var toggle = (Toggle) element;

                void OnToggleChanged(ChangeEvent<bool> evt)
                {
                    _list[i].md5 = evt.newValue;
                }
                
                toggle.UnregisterCallback<ChangeEvent<bool>>(OnToggleChanged);
                toggle.RegisterCallback<ChangeEvent<bool>>(OnToggleChanged);

                toggle.value = _list[i].md5;
            };
        }

        private static List<BundleConfigData> DeepCopyList(List<BundleConfigData> list)
        {
            List<BundleConfigData> newList = new List<BundleConfigData>();

            foreach (var data in list)
            {
                newList.Add(new BundleConfigData()
                {
                    path = data.path,
                    md5 = data.md5,
                    packCompressType = data.packCompressType,
                    packDirType = data.packDirType,
                });
            }
            
            return newList;
        }
        
        // 移除无效路径项
        private void FormatList() 
        {
            for (int i = _list.Count - 1; i >= 0; i--)
            {
                var data = _list[i];
                if (string.IsNullOrEmpty(data.path))
                    _list.RemoveAt(i);
            }
        }
    }
}
