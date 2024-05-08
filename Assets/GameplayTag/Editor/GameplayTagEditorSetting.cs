using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EGF.Editor
{
    public class GameplayTagEditorSetting : ScriptableObject
    {
        #region Editor Setting
        
        static GameplayTagEditorSetting FindSettings(){
            var guids = AssetDatabase.FindAssets($"t:{nameof(GameplayTagEditorSetting)}");
            if (guids.Length > 1) {
                Debug.LogWarning($"Found multiple settings files, using the first.");
            }

            switch (guids.Length) {
                case 0:
                    return null;
                default:
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    return AssetDatabase.LoadAssetAtPath<GameplayTagEditorSetting>(path);
            }
        }

        internal static GameplayTagEditorSetting GetOrCreateSettings() {
            var settings = FindSettings();
            if (settings == null) {
                settings = ScriptableObject.CreateInstance<GameplayTagEditorSetting>();
                AssetDatabase.CreateAsset(settings, "Assets/GameplayTag/GameplayTagEditorSetting.asset");
                AssetDatabase.SaveAssets();
            }
            return settings;
        }
        
        internal static SerializedObject GetSerializedSettings() {
            return new SerializedObject(GetOrCreateSettings());
        }
        
        #endregion
        
        // public VisualTreeAsset uxml;
        // public StyleSheet uss;
        // public TextAsset text;
        public bool CheckSettingReady()
        {
            if (styleUss 
                && tagEditorXml && tagDataElementXml 
                && tagContainer && tagContainerElement) return true;
            
            Debug.Log("lack of some setting, check out GameplayTagEditorSetting asset and reopen tag window.");
            return false;

        }

        public StyleSheet styleUss;
        
        [Header("Editor")]
        public VisualTreeAsset tagEditorXml;
        public VisualTreeAsset tagDataElementXml;
        
        [Header("Runtime Editor")]
        public VisualTreeAsset tagContainer;
        public VisualTreeAsset tagContainerElement;
    }
}
