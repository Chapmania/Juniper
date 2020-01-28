using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

using UnityEditor;

using UnityEngine;

namespace Juniper
{
    public static class SerializedPropertyExt
    {
        public static void SetValue<T>(this SerializedProperty property, T value)
        {
            if (property is null)
            {
                throw new System.ArgumentNullException(nameof(property));
            }

            var obj = GetObject(property);
            if (obj != default
                && obj.Parent != default
                && obj.Object != default)
            {
                obj.Field.SetValue(obj.Parent, value);
            }
        }

        public static T GetObject<T>(this SerializedProperty property)// where T : new()
        {
            if (property is null)
            {
                throw new System.ArgumentNullException(nameof(property));
            }

            var obj = GetObject(property);
            if (obj == default || obj.Object == default)
            {
                return default;
            }
            else
            {
                return (T)obj.Object;
            }
        }

        public static T GetScriptableObject<T>(this SerializedProperty property) where T : ScriptableObject
        {
            if (property is null)
            {
                throw new System.ArgumentNullException(nameof(property));
            }

            var obj = GetObject(property);
            if (obj == default || obj.Object == default)
            {
                return ScriptableObject.CreateInstance<T>();
            }
            else
            {
                return (T)obj.Object;
            }
        }

        /// <summary>
        /// The array index pattern.
        /// </summary>
        private static readonly Regex arrayIndexPattern = new Regex("data\\[(\\w+)\\]", RegexOptions.Compiled);

        private static (FieldInfo Field, object Object, object Parent) GetObject(SerializedProperty property)
        {
            object head = property.serializedObject.targetObject;
            var parts = property.propertyPath.Split('.');
            for (var i = 0; i < parts.Length && head != null; ++i)
            {
                var part = parts[i];
                if (part == "Array")
                {
                    var indexStr = parts[++i];
                    var match = arrayIndexPattern.Match(indexStr);
                    var index = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                    var arr = (object[])head;
                    if (0 <= index && index < arr.Length)
                    {
                        head = arr[index];
                    }
                    else
                    {
                        head = null;
                    }
                }
                else
                {
                    var type = head.GetType();
                    var field = type.GetField(part, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var parent = head;
                    head = field.GetValue(head);
                    if (i == parts.Length - 1)
                    {
                        return (field, head, parent);
                    }
                }
            }

            return default;
        }
    }
}