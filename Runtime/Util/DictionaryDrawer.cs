#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Elympics
{
    public class DictionaryDrawer<TD, TK, TV> : PropertyDrawer
        where TD : class, IDictionary<TK, TV>, new()
    {
        private const float ButtonWidth = 18f;

        private TD _dictionary;
        private bool _foldout;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            CheckInitialize(property, label);
            return _foldout ? (_dictionary.Count + 1) * 17f : 17f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            CheckInitialize(property, label);

            position.height = 17f;

            var foldoutRect = position;
            foldoutRect.width -= 2 * ButtonWidth;
            EditorGUI.BeginChangeCheck();
            _foldout = EditorGUI.Foldout(foldoutRect, _foldout, label, true);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(label.text, _foldout);

            if (!_foldout)
                return;

            foreach (var (key, value) in _dictionary)
            {
                position.y += 17f;

                var keyRect = position;
                keyRect.width /= 5;
                keyRect.width -= 4;
                EditorGUI.BeginDisabledGroup(true);
                _ = DoField(keyRect, typeof(TK), key);
                EditorGUI.EndDisabledGroup();

                var valueRect = position;
                valueRect.x = position.width / 5 + 15;
                valueRect.width = position.width / 5 * 4;
                _ = DoField(valueRect, typeof(TV), value);
            }
        }

        private void CheckInitialize(SerializedProperty property, GUIContent label)
        {
            if (_dictionary == null)
            {
                var target = property.serializedObject.targetObject;
                _dictionary = fieldInfo.GetValue(target) as TD;
                if (_dictionary == null)
                {
                    _dictionary = new TD();
                    fieldInfo.SetValue(target, _dictionary);
                }

                _foldout = EditorPrefs.GetBool(label.text);
            }
        }

        private static readonly Dictionary<Type, Func<Rect, object, object>> Fields =
            new()
            {
                {typeof(int), (rect, value) => EditorGUI.IntField(rect, (int) value)},
                {typeof(float), (rect, value) => EditorGUI.FloatField(rect, (float) value)},
                {typeof(string), (rect, value) => EditorGUI.TextField(rect, (string) value)},
                {typeof(bool), (rect, value) => EditorGUI.Toggle(rect, (bool) value)},
                {typeof(Vector2), (rect, value) => EditorGUI.Vector2Field(rect, GUIContent.none, (Vector2) value)},
                {typeof(Vector3), (rect, value) => EditorGUI.Vector3Field(rect, GUIContent.none, (Vector3) value)},
                {typeof(Bounds), (rect, value) => EditorGUI.BoundsField(rect, (Bounds) value)},
                {typeof(Rect), (rect, value) => EditorGUI.RectField(rect, (Rect) value)},
            };

        private static T DoField<T>(Rect rect, Type type, T value)
        {
            if (Fields.TryGetValue(type, out var field))
                return (T)field(rect, value);

            if (type.IsEnum)
                return (T)(object)EditorGUI.EnumPopup(rect, (Enum)(object)value);

            if (typeof(UnityObject).IsAssignableFrom(type))
                return (T)(object)EditorGUI.ObjectField(rect, (UnityObject)(object)value, type, true);

            Debug.Log("Type is not supported: " + type);
            return value;
        }
    }
}
#endif
