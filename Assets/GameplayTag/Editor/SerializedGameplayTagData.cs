using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EGF.Editor
{
    public class SerializedGameplayTagData
    {
        private const string RootPropPath = "rootNode";
        private const string SubNodesPropPath = "subNodes";
        
        private readonly SerializedObject _serializedTarget;
        private readonly GameplayTagData _target;

        public SerializedObject SerializedTarget => _serializedTarget;
        public bool IsEditingMultipleObjects => _serializedTarget.isEditingMultipleObjects;

        // create
        public SerializedGameplayTagData(GameplayTagData gameplayTagData)
        {
            _serializedTarget = new SerializedObject(gameplayTagData);
            _target = gameplayTagData;
        }
        
        SerializedProperty AppendArrayElement(SerializedProperty arrayProperty) {
            arrayProperty.InsertArrayElementAtIndex(arrayProperty.arraySize);
            return arrayProperty.GetArrayElementAtIndex(arrayProperty.arraySize - 1);
        }
        
        // 从某个节点开始遍历
        static void Traverse(SerializedProperty nodeProperty, Action<SerializedProperty> visitor)
        {
            if (nodeProperty == null) return;
                
            visitor.Invoke(nodeProperty);
            var subNodes = nodeProperty.FindPropertyRelative(SubNodesPropPath);
            if (!subNodes.isArray) return;
            for (var i = 0; i < subNodes.arraySize; i++)
            {
                var nodeProp = subNodes.GetArrayElementAtIndex(i);
                Traverse(nodeProp, visitor);
            }
        }
        
        public void AddTag(string newTag)
        {
            var tagHash = GameplayTagUtils.GetTagHashFromString(newTag);
            var length = tagHash.Length;

            SerializedProperty currentProperty = _serializedTarget.FindProperty(RootPropPath);
            for (var depth = 0; depth < length; depth++)
            {
                var hasDesiredNodeAtDepth = false;
                var subNodes = currentProperty.FindPropertyRelative(SubNodesPropPath);
                // 当前深度下是否已存在所需节点
                for (int i = 0; i < subNodes.arraySize; i++)
                {
                    var nodeProp = subNodes.GetArrayElementAtIndex(i);
                    if(nodeProp.managedReferenceValue != null && GetTagHashAtDepth(nodeProp,depth) != tagHash[depth]) continue;
                    
                    hasDesiredNodeAtDepth = true;
                    currentProperty = nodeProp;
                    break;
                }
                
                if (hasDesiredNodeAtDepth) continue;
                {
                    // 创建当前深度所需的节点
                    var node = GTagTrieNode.CreateFromTag(tagHash, newTag, depth);
                    subNodes = currentProperty.FindPropertyRelative(SubNodesPropPath);

                    var adding = AppendArrayElement(subNodes);
                    adding.managedReferenceValue = node;
                    _serializedTarget.ApplyModifiedProperties();
                    currentProperty = adding;
                }
            }

            _serializedTarget.ApplyModifiedProperties();
        }

        public void RemoveTag(GameplayTagHash tagHash)
        {
            var length = tagHash.Length;
            
            SerializedProperty currentProperty = _serializedTarget.FindProperty(RootPropPath);
            int depth = 0;
            bool hasDesiredNodeAtDepth;
            do
            {
                hasDesiredNodeAtDepth = false;
                var subNodesPropArray = currentProperty.FindPropertyRelative(SubNodesPropPath);
                if(!subNodesPropArray.isArray || subNodesPropArray.arraySize < 1) break;
                for (int i = 0; i < subNodesPropArray.arraySize; i++)
                {
                    var nodeProp = subNodesPropArray.GetArrayElementAtIndex(i);
                    if (GetTagHashAtDepth(nodeProp, depth) != tagHash[depth]) continue;
                    
                    // 移除
                    if (depth + 1 == length)
                    {
                        subNodesPropArray.DeleteArrayElementAtIndex(i);
                        subNodesPropArray.serializedObject.ApplyModifiedProperties();
                        return;
                    }
                    currentProperty = nodeProp;
                    hasDesiredNodeAtDepth = true;
                    break;
                }
                depth++;
            } while (depth < length && hasDesiredNodeAtDepth);
        }
        
        public bool ContainsTagInternal(GameplayTagHash tagHash, out GTagTrieNode nodeInfo)
        {
            var length = tagHash.Length;
            nodeInfo = null;
            if (length < 1) return false;

            SerializedProperty currentProperty = _serializedTarget.FindProperty(RootPropPath);
            int depth = 0;
            bool hasDesiredNodeAtDepth;
            do
            {
                hasDesiredNodeAtDepth = false;
                var subNodesPropArray = currentProperty.FindPropertyRelative(SubNodesPropPath);
                if(!subNodesPropArray.isArray || subNodesPropArray.arraySize < 1) break;
                for (int i = 0; i < subNodesPropArray.arraySize; i++)
                {
                    var nodeProp = subNodesPropArray.GetArrayElementAtIndex(i);
                    if (GetTagHashAtDepth(nodeProp, depth) != tagHash[depth]) continue;
                    
                    currentProperty = nodeProp;
                    hasDesiredNodeAtDepth = true;
                    break;

                }
                depth++;
            } while (depth < length && hasDesiredNodeAtDepth);

            if (hasDesiredNodeAtDepth && currentProperty.managedReferenceValue is GTagTrieNode node)
                nodeInfo = node;
            return hasDesiredNodeAtDepth;
        }

        public int GetTagHashAtDepth(SerializedProperty property, int depth)
        {
            return GameplayTagHash.GetTagHashAtDepth(property, depth);
        }
        
        /// 遍历子节点并执行操作
        public void Traverse(Action<SerializedProperty> visitor)
        {
            // 注意 rootNode 本身不能参与
            var subNodes = _serializedTarget.FindProperty($"{RootPropPath}.{SubNodesPropPath}");
            if (!subNodes.isArray) return;
            for (var i = 0; i < subNodes.arraySize; i++)
            {
                var nodeProp = subNodes.GetArrayElementAtIndex(i);
                Traverse(nodeProp, visitor);
            }
        }
    }
}
