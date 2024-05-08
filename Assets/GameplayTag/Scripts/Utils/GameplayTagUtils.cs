using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace EGF
{
    public static class GameplayTagUtils
    {
#if UNITY_EDITOR
        /*
         * 注意：
         * 该方法会在编辑器每次启动、代码重新编译等情况调用。
         */
        [UnityEditor.InitializeOnLoadMethod]
        public static void EditorInit()
        {
            _gameplayTagData = Resources.Load<GameplayTagData>(FileName);
            if(_gameplayTagData)
                _gameplayTagData.Init();
        }
        
        public static void SetGameplayTagData(GameplayTagData data)
        {
            _gameplayTagData = data;
        }
#endif
        
        public const string DataPath = "Assets/GameplayTag/Resources";
        public const string FileName = "EGF_GameplayTagData";
        
        private static GameplayTagData _gameplayTagData;

        public static GameplayTagData GameplayTagData
        {
            get
            {
                if (_gameplayTagData) return _gameplayTagData;
                
                _gameplayTagData = Resources.Load<GameplayTagData>(FileName);
                if(_gameplayTagData)
                    _gameplayTagData.Init();
                return _gameplayTagData;
            }
        }

        #region Hash
        
        private static int GetHashFromString(string text)
        {
            MD5 md5Hasher = MD5.Create();
            var hashed = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(text));
            return BitConverter.ToInt32(hashed, 0);
        }
        public static GameplayTagHash GetTagHashFromString(string tagText)
        {
            if (string.IsNullOrEmpty(tagText)) return new GameplayTagHash();
            
            string[] tagParts = tagText.Split('.');
            GameplayTagHash tagHash = new GameplayTagHash();

            for (int i = 0; i < Mathf.Min(tagParts.Length, 4); i++)
            {
                tagHash[i] = GetHashFromString(tagParts[i]);
            }

            return tagHash;
        }
        
        #endregion
    }
}
