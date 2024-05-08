using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EGF.Editor
{
    [CustomPropertyDrawer(typeof(GameplayTagContainer))]
    public class GameplayTagContainerDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var target = GameplayTagEditorUtils.GetObjectFromProperty(property) as GameplayTagContainer;

            /*
             * 在显示元素过多，超出父元素显示范围的部分情况下，PropertyDrawer 的部分内部变量可能会发生混乱，
             * 这可能是由于Unity使用对象池回收出了一些问题，最终导致 PropertyDrawer 引用和操作同一类型的另一个 PropertyDrawer的变量
             * 为避免这种情况，将不允许混淆的变量保存在单独封装的 VisualElement，避免对象池回收复用导致的引用混乱。
             */
            var root = new GameplayTagContainerView(property.displayName, target, property);
            return root;
        }
    }
}
