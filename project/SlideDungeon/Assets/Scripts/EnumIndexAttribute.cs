using System;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Enum をキーとして配列を表示するための属性クラス
/// 配列の各要素に Enum 名をラベルとして表示できる
/// </summary>
public class EnumIndexAttribute : PropertyAttribute
{
    // Enum の名前一覧を保持する配列
    private string[] _names;

    /// <summary>
    /// コンストラクタ
    /// 引数で受け取った Enum 型から名前一覧を取得する
    /// </summary>
    /// <param name="enumType">表示に使用する Enum の型</param>
    public EnumIndexAttribute(Type enumType)
    {
        // Enum.GetNames によって
        // enum の要素名（例: Idle, Run, Jump）を string[] として取得
        _names = Enum.GetNames(enumType);
    }

#if UNITY_EDITOR
    /// <summary>
    /// EnumIndexAttribute 用の PropertyDrawer
    /// Inspector 表示をカスタマイズするクラス
    /// </summary>
    [CustomPropertyDrawer(typeof(EnumIndexAttribute))]
    private class Drawer : PropertyDrawer
    {
        /// <summary>
        /// Inspector に表示する GUI を描画する
        /// </summary>
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            // 属性本体から Enum 名の配列を取得
            EnumIndexAttribute enumIndex = (EnumIndexAttribute)attribute;
            string[] names = enumIndex._names;

            /*
               property.propertyPath の例:
                 "statusList.Array.data[2]"
             
               この文字列から [2] の部分を取り出して
               配列のインデックスとして使用する
             */

            // [, ] で分割 → 数字部分だけを取り出す
            string indexText = property.propertyPath
                .Split('[', ']')
                .Last(s => !string.IsNullOrEmpty(s));

            // インデックス番号に変換
            int index = int.Parse(indexText);

            // Enum の要素数以内であれば
            // ラベル名を Enum 名に差し替える
            if (index < names.Length)
            {
                label.text = names[index];
            }

            // 通常の PropertyField を描画
            // includeChildren = true にすることで
            // 配列要素がクラスや構造体でも展開表示される
            EditorGUI.PropertyField(rect, property, label, includeChildren: true);
        }

        /// <summary>
        /// プロパティの高さを取得
        /// 子要素を含めた高さをそのまま返す
        /// </summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, includeChildren: true);
        }
    }
#endif
}