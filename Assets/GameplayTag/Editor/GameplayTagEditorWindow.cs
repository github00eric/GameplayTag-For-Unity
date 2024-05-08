using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EGF.Editor
{
    public class GameplayTagEditorWindow : EditorWindow
    {
        private VisualElement _tagContainer;
        private ToolbarButton _toolbarButton;
        private TextField _inputTag;

        // private SerializedObject _serializedObject;
        private SerializedGameplayTagData _serializedData;
        private GameplayTagData _data;

        private const string RootPropPath = "rootNode";
        private const string SubNodesPropPath = "subNodes";
        
        [MenuItem("Tools/GameplayTagsEditor")]
        public static void OpenWindow() {
            GameplayTagEditorWindow wnd = GetWindow<GameplayTagEditorWindow>();
            wnd.titleContent = new GUIContent("Gameplay Tag Editor");
            wnd.minSize = new Vector2(250, 400);
        }
        
        private void CreateGUI()
        {
            var data = GameplayTagEditorUtils.GameplayTagData;
            var editorSetting = GameplayTagEditorSetting.GetOrCreateSettings();
            if (!editorSetting.CheckSettingReady()) return;

            _serializedData = new SerializedGameplayTagData(data);
            _data = data;
            if(_serializedData.IsEditingMultipleObjects) return;
            
            // editor window root
            VisualElement root = rootVisualElement;
            editorSetting.tagEditorXml.CloneTree(root);
            _toolbarButton = root.Q<ToolbarButton>();
            _inputTag = root.Q<TextField>();
            _toolbarButton.clickable.clicked += OnAddTagClicked;
            _inputTag.value = "new tag here";
            // tag Container
            _tagContainer = new VisualElement();
            root.Add(_tagContainer);

            _tagContainer.Unbind();
            // method 1: （无法检查到子节点的变化）
            // _tagContainer.TrackPropertyValue(_serializedObject.FindProperty(RootPropPath), RefreshTagContainerView);
            // method 2: （检查子节点变化有效，但可能会检测到无关数据的变化。可用事件来替代 Track触发界面更新）
            _tagContainer.TrackSerializedObjectValue(_serializedData.SerializedTarget, RefreshTagContainerView);

            RefreshTagContainerView(_serializedData.SerializedTarget);
        }
        
        private void OnAddTagClicked()
        {
            Debug.Log($"Add gameplay tag: {_inputTag.value}");
            _serializedData.AddTag(_inputTag.value);
        }
        
        private void RefreshTagContainerView(SerializedObject serializedObject)
        {
            // 遍历操作
            void Visitor(SerializedProperty obj)
            {
                VisualElement element = new VisualElement();
                GameplayTagEditorSetting.GetOrCreateSettings().tagDataElementXml.CloneTree(element);
                // 展示标签
                var tagDepth = obj.FindPropertyRelative("depth").intValue;
                var tagName = obj.FindPropertyRelative("name").stringValue;
                if (tagDepth > 0)
                {
                    tagName = tagName.Split('.')[tagDepth];
                    var prefix = "";
                    for (int i = 0; i < tagDepth; i++)
                        prefix += "┗━━";      // 制表符
                    tagName = prefix + tagName;
                }
                element.Q<Label>().text = tagName;

                void ClickDelete()
                {
                    Debug.Log($"Delete gameplay tag: {tagDepth}-{tagName}");
                    var hash = new GameplayTagHash()
                    {
                        hash0 = _serializedData.GetTagHashAtDepth(obj,0),
                        hash1 = _serializedData.GetTagHashAtDepth(obj, 1),
                        hash2 = _serializedData.GetTagHashAtDepth(obj, 2),
                        hash3 = _serializedData.GetTagHashAtDepth(obj, 3),
                    };
                    _serializedData.RemoveTag(hash);
                }
                element.Q<Button>().clickable.clicked += ClickDelete;
                _tagContainer.Add(element);
            }
            
            _tagContainer.Clear();
            _serializedData.Traverse(Visitor);
        }
    }
}
