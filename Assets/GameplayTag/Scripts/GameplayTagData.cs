using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EGF
{
    /*
     * 该信息用于向编辑器提供可使用的标签，并用标签的文字来快速选中指定的标签哈希值
     * 由于删除了引用信息，在修改标签信息时，不会同步修改已配置好的文件中的标签，会引发大量的标签失效。
     * 
     * 只要文字相同就能生成相同哈希值。
     * 可以在代码中结合 public const string XXX 安全引用标签文字，方便后续的修改。
     */
    /// 记录项目的所有可选游戏性标签信息。
    public class GameplayTagData : ScriptableObject
    {
        [SerializeReference]
        public GTagTrieNode rootNode;

        public GameplayTagData()
        {
            rootNode = new GTagTrieNode();
        }
        
        public void Init()
        {
            // Debug.LogWarning("Gameplay Tag Data Init.");
            // HACK：排序整理各个节点......
        }

        public void AddTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;
            AddTagInternal(tag);
        }
        
        public void RemoveTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;
            RemoveTagInternal(tag);
        }

        // Editor Only
        public bool ContainsTag(string tag)
        {
            return !string.IsNullOrEmpty(tag) && ContainsTagInternal(tag);
        }
        public bool ContainsTag(GameplayTagHash tagHash)
        {
            return tagHash.Length > 0 && ContainsTagInternal(tagHash, out var node);
        }
        public bool ContainsTag(GameplayTagHash tagHash, out GTagTrieNode node)
        {
            node = null;
            return tagHash.Length > 0 && ContainsTagInternal(tagHash, out node);
        }

        private void AddTagInternal(string tag)
        {
            var tagHash = GameplayTagUtils.GetTagHashFromString(tag);
            var length = tagHash.Length;
            
            GTagTrieNode current = rootNode;
            for (var depth = 0; depth < length; depth++)
            {
                var hasDesiredNodeAtDepth = false;
                // 当前深度下是否已存在所需节点
                foreach (var node in current.subNodes)
                {
                    if (node.hash[depth] != tagHash[depth]) continue;
                    
                    hasDesiredNodeAtDepth = true;
                    current = node;
                    break;
                }

                if (hasDesiredNodeAtDepth) continue;
                
                {
                    // 创建当前深度所需的节点
                    var node = GTagTrieNode.CreateFromTag(tagHash, tag, depth);
                    current.subNodes.Add(node);
                    current = node;
                }
            }
        }

        private void RemoveTagInternal(string tag)
        {
            var hash = GameplayTagUtils.GetTagHashFromString(tag);
            RemoveTagInternal(hash);
        }
        private void RemoveTagInternal(GameplayTagHash tagHash)
        {
            var length = tagHash.Length;
            
            GTagTrieNode current = rootNode;
            int depth = 0;
            bool hasDesiredNodeAtDepth;
            do
            {
                hasDesiredNodeAtDepth = false;
                if(current.subNodes == null || current.subNodes.Count == 0) break;
                List<GTagTrieNode> wait2Check = new List<GTagTrieNode>(current.subNodes);
                foreach (var node in wait2Check)
                {
                    if (node.hash[depth] != tagHash[depth]) continue;
                    
                    // 移除
                    if (depth + 1 == length)
                    {
                        current.subNodes.Remove(node);
                        return;
                    }
                    current = node;
                    hasDesiredNodeAtDepth = true;
                    break;
                }
                depth++;
            } while (depth < length && hasDesiredNodeAtDepth);
        }
        
        private bool ContainsTagInternal(string tag)
        {
            var tagHash = GameplayTagUtils.GetTagHashFromString(tag);
            return ContainsTagInternal(tagHash, out var node);
        }
        private bool ContainsTagInternal(GameplayTagHash tagHash, out GTagTrieNode nodeInfo)
        {
            var length = tagHash.Length;

            GTagTrieNode current = rootNode;
            int depth = 0;
            bool hasDesiredNodeAtDepth;
            do
            {
                hasDesiredNodeAtDepth = false;
                if(current.subNodes == null || current.subNodes.Count == 0) break;
                foreach (var node in current.subNodes)
                {
                    if (node.hash[depth] != tagHash[depth]) continue;
                    
                    current = node;
                    hasDesiredNodeAtDepth = true;
                    break;
                }
                depth++;
            } while (depth < length && hasDesiredNodeAtDepth);
            
            nodeInfo = hasDesiredNodeAtDepth? current: null;
            return hasDesiredNodeAtDepth;
        }
    }
}
