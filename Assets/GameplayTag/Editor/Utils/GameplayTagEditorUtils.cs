using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace EGF.Editor
{
    public static class GameplayTagEditorUtils
    {
        public static GameplayTagData GameplayTagData
        {
            get
            {
                if (!GameplayTagUtils.GameplayTagData)
                    Init();
                return GameplayTagUtils.GameplayTagData;
            }
        }
        
        // 载入失败则创建
        static void Init()
        {
            if (GameplayTagUtils.GameplayTagData) return;
            
            if (!Directory.Exists(GameplayTagUtils.DataPath))
                Directory.CreateDirectory(GameplayTagUtils.DataPath);

            var newData = ScriptableObject.CreateInstance<GameplayTagData>();
            AssetDatabase.CreateAsset(newData, $"{GameplayTagUtils.DataPath}/{GameplayTagUtils.FileName}.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            GameplayTagUtils.SetGameplayTagData(newData);
            GameplayTagUtils.GameplayTagData.Init();
        }
        
        // TODO：撤销和重做操作......

        #region Get Object From Property

        /// 反射获取 SerializedProperty 字段的实际对象
        public static object GetObjectFromProperty(SerializedProperty property)
        {
            var rootObject = property.serializedObject.targetObject;
            var propertyPath = property.propertyPath;

            string[] pathElements = propertyPath.Split('.');
            
            // 解析字段路径和类型
            List<FieldStringData> pathParseData = new List<FieldStringData>();
            int skipElement = 0;
            for (int i = 0; i < pathElements.Length; i++)
            {
                // 跳过集合的元素
                if (skipElement > 0)
                {
                    skipElement--;
                    continue;
                }
                
                // 集合字段
                if (i + 1 < pathElements.Length && pathElements[i + 1] == "Array")
                {
                    skipElement = 2;
                    var dataIndex = pathElements[i + 2];
                    dataIndex = dataIndex.Replace("data[", "");
                    dataIndex = dataIndex.Replace("]", "");
                    
                    var arrayIndex = int.Parse(dataIndex);
                    pathParseData.Add(new FieldStringData(FieldStringData.ArrayFieldInt, pathElements[i], arrayIndex));
                    continue;
                }
                
                pathParseData.Add(new FieldStringData(FieldStringData.FieldInt, pathElements[i], 0));
            }

            // 获取字段
            object cacheObject = rootObject;
            Type cacheType = rootObject.GetType();
            foreach (var parseData in pathParseData)
            {
                FieldInfo getField;
                if (parseData.DataTypeInt == FieldStringData.ArrayFieldInt)
                {
                    var parseName = parseData.DataName;
                    var parseIndex = parseData.ArrayIndex;
                    getField = cacheType.GetField(parseName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (getField == null) return null;
                    
                    var listObj = getField.GetValue(cacheObject);
                    if (listObj is not IEnumerable list) return null;
                    
                    cacheObject = GetObjectFromArray(list, parseIndex);
                    cacheType = cacheObject.GetType();
                }
                else
                {
                    var parseName = parseData.DataName;
                    getField = cacheType.GetField(parseName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (getField == null) return null;
                    
                    cacheObject = getField.GetValue(cacheObject);
                    cacheType = cacheObject.GetType();
                }
            }
            
            return cacheObject;
        }
        
        private static object GetObjectFromArray(IEnumerable array, int index)
        {
            int id = 0;
            foreach (var data in array)
            {
                if (id == index)
                {
                    return data;
                }

                id++;
            }

            Debug.LogWarning($"Function [GetObjectFromArray] index is out of range. index = {index}");
            return null;
        }

        private readonly struct FieldStringData
        {
            public const int FieldInt = 0;
            public const int ArrayFieldInt = 1;
            
            public readonly int DataTypeInt;
            public readonly string DataName;
            public readonly int ArrayIndex;

            public FieldStringData(int initDataTypeInt, string initDataName, int initArrayIndex)
            {
                DataTypeInt = initDataTypeInt;
                DataName = initDataName;
                ArrayIndex = initArrayIndex;
            }
        }

        #endregion
    }
}
