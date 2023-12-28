using System;
using System.Collections.Generic;
using System.IO;
using Engine.Scripts.Editor.Resource.BundleBuild;
using Engine.Scripts.Runtime.Resource;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Engine.Scripts.Editor.Resource.BundleConfigWindow
{
    public class BundleConfigWindow : EditorWindow
    {
        [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;

        private static BundleConfig _config;
        private static List<BundleConfigData> _list;

        private static Dictionary<string,  EventCallback<ChangeEvent<string>>> _eventStringDic = new ();
        private static Dictionary<string, EventCallback<ClickEvent>> _eventClickDic = new ();
        private static Dictionary<string, EventCallback<ChangeEvent<Enum>>> _eventEnumDic = new ();
        private static Dictionary<string, EventCallback<ChangeEvent<bool>>> _eventBoolDic = new ();

        [MenuItem("Bundle/Bundle Config/Bundle Config Window")]
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
                _config = AssetDatabase.LoadAssetAtPath<BundleConfig>(BundleBuilder.CONFIG_PATH);

            if (_config == null)
            {
                Debug.LogError($"【Bundle Config】 There is no BundleConfigData.asset at {ResMgr.BUNDLE_ASSETS_PATH}BundleConfig");
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
            
            // 主按钮事件注册
            MainBtnEventReg(labelFromUXML);
            
            var listView = labelFromUXML.Q<MultiColumnListView>();

            listView.itemsSource = _list;

            // 创建列处理
            MakeCell(listView);

            // 取消列绑定事件
            UnbindColumnsEvent(listView);
                
            // 绑定列事件
            BindColumnsEvent(listView);
        }

        /// <summary>
        /// 主按钮事件注册
        /// </summary>
        /// <param name="labelFromUXML"></param>
        private void MainBtnEventReg(VisualElement labelFromUXML)
        {
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
        }

        /// <summary>
        /// 创建列处理
        /// </summary>
        /// <param name="listView"></param>
        private void MakeCell(MultiColumnListView listView)
        {
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
        }
        
        /// <summary>
        /// 取消列绑定事件
        /// </summary>
        /// <param name="listView"></param>
        private void UnbindColumnsEvent(MultiColumnListView listView)
        {
            listView.columns["path"].unbindCell = (element, i) =>
            {
                var textField = element.Q<TextField>();
                var selector = element.Q<Button>();
                
                var key = $"path_textField_{i}";
                if (_eventStringDic.TryGetValue(key, out var handler1))
                {
                    textField.UnregisterCallback(handler1);
                    
                    _eventStringDic.Remove(key);
                }
                
                key = $"path_selector_{i}";
                if (_eventClickDic.TryGetValue(key, out var handler2))
                {
                    selector.UnregisterCallback(handler2);
                    
                    _eventClickDic.Remove(key);
                }
            };
            listView.columns["dirType"].unbindCell = (element, i) =>
            {
                var ele = (EnumField) element;

                var key = $"dirType_{i}";
                
                if (_eventEnumDic.TryGetValue(key, out var handler))
                {
                    ele.UnregisterCallback(handler);
                    
                    _eventEnumDic.Remove(key);
                }
            };
            listView.columns["compressType"].unbindCell = (element, i) =>
            {
                var ele = (EnumField) element;

                var key = $"compressType_{i}";
                
                if (_eventEnumDic.TryGetValue(key, out var handler))
                {
                    ele.UnregisterCallback(handler);
                    
                    _eventEnumDic.Remove(key);
                }
            };
            listView.columns["md5"].unbindCell = (element, i) =>
            {
                var ele = (Toggle) element;

                var key = $"md5_{i}";
                
                if (_eventBoolDic.TryGetValue(key, out var handler))
                {
                    ele.UnregisterCallback(handler);
                    
                    _eventBoolDic.Remove(key);
                }
            };
        }

        /// <summary>
        /// 列表绑定事件
        /// </summary>
        /// <param name="listView"></param>
        private void BindColumnsEvent(MultiColumnListView listView)
        {
            listView.columns["path"].bindCell = (element, i) =>
            {
                var textField = element.Q<TextField>();
                var selector = element.Q<Button>();
                
                void OnPathChanged(ChangeEvent<string> evt)
                {
                    _list[i].path = evt.newValue;
                }
                
                var key = $"path_textField_{i}";
                if (!_eventStringDic.ContainsKey(key))
                {
                    _eventStringDic.Add(key, OnPathChanged);
                    textField.RegisterCallback<ChangeEvent<string>>(OnPathChanged);
                }
        
                void OnSelectPath(ClickEvent evt)
                {
                    var folder = Path.Combine(Application.dataPath, "BundleAssets").Replace("/", "\\");
                    string path = EditorUtility.OpenFolderPanel("SelectFolder", folder, "").Replace("/", "\\");
                    
                    if (string.IsNullOrEmpty(path))
                        return;
                    
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
                
                key = $"path_selector_{i}";
                if (!_eventClickDic.ContainsKey(key))
                {
                    _eventClickDic.Add(key, OnSelectPath);
                    selector.RegisterCallback<ClickEvent>(OnSelectPath);
                }
                
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
                
                var key = $"dirType_{i}";
                if (!_eventEnumDic.ContainsKey(key))
                {
                    _eventEnumDic.Add(key, OnEnumChanged);
                    ele.RegisterCallback<ChangeEvent<Enum>>(OnEnumChanged);
                }

                ele.value = _list[i].packDirType;
            };
            listView.columns["compressType"].bindCell = (element, i) =>
            {
                var ele = (EnumField) element;

                void OnEnumChanged(ChangeEvent<Enum> evt)
                {
                    _list[i].packCompressType = (EABCompress) evt.newValue;
                }
                
                var key = $"compressType_{i}";
                if (!_eventEnumDic.ContainsKey(key))
                {
                    _eventEnumDic.Add(key, OnEnumChanged);
                    ele.RegisterCallback<ChangeEvent<Enum>>(OnEnumChanged);
                }

                ele.value = _list[i].packCompressType;
            };
            listView.columns["md5"].bindCell = (element, i) =>
            {
                var toggle = (Toggle) element;

                void OnToggleChanged(ChangeEvent<bool> evt)
                {
                    _list[i].md5 = evt.newValue;
                }
                
                var key = $"md5_{i}";
                if (!_eventBoolDic.ContainsKey(key))
                {
                    _eventBoolDic.Add(key, OnToggleChanged);
                    toggle.RegisterCallback<ChangeEvent<bool>>(OnToggleChanged);
                }
                
                toggle.value = _list[i].md5;
            };
        }
        
        // 深拷贝列表
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
