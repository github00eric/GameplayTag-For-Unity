using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EGF.Editor
{
    public class GameplayTagContainerView : VisualElement
    {
        private const string ShowStyle = "show-detail";
        private const string HideStyle = "hide-detail";
        private const string InvalidTagPrefix = "----";
        private const string InvalidTagText = "-- UnConfigured Tag --";
        
        // 界面引用
        private readonly ScrollView _tagContainer;
        private readonly Button _tagButton;
        
        // 标签元素索引，如果没打开过 detail 下拉界面，可能为 null
        private Dictionary<int, VisualElement> _tagElementRefs;
        private bool _tagContainerShow;
        private bool TagContainerInitialized => _tagElementRefs != null;
        
        // 标签计数
        private int _tagCount;

        // data
        private readonly SerializedProperty _targetPropertyRef;
        private readonly GameplayTagContainer _targetRef;
        private readonly SerializedGameplayTagData _serializedGameplayTagData;

        public GameplayTagContainerView(string displayName, GameplayTagContainer target, SerializedProperty property)
        {
            var editorSetting = GameplayTagEditorSetting.GetOrCreateSettings();
            if (!editorSetting.CheckSettingReady())
            {
                Add(new HelpBox("Invalid editor setting", HelpBoxMessageType.Warning));
                return;
            }
            // get GameplayTagContainer
            if (target == null)
            {
                Add(new HelpBox("Invalid property path", HelpBoxMessageType.Warning));
                return;
            }
            _targetRef = target;
            _targetPropertyRef = property;
            _targetRef.ClearNodeChangeEvent();
            _targetRef.ONNodeAdd += HandleAddTag;
            _targetRef.ONNodeRemove += HandleRemoveTag;
            _targetRef.ONClear += HandleClearTag;
            
            _serializedGameplayTagData = new SerializedGameplayTagData(GameplayTagUtils.GameplayTagData);
            
            // create ui
            editorSetting.tagContainer.CloneTree(this);
            this.Q<Label>().text = displayName;

            _tagContainer = this.Q<ScrollView>();
            ResetTagContainer();
            // 如果项目的 GameplayTagData 发生变化，就收起并刷新 TagContainer
            _tagContainer.TrackSerializedObjectValue(_serializedGameplayTagData.SerializedTarget,
                so => ResetTagContainer());
            
            _tagButton = this.Q<Button>("btn-show-detail");
            _tagButton.clickable.clicked += HideOrShowTagContainer;
            // 统计实际标签数量
            _tagCount = 0;
            _targetRef.Traverse(node =>
            {
                if (node.active)
                    _tagCount++;
            });
            SetTagCount(_tagCount);
        }
        
        private void SetTagCount(int count)
        {
            var countText = count.ToString();
            _tagButton.text = "Active Tag: " + countText;
        }

        private void ResetTagContainer()
        {
            _tagContainerShow = false;
            _tagElementRefs = null;
            RefreshTagContainerShow();
        }
        
        private void HideOrShowTagContainer()
        {
            _tagContainerShow = !_tagContainerShow;
            if (_tagContainerShow && !TagContainerInitialized)
            {
                InitializeTagElements();
            }
            RefreshTagContainerShow();
        }
        
        private void RefreshTagContainerShow()
        {
            if(_tagContainer == null) return;

            if (_tagContainerShow)
            {
                _tagContainer.RemoveFromClassList(HideStyle);
                _tagContainer.AddToClassList(ShowStyle);
            }
            else
            {
                _tagContainer.RemoveFromClassList(ShowStyle);
                _tagContainer.AddToClassList(HideStyle);
            }
        }

        private void RefreshTagElementBackground(VisualElement element, bool showState)
        {
            var tagElement = element.Q<VisualElement>("tag");
            var tagText = tagElement.Q<Label>();

            if (showState)
            {
                tagElement.AddToClassList("contain-tag-on");
                tagElement.RemoveFromClassList("contain-tag-off");
                tagText.AddToClassList("contain-tag-text");
            }
            else
            {
                tagElement.AddToClassList("contain-tag-off");
                tagElement.RemoveFromClassList("contain-tag-on");
                tagText.RemoveFromClassList("contain-tag-text");
            }
        }

        private void ChangeTagToggle(GameplayTagHash hash, bool nextState)
        {
            if (nextState)
                _targetRef.AddTagRuntime(hash);
            else
                _targetRef.RemoveTagRuntime(hash);
        }

        private VisualElement CreateOneTagElement(SerializedProperty elementProp)
        {
            var element = new VisualElement();
            GameplayTagEditorSetting.GetOrCreateSettings().tagContainerElement.CloneTree(element);
            // 标签字符及其展示
            var tagDepth = elementProp.FindPropertyRelative("depth").intValue;
            var tagName = elementProp.FindPropertyRelative("name").stringValue;
            // 前缀空格
            var prefix = "";
            if (tagDepth > 0)
            {
                tagName = tagName.Split('.')[tagDepth];
                for (int i = 0; i < tagDepth; i++)
                    prefix += "        ";      // 制表符
            }
            var space = element.Q<Label>("space");
            space.text = prefix;
            // 标签
            var tagText = element.Q<VisualElement>("tag").Q<Label>();
            tagText.text = tagName;
            // 勾选
            var toggle = element.Q<Toggle>();
            var hash = GameplayTagHash.GetTagHashFromNodeSerializedProperty(elementProp);
            void ToggleSelect(ChangeEvent<bool> evt)
            {
                ChangeTagToggle(hash, evt.newValue);
            }
            toggle.RegisterValueChangedCallback(ToggleSelect);
            toggle.SetValueWithoutNotify(false);
            // 背景
            RefreshTagElementBackground(element, false);
            
            return element;
        }

        /// 未配置 Gameplay Tag Data 的标签数据创建为 Invalid Tag
        private VisualElement CreateInvalidTagElement(GTagRuntimeTrieNode node)
        {
            // 对于不在 GameplayTagData中的数据，作为未配置的标签添加
            var element = new VisualElement();
            GameplayTagEditorSetting.GetOrCreateSettings().tagContainerElement.CloneTree(element);
            // 前缀空格 ---- 标记未配置标签
            var space = element.Q<Label>("space");
            space.text = InvalidTagPrefix;
            // 标签
            var tagText = element.Q<VisualElement>("tag").Q<Label>();
            tagText.text = InvalidTagText;
            // 勾选
            var toggle = element.Q<Toggle>();
            var hash = node.hash;
            void ToggleSelect(ChangeEvent<bool> evt)
            {
                ChangeTagToggle(hash, evt.newValue);
            }
            toggle.RegisterValueChangedCallback(ToggleSelect);
            toggle.SetValueWithoutNotify(node.active);
            // 背景
            RefreshTagElementBackground(element, true);

            return element;
        }

        /// 首次打开详细信息展示时，添加标签所有元素
        private void InitializeTagElements()
        {
            _tagContainer.Clear();
            _tagElementRefs ??= new Dictionary<int, VisualElement>();
            _tagElementRefs.Clear();
            // 添加可选标签，数据来源于 gameplay tag data
            var editorSetting = GameplayTagEditorSetting.GetOrCreateSettings();
            void GetAndAddElement(SerializedProperty elementProp)
            {
                var element = CreateOneTagElement(elementProp);
                _tagContainer.Add(element);
                
                var key = GameplayTagHash.GetTagHashFromNodeSerializedProperty(elementProp).GetDictHashInt();
                _tagElementRefs.Add(key, element);
            }
            _serializedGameplayTagData.Traverse(GetAndAddElement);

            void AddContainerTag(GTagRuntimeTrieNode node)
            {
                var key = node.hash.GetDictHashInt();
                if (_tagElementRefs.ContainsKey(key))
                {
                    var element = _tagElementRefs[key];
                    RefreshTagElementBackground(element, true);
                    element.Q<Toggle>().SetValueWithoutNotify(node.active);
                }
                else
                {
                    // 对于不在 GameplayTagData中的数据，添加已失效的标签
                    var element = CreateInvalidTagElement(node);
                    _tagContainer.Add(element);
                    _tagElementRefs.Add(key, element);
                }
            }
            _targetRef.Traverse(AddContainerTag);
        }

        // 设置脏标记，通知 Unity 储存系统
        void SetDirty()
        {
            if(Application.isPlaying) return;
            
            var ownerObj = _targetPropertyRef.serializedObject.targetObject;
            EditorUtility.SetDirty(ownerObj);
        }

        private void HandleAddTag(GTagRuntimeTrieNode node, bool activeHistory)
        {
            SetDirty();
            var key = node.hash.GetDictHashInt();

            if (activeHistory && !node.active)
            {
                _tagCount--;
                SetTagCount(_tagCount);
            }
            else if (!activeHistory && node.active)
            {
                _tagCount++;
                SetTagCount(_tagCount);
            }
            
            // 处理 detail界面 展示标签
            if (!TagContainerInitialized) return;
            
            if (_tagElementRefs.ContainsKey(key))
            {
                var element = _tagElementRefs[key];
                RefreshTagElementBackground(element, true);
                element.Q<Toggle>().SetValueWithoutNotify(node.active);
            }
            else
            {
                // 对于不在 GameplayTagData中的数据，作为未配置的标签添加
                var element = CreateInvalidTagElement(node);
                _tagContainer.Add(element);
                _tagElementRefs.Add(key, element);
            }
        }

        private void HandleRemoveTag(GTagRuntimeTrieNode arg0)
        {
            SetDirty();
            var key = arg0.hash.GetDictHashInt();
            if (arg0.active)
            {
                _tagCount--;
                SetTagCount(_tagCount);
            }

            // 处理 detail界面 展示标签
            if (!TagContainerInitialized) return;
            
            if (!_tagElementRefs.ContainsKey(key)) return;

            var element = _tagElementRefs[key];
            // 无效标签移除
            var invalid = element.Q<Label>("space").text.Contains('-');
            if (invalid)
            {
                _tagContainer.Remove(element);
                _tagElementRefs.Remove(key);
                return;
            }
            // 勾选
            element.Q<Toggle>().SetValueWithoutNotify(arg0.active);
            // 背景
            RefreshTagElementBackground(element, false);
        }

        private void HandleClearTag()
        {
            SetDirty();
            _tagCount = 0;
            SetTagCount(_tagCount);

            // 处理 detail界面 展示标签
            if (!TagContainerInitialized) return;
            
            var wait2RemoveRefs = new List<int>();
            foreach (var elementPair in _tagElementRefs)
            {
                var key = elementPair.Key;
                var element = elementPair.Value;
                    
                // 无效标签移除
                var invalid = element.Q<Label>("space").text.Contains('-');
                if (invalid)
                {
                    _tagContainer.Remove(element);
                    wait2RemoveRefs.Add(key);
                    continue;
                }
                // 勾选
                element.Q<Toggle>().SetValueWithoutNotify(false);
                // 背景
                RefreshTagElementBackground(element, false);
            }

            foreach (var key in wait2RemoveRefs)
            {
                _tagElementRefs.Remove(key);
            }
        }
    }
}
