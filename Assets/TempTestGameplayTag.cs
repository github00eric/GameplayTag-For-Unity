using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EGF
{
    public class TempTestGameplayTag : MonoBehaviour
    {
        public GameplayTagContainer tagContainer1;
        public GameplayTagContainer tagContainer2;

        public string nameAdd;
        public string nameContain;

        private GameplayTagHash hashAdd;
        private GameplayTagHash hashContain;
        
        [Serializable]
        public class TestData
        {
            public int a;
            public int b;
            public int c;
            public int d;
            public int e;
            public string msgA;
            public string msgB;
            public string msgC;
            public string msgD;
            public GameplayTagContainer containerA;
            public GameplayTagContainer containerB;
            public GameplayTagContainer containerC;
            public GameplayTagContainer containerD;
        }

        public TestData[] dataList1;
        public List<TestData> dataList2;

        [ContextMenu("Test Add")]
        public void Test1()
        {
            hashAdd = GameplayTagUtils.GetTagHashFromString(nameAdd);
            tagContainer1.AddTagRuntime(hashAdd);
            
            hashContain = GameplayTagUtils.GetTagHashFromString(nameContain);
            tagContainer2.AddTagRuntime(hashContain);
        }

        [ContextMenu("Test 1 contains 2")]
        public void Test2()
        {
            Debug.Log("Contain Hash: " + tagContainer1.Contains(hashContain));
            Debug.Log("Contain container: " + tagContainer1.Contains(tagContainer2));
            Debug.Log("Contain containerAny: " + tagContainer1.ContainsAny(tagContainer2));
        }

        [ContextMenu("Test remove")]
        public void Test3()
        {
            hashAdd = GameplayTagUtils.GetTagHashFromString(nameAdd);
            tagContainer1.RemoveTagRuntime(hashAdd);
            
            hashContain = GameplayTagUtils.GetTagHashFromString(nameContain);
            tagContainer2.RemoveTagRuntime(hashContain);
        }

        [ContextMenu("Test Clear")]
        public void Test4()
        {
            tagContainer1.ClearTagRuntime();
            tagContainer2.ClearTagRuntime();
        }

        [ContextMenu("Test Equal")]
        public void Test5()
        {
            hashAdd = GameplayTagUtils.GetTagHashFromString(nameAdd);
            hashContain = GameplayTagUtils.GetTagHashFromString(nameContain);
            
            Debug.Log(hashAdd.Equals(hashContain));
        }
    }
}
