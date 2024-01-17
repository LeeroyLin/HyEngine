using System;
using System.Collections.Generic;
using System.IO;
using Engine.Scripts.Editor.Resource.BundleBuild;
using Engine.Scripts.Runtime.Resource;
using Newtonsoft.Json;
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
            if (_config == null)
            {
                if (File.Exists(BundleBuilder.CONFIG_PATH))
                {
                    var content = File.ReadAllText(BundleBuilder.CONFIG_PATH);
                    _config = JsonConvert.DeserializeObject<BundleConfig>(content);
                }
                else
                {
                    _config = new BundleConfig();
                    var jsonStr = JsonConvert.SerializeObject(_config);
                    File.WriteAllText(BundleBuilder.CONFIG_PATH, jsonStr);
                }
            }

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

                var jsonStr = JsonConvert.SerializeObject(_config);
                File.WriteAllText(BundleBuilder.CONFIG_PATH, jsonStr);
                
                listView.RefreshItems();
                
                Debug.Log($"Save success.");
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
            listView.columns["updateType"].makeCell = () => new EnumField(EABUpdate.Advance);
            listView.columns["md5"].makeCell = () => new Toggle();
        }
        
        void OnPathChanged(ChangeEvent<string> evt, int idx)
        {
            _list[idx].path = evt.newValue;
        }
        
        void OnSelectPath(ClickEvent evt, PathSelectorArgs args)
        {
            var folder = Path.Combine(Application.dataPath, "BundleAssets").Replace("\\", "/");
            string path = EditorUtility.OpenFolderPanel("SelectFolder", folder, "").Replace("\\", "/");
                    
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
                    
            args.textField.value = relPath;
            _list[args.idx].path = relPath;
        }

        struct PathSelectorArgs
        {
            public TextField textField;
            public int idx;
        }
        
        void OnDirTypeChanged(ChangeEvent<Enum> evt, int idx)
        {
            _list[idx].packDirType = (EABPackDir) evt.newValue;
        }
        
        void OnCompressTypeChanged(ChangeEvent<Enum> evt, int idx)
        {
            _list[idx].packCompressType = (EABCompress) evt.newValue;
        }
        
        void OnUpdateTypeChanged(ChangeEvent<Enum> evt, int idx)
        {
            _list[idx].updateType = (EABUpdate) evt.newValue;
        }
        
        void OnMd5Changed(ChangeEvent<bool> evt, int idx)
        {
            _list[idx].md5 = evt.newValue;
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
                
                textField.UnregisterCallback<ChangeEvent<string>, int>(OnPathChanged);
                textField.RegisterCallback<ChangeEvent<string>, int>(OnPathChanged, i);
        
                selector.UnregisterCallback<ClickEvent, PathSelectorArgs>(OnSelectPath);
                selector.RegisterCallback<ClickEvent, PathSelectorArgs>(OnSelectPath, new PathSelectorArgs()
                {
                    textField = textField,
                    idx = i,
                });
                
                textField.value = _list[i].path;
                textField.isReadOnly = true;
            };
            listView.columns["dirType"].bindCell = (element, i) =>
            {
                var ele = (EnumField) element;

                ele.UnregisterCallback<ChangeEvent<Enum>, int>(OnDirTypeChanged);
                ele.RegisterCallback<ChangeEvent<Enum>, int>(OnDirTypeChanged, i);

                ele.value = _list[i].packDirType;
            };
            listView.columns["compressType"].bindCell = (element, i) =>
            {
                var ele = (EnumField) element;

                ele.UnregisterCallback<ChangeEvent<Enum>, int>(OnCompressTypeChanged);
                ele.RegisterCallback<ChangeEvent<Enum>, int>(OnCompressTypeChanged, i);

                ele.value = _list[i].packCompressType;
            };
            listView.columns["updateType"].bindCell = (element, i) =>
            {
                var ele = (EnumField) element;

                ele.UnregisterCallback<ChangeEvent<Enum>, int>(OnUpdateTypeChanged);
                ele.RegisterCallback<ChangeEvent<Enum>, int>(OnUpdateTypeChanged, i);

                ele.value = _list[i].updateType;
            };
            listView.columns["md5"].bindCell = (element, i) =>
            {
                var toggle = (Toggle) element;
                
                toggle.UnregisterCallback<ChangeEvent<bool>, int>(OnMd5Changed);
                toggle.RegisterCallback<ChangeEvent<bool>, int>(OnMd5Changed, i);
                
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
                    updateType = data.updateType,
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
