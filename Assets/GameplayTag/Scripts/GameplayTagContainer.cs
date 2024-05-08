using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

namespace EGF
{
    [Serializable]
    public partial class GameplayTagContainer
    {
        // 序列化数据
        [SerializeField][SerializeReference] private GTagRuntimeTrieNode rootNode = new GTagRuntimeTrieNode();
        private GTagRuntimeTrieNode Root => rootNode ??= new GTagRuntimeTrieNode();
        
        #region Editor Event
#if UNITY_EDITOR
        // 节点变动事件（更新编辑器界面）
        public event Action<GTagRuntimeTrieNode, bool> ONNodeAdd; // 添加或改变节点的 active, bool 值记录 active 修改前的状态
        public event Action<GTagRuntimeTrieNode> ONNodeRemove;    // 移除节点
        public event Action ONClear;                              // 清空节点
#endif
        [Conditional("UNITY_EDITOR")]
        public void ClearNodeChangeEvent()
        {
#if UNITY_EDITOR
            ONNodeAdd = null;
            ONNodeRemove = null;
            ONClear = null;
#endif
        }
        
        [Conditional("UNITY_EDITOR")]
        private void SendNodeAddEvent(GTagRuntimeTrieNode node, bool activeHistory)
        {
#if UNITY_EDITOR
            if(node == null) return;
            ONNodeAdd?.Invoke(node, activeHistory);
#endif
        }

        [Conditional("UNITY_EDITOR")]
        private void SendNodeRemoveEvent(GTagRuntimeTrieNode node)
        {
#if UNITY_EDITOR
            if(node == null) return;
            ONNodeRemove?.Invoke(node);
#endif
        }

        [Conditional("UNITY_EDITOR")]
        private void SendClearEvent()
        {
#if UNITY_EDITOR
            ONClear?.Invoke();
#endif
        }
        
        #endregion Editor Event
        
        // 从某个节点开始遍历
        static void Traverse(GTagRuntimeTrieNode node, Action<GTagRuntimeTrieNode> visitor)
        {
            if (node == null) return;
                
            visitor.Invoke(node);
            var subNodes = node.subNodes;
            if(subNodes == null || subNodes.Count < 1) return;
            foreach (var nodeProp in subNodes)
                Traverse(nodeProp, visitor);
        }
        
        /// 检查节点是否失效需要自动移除
        private bool NeedAutoRemove(GTagRuntimeTrieNode tagNode)
        {
            return !tagNode.active && (tagNode.subNodes == null || tagNode.subNodes.Count == 0);
        }
        
        // HACK：待验证
        public void AddTagRuntime(GameplayTagHash tagHash)
        {
            if(!tagHash.IsValid) return;
            
            var length = tagHash.Length;
            var targetDepth = length - 1;
            
            GTagRuntimeTrieNode current = Root;
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

                if (hasDesiredNodeAtDepth)
                {
                    if (depth == targetDepth)
                    {
                        var historyActive = current.active;
                        current.active = true;
                        SendNodeAddEvent(current, historyActive);
                    }
                    continue;
                }
                
                // 创建当前深度所需的节点
                var newNode = GTagRuntimeTrieNode.CreateFromTag(tagHash, depth);
                if (depth == targetDepth)
                    newNode.active = true;
                current.subNodes.Add(newNode);
                SendNodeAddEvent(newNode, false);
                current = newNode;
            }
            
        }
        
        // HACK：待验证
        public void RemoveTagRuntime(GameplayTagHash tagHash)
        {
            if(!tagHash.IsValid) return;
            
            var length = tagHash.Length;
            int depth = 0;
            GTagRuntimeTrieNode[] cache = new GTagRuntimeTrieNode[length];
            GTagRuntimeTrieNode current = Root;
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
                
                if (!hasDesiredNodeAtDepth) continue;
                cache[depth] = current;
                depth++;

            } while (depth < length && hasDesiredNodeAtDepth);
            var removeIndex = depth - 1;

            // 找出目标才移除
            if (!hasDesiredNodeAtDepth) return;
            for (int i = removeIndex; i >= 0; i--)
            {
                var removingNode = cache[i];
                
                // 目标标记移除
                if (i == removeIndex)
                {
                    var historyActive = removingNode.active;
                    removingNode.active = false;
                    SendNodeAddEvent(removingNode, historyActive);
                }
                // 自动清理无效节点
                if (!NeedAutoRemove(removingNode)) continue;
                var parentNode = i - 1 < 0 ? Root : cache[i - 1];
                parentNode.subNodes.Remove(removingNode);
                SendNodeRemoveEvent(removingNode);
            }
            
            // // 目标标记移除
            // var changeNode = cache[depth];
            // var historyActive = changeNode.active;
            // changeNode.active = false;
            // SendNodeAddEvent(changeNode, historyActive);
            //
            // // 自动清理目标父级未标记节点
            // while (depth > 0)
            // {
            //     var checking = cache[depth];
            //     if (!NeedAutoRemove(checking)) break;
            //         
            //     cache[depth - 1].subNodes.Remove(checking);
            //     SendNodeRemoveEvent(checking);
            //     depth--;
            // }
            // // 检查和移除最初节点
            // if (NeedAutoRemove(cache[0]))
            // {
            //     var removingNode = cache[0];
            //     Root.subNodes.Remove(removingNode);
            //     SendNodeRemoveEvent(removingNode);
            // }
        }

        public void ClearTagRuntime()
        {
            Root.subNodes.Clear();
            SendClearEvent();
        }
        
        public bool IsEmpty()
        {
            return Root.subNodes.Count == 0;
        }

        /// 单个标签包含检查
        public bool Contains(GameplayTagHash tagHash)
        {
            var length = tagHash.Length;

            GTagRuntimeTrieNode current = Root;
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
            
            return hasDesiredNodeAtDepth;
        }

        public bool Contains(IEnumerable<GameplayTagHash> tags)
        {
            var result = false;
            foreach (var tagHash in tags)
            {
                result = Contains(tagHash);
                if(!result)
                    break;
            }

            return result;
        }
        
        public bool Contains(GameplayTagContainer selector)
        {
            var stopTraverse = false;
            var result = false;

            void Visitor(GTagRuntimeTrieNode node)
            {
                if(!node.active) return;
                result = this.Contains(node.hash);
                if (!result)
                    stopTraverse = true;
            }
            // 注意 rootNode 本身不能参与
            foreach (var check in selector.Root.subNodes)
                GTagRuntimeTrieNode.TraverseTree(check, Visitor, (node) => stopTraverse);
            return result;
        }

        public bool ContainsAny(IEnumerable<GameplayTagHash> tags)
        {
            var result = false;
            foreach (var tagHash in tags)
            {
                result = Contains(tagHash);
                if(result)
                    break;
            }

            return result;
        }

        public bool ContainsAny(GameplayTagContainer selector)
        {
            var stopTraverse = false;
            var result = false;

            void Visitor(GTagRuntimeTrieNode node)
            {
                result = Contains(node.hash);
                if (result)
                    stopTraverse = true;
            }
            
            // 注意 rootNode 本身不能参与
            foreach (var check in selector.Root.subNodes)
                GTagRuntimeTrieNode.TraverseTree(check, Visitor, (node) => stopTraverse);
            return result;
        }
        
        /// 遍历子节点并执行操作
        public void Traverse(Action<GTagRuntimeTrieNode> visitor)
        {
            // 注意 rootNode 本身不能参与
            var subNodes = Root.subNodes;
            if(subNodes == null || subNodes.Count < 1) return;
            foreach (var nodeProp in subNodes)
                Traverse(nodeProp, visitor);
        }
    }
}
