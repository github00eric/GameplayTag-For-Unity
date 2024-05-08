using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EGF
{
    /// 用于编辑器配置的游戏性标签，不可重复添加，删除时会完全删除
    [Serializable]
    public class GTagTrieNode
    {
        public int depth;
        public string name;
        public GameplayTagHash hash;
        [SerializeReference] public List<GTagTrieNode> subNodes;

        public GTagTrieNode()
        {
            subNodes = new List<GTagTrieNode>();
        }
        
        public static GTagTrieNode CreateFromTag(GameplayTagHash tagHash, string tag, int copyDepth)
        {
            // hash
            GameplayTagHash newHash = new GameplayTagHash();
            for (int i = 0; i < copyDepth + 1; i++)
                newHash[i] = tagHash[i];
            // tag
            string[] tagParts = tag.Split('.');
            string displayName = string.Join('.', tagParts.Take(copyDepth + 1));
            // create node
            var result = new GTagTrieNode()
            {
                depth = copyDepth,
                name = displayName,
                hash = newHash,
                subNodes = new List<GTagTrieNode>(),
            };
            return result;
        }
    }
    
    /// 运行时阶段的游戏性标签
    [Serializable]
    public class GTagRuntimeTrieNode
    {
        // 有 active 标记的是实际添加的标签，没有则是跟随子节点一同加入的标签
        // 没有子节点，且没有 active 标记的节点会被自动移除
        // 仅 ContainsAll 时使用
        public bool active;
        public GameplayTagHash hash;
        [SerializeReference] public List<GTagRuntimeTrieNode> subNodes;

        public GTagRuntimeTrieNode()
        {
            subNodes = new List<GTagRuntimeTrieNode>();
        }
        
        public static GTagRuntimeTrieNode CreateFromTag(GameplayTagHash tagHash, int copyDepth)
        {
            GameplayTagHash newHash = new GameplayTagHash();
            for (int i = 0; i < copyDepth + 1; i++)
                newHash[i] = tagHash[i];
            
            var result = new GTagRuntimeTrieNode()
            {
                hash = newHash,
                subNodes = new List<GTagRuntimeTrieNode>(),
            };
            return result;
        }
        
        // 注意 rootNode 本身不能参与
        internal static void TraverseTree(GTagRuntimeTrieNode node, Action<GTagRuntimeTrieNode> visitor, Func<GTagRuntimeTrieNode, bool> stopCondition)
        {
            if (node == null || stopCondition.Invoke(node))
                return;
            
            visitor.Invoke(node);
            if (node.subNodes == null) return;
            foreach (var subNode in node.subNodes)
                TraverseTree(subNode, visitor, stopCondition);
        }

    }
}