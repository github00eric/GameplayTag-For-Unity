using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EGF
{
    // 用于前缀树查询用的多层 hash，最多 4层，再多没必要
    [Serializable]
    public struct GameplayTagHash
    {
        public int hash0;
        public int hash1;
        public int hash2;
        public int hash3;
        
        public const int InValid = 0;
        public bool IsValid => hash0 != 0;
        
        public int Length
        {
            get
            {
                for (int i = 0; i < 4; i++)
                {
                    if (this[i] == InValid)
                        return i;
                }
                return 4;
            }
        }
        
        // 索引器
        public int this[int index]
        {
            get
            {
                return index switch
                {
                    0 => hash0,
                    1 => hash1,
                    2 => hash2,
                    3 => hash3,
                    _ => 0
                };
            }
            set
            {
                switch (index)
                {
                    case 0: hash0 = value; break;
                    case 1: hash1 = value; break;
                    case 2: hash2 = value; break;
                    case 3: hash3 = value; break;
                    // default: throw new ArgumentOutOfRangeException("index", "Index out of range");
                    default: break;
                }
            }
        }

        public int GetDictHashInt()
        {
            return HashCode.Combine(hash0, hash1, hash2, hash3);
        }
        
#if UNITY_EDITOR
        public static int GetTagHashAtDepth(UnityEditor.SerializedProperty property, int depth)
        {
            const string hashProp0 = "hash0";
            const string hashProp1 = "hash1";
            const string hashProp2 = "hash2";
            const string hashProp3 = "hash3";

            var hashProp = property.FindPropertyRelative("hash");
            
            int result = depth switch
            {
                0 => hashProp.FindPropertyRelative(hashProp0).intValue,
                1 => hashProp.FindPropertyRelative(hashProp1).intValue,
                2 => hashProp.FindPropertyRelative(hashProp2).intValue,
                3 => hashProp.FindPropertyRelative(hashProp3).intValue,
                _ => 0
            };

            return result;
        }

        public static GameplayTagHash GetTagHashFromNodeSerializedProperty(UnityEditor.SerializedProperty property)
        {
            var hash = new GameplayTagHash()
            {
                hash0 = GetTagHashAtDepth(property,0),
                hash1 = GetTagHashAtDepth(property, 1),
                hash2 = GetTagHashAtDepth(property, 2),
                hash3 = GetTagHashAtDepth(property, 3),
            };
            return hash;
        }
#endif
    }
}
