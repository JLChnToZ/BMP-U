using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using UnityObject = UnityEngine.Object;

namespace JLChnToZ.Toolset.Editor.ScriptTester {
    enum PropertyType {
        Unknown,
        Bool,
        Enum,
        Integer,
        Long,
        Single,
        Double,
        Vector2,
        Vector3,
        Vector4,
        Quaterion,
        Color,
        Rect,
        Bounds,
        Curve,
        String,
        Object,
        Array
    }

    struct ComponentMethod {
        public ConstructorInfo ctorInfo;
        public MethodInfo method;
        public object target;
    }

    struct ComponentFields {
        public FieldInfo field;
        public PropertyInfo property;
        public UnityObject target;
    }

    interface IReflectorDrawer {
        void Draw();
        bool AllowPrivateFields { get; set; }
        bool AllowObsolete { get; set; }
        bool Changed { get; }
        object Value { get; }
        MemberInfo Info { get; }
        event Action OnRequireRedraw;
    }

    public static class Helper {
        internal static readonly Dictionary<Type, PropertyType> propertyTypeMapper = new Dictionary<Type, PropertyType>();
        static double clickTime;

        internal static void InitPropertyTypeMapper() {
            if(propertyTypeMapper.Count > 0)
                return;
            propertyTypeMapper.Add(typeof(string), PropertyType.String);
            propertyTypeMapper.Add(typeof(bool), PropertyType.Bool);
            propertyTypeMapper.Add(typeof(byte), PropertyType.Integer);
            propertyTypeMapper.Add(typeof(sbyte), PropertyType.Integer);
            propertyTypeMapper.Add(typeof(ushort), PropertyType.Integer);
            propertyTypeMapper.Add(typeof(short), PropertyType.Integer);
            propertyTypeMapper.Add(typeof(uint), PropertyType.Integer);
            propertyTypeMapper.Add(typeof(int), PropertyType.Integer);
            propertyTypeMapper.Add(typeof(ulong), PropertyType.Long);
            propertyTypeMapper.Add(typeof(long), PropertyType.Long);
            propertyTypeMapper.Add(typeof(float), PropertyType.Single);
            propertyTypeMapper.Add(typeof(double), PropertyType.Double);
            propertyTypeMapper.Add(typeof(Vector2), PropertyType.Vector2);
            propertyTypeMapper.Add(typeof(Vector3), PropertyType.Vector3);
            propertyTypeMapper.Add(typeof(Vector4), PropertyType.Vector4);
            propertyTypeMapper.Add(typeof(Quaternion), PropertyType.Quaterion);
            propertyTypeMapper.Add(typeof(Color), PropertyType.Color);
            propertyTypeMapper.Add(typeof(Rect), PropertyType.Rect);
            propertyTypeMapper.Add(typeof(Bounds), PropertyType.Bounds);
            propertyTypeMapper.Add(typeof(AnimationCurve), PropertyType.Curve);
            propertyTypeMapper.Add(typeof(UnityObject), PropertyType.Object);
            propertyTypeMapper.Add(typeof(IList<>), PropertyType.Array);
        }

        static readonly Hashtable storedState = new Hashtable();

        internal static void StoreState(object key, object value) {
            if(storedState.ContainsKey(key))
                storedState[key] = value;
            else
                storedState.Add(key, value);
        }

        internal static T GetState<T>(object key, T defaultValue = default(T)) {
            return storedState.ContainsKey(key) ? (T)storedState[key] : defaultValue;
        }

        internal static void ReadOnlyLabelField(string label, string value) {
            if(value.Contains('\r') || value.Contains('\n')) {
                EditorGUILayout.PrefixLabel(label);
                EditorGUILayout.SelectableLabel(value, EditorStyles.textArea);
            } else {
                EditorGUILayout.PrefixLabel(label);
                EditorGUILayout.SelectableLabel(value, EditorStyles.textField);
            }
        }

        internal static Rect ScaleRect(Rect source,
            float xScale = 0, float yScale = 0, float widthScale = 1, float heightScale = 1,
            float offsetX = 0, float offsetY = 0, float offsetWidth = 0, float offsetHeight = 0) {
            return new Rect(
                source.x + source.width * xScale + offsetX,
                source.y + source.height * yScale + offsetY,
                source.width * widthScale + offsetWidth,
                source.height * heightScale + offsetHeight
            );
        }

        internal static string GetMemberName(MemberInfo member, bool simplifed = false) {
            var ret = new StringBuilder();
            var props = new List<string>();
            var field = member as FieldInfo;
            var property = member as PropertyInfo;
            var method = member as MethodInfo;
            if(field != null) {
                if(!field.IsPublic)
                    props.Add(simplifed ? "P" : "Private");
                if(field.IsStatic)
                    props.Add(simplifed ? "S" : "Static");
                if(field.IsInitOnly)
                    props.Add(simplifed ? "R" : "Read Only");
                if(field.IsLiteral)
                    props.Add(simplifed ? "C" : "Constant");
            } else if(method != null) {
                if(!method.IsPublic)
                    props.Add(simplifed ? "P" : "Private");
                if(method.IsStatic)
                    props.Add(simplifed ? "S" : "Static");
            } else if(property != null) {
                if(property.CanRead && property.CanWrite)
                    props.Add(simplifed ? "RW" : "Read Write");
                if(property.CanRead && (method = property.GetGetMethod()) != null) {
                    if(!property.CanWrite)
                        props.Add(simplifed ? "R" : "Read Only");
                    if(!method.IsPublic)
                        props.Add(simplifed ? "Pg" : "Private Get");
                    if(method.IsStatic)
                        props.Add(simplifed ? "Sg" : "Static Get");
                }
                if(property.CanWrite && (method = property.GetSetMethod()) != null) {
                    if(!property.CanRead)
                        props.Add(simplifed ? "W" : "Write Only");
                    if(!method.IsPublic)
                        props.Add(simplifed ? "Ps" : "Private Set");
                    if(method.IsStatic)
                        props.Add(simplifed ? "Ss" : "Static Set");
                }
            }
            if(props.Count > 0)
                ret.Append("(");
            JoinStringList(ret, props, simplifed ? "" : ", ");
            if(props.Count > 0)
                ret.Append(") ");
            ret.Append(member.Name);
            return ret.ToString();
        }

        internal static StringBuilder JoinStringList(StringBuilder sb, IEnumerable<string> list, string separator) {
            if(sb == null)
                sb = new StringBuilder();
            bool nonFirst = false;
            foreach(var item in list) {
                if(nonFirst)
                    sb.Append(separator);
                sb.Append(item);
                nonFirst = true;
            }
            return sb;
        }

        internal static Quaternion QuaternionField(string label, Quaternion value, params GUILayoutOption[] options) {
            var cValue = new Vector4(value.x, value.y, value.z, value.w);
            cValue = EditorGUILayout.Vector4Field(label, cValue, options);
            return new Quaternion(cValue.x, cValue.y, cValue.z, cValue.w);
        }

        internal static Quaternion QuaternionField(Rect position, string label, Quaternion value) {
            var cValue = new Vector4(value.x, value.y, value.z, value.w);
            cValue = EditorGUI.Vector4Field(position, label, cValue);
            return new Quaternion(cValue.x, cValue.y, cValue.z, cValue.w);
        }

        internal static object EnumField(Rect position, GUIContent label, Type type, object value) {
            GUIContent[] itemNames;
            Array itemValues;
            int val = EnumFieldPreProcess(type, value, out itemNames, out itemValues);
            int newVal = EditorGUI.Popup(position, label, val, itemNames);
            return EnumFieldPostProcess(itemValues, newVal);
        }

        internal static object EnumField(GUIContent label, Type type, object value, params GUILayoutOption[] options) {
            GUIContent[] itemNames;
            Array itemValues;
            int val = EnumFieldPreProcess(type, value, out itemNames, out itemValues);
            int newVal = EditorGUILayout.Popup(label, val, itemNames, options);
            return EnumFieldPostProcess(itemValues, newVal);
        }

        static int EnumFieldPreProcess(Type type, object rawValue, out GUIContent[] itemNames, out Array itemValues) {
            itemNames = Enum.GetNames(type).Select(x => new GUIContent(x)).ToArray();
            itemValues = Enum.GetValues(type);
            long val = Convert.ToInt64(rawValue);
            for(int i = 0; i < itemValues.Length; i++)
                if(Convert.ToInt64(itemValues.GetValue(i)) == val)
                    return i;
            return 0;
        }

        static object EnumFieldPostProcess(Array itemValues, int val) {
            return itemValues.GetValue(val);
        }


        internal static object MaskedEnumField(Rect position, string label, Type type, object mask) {
            return MaskedEnumField(position, new GUIContent(label), type, mask);
        }

        internal static object MaskedEnumField(Rect position, GUIContent label, Type type, object mask) {
            string[] itemNames;
            Array itemValues;
            int val = MaskedEnumFieldPreProcess(type, mask, out itemNames, out itemValues);
            int newVal = EditorGUI.MaskField(position, label, val, itemNames);
            return MaskedEnumFieldPostProcess(type, itemValues, mask, val, newVal);
        }

        internal static object MaskedEnumField(string label, Type type, object mask, params GUILayoutOption[] options) {
            return MaskedEnumField(new GUIContent(label), type, mask, options);
        }

        internal static object MaskedEnumField(GUIContent label, Type type, object mask, params GUILayoutOption[] options) {
            string[] itemNames;
            Array itemValues;
            int val = MaskedEnumFieldPreProcess(type, mask, out itemNames, out itemValues);
            int newVal = EditorGUILayout.MaskField(label, val, itemNames, options);
            return MaskedEnumFieldPostProcess(type, itemValues, mask, val, newVal);
        }

        static int MaskedEnumFieldPreProcess(Type type, object rawValue, out string[] itemNames, out Array itemValues) {
            itemNames = Enum.GetNames(type);
            itemValues = Enum.GetValues(type);
            int maskVal = 0;
            long value = Convert.ToInt64(rawValue), itemValue;
            for(int i = 0; i < itemValues.Length; i++) {
                itemValue = Convert.ToInt64(itemValues.GetValue(i));
                if(itemValue != 0) {
                    if((value & itemValue) != 0)
                        maskVal |= 1 << i;
                } else if(value == 0)
                    maskVal |= 1 << i;
            }
            return maskVal;
        }

        static object MaskedEnumFieldPostProcess(Type enumType, Array itemValues, object rawValue, int maskVal, int newMaskVal) {
            int changes = maskVal ^ newMaskVal;
            long value = Convert.ToInt64(rawValue), itemValue;
            for(int i = 0; i < itemValues.Length; i++)
                if((changes & (1 << i)) != 0) {
                    itemValue = Convert.ToInt64(itemValues.GetValue(i));
                    if((newMaskVal & (1 << i)) != 0) {
                        if(itemValue == 0) {
                            rawValue = 0;
                            break;
                        }
                        value |= itemValue;
                    } else
                        value &= ~itemValue;
                }
            return Enum.ToObject(enumType, value);
        }

        internal static string StringField(GUIContent label, string value, bool readOnly, params GUILayoutOption[] options) {
            int length = value == null ? 0 : value.Length;
            if(length > 5000) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, new GUIContent("Text too long to display (" + length + " characters)"));
                if(GUILayout.Button("Copy", GUILayout.ExpandWidth(false)))
                    EditorGUIUtility.systemCopyBuffer = value;
                if(!readOnly && GUILayout.Button("Paste", GUILayout.ExpandWidth(false))) {
                    value = EditorGUIUtility.systemCopyBuffer;
                    GUI.changed = true;
                }
                EditorGUILayout.EndHorizontal();
            } else {
                int lines = CountLines(value);
                if(lines > 1) {
                    var _opts = options.ToList();
                    _opts.Add(GUILayout.Height(EditorGUIUtility.singleLineHeight * lines));
                    _opts.Add(GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth));
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.PrefixLabel(label);
                    if(readOnly)
                        EditorGUILayout.SelectableLabel(value, EditorStyles.textArea, _opts.ToArray());
                    else
                        value = EditorGUILayout.TextArea(value, _opts.ToArray());
                    EditorGUILayout.EndVertical();
                } else {
                    if(readOnly) {
                        var _opts = options.ToList();
                        _opts.Add(GUILayout.Height(EditorGUIUtility.singleLineHeight));
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PrefixLabel(label);
                        EditorGUILayout.SelectableLabel(value, EditorStyles.textField, _opts.ToArray());
                        EditorGUILayout.EndHorizontal();
                    } else
                        value = EditorGUILayout.TextField(label, value, options);
                }
            }
            return value;
        }

        internal static string StringField(Rect position, GUIContent label, string value, bool readOnly) {
            if(readOnly) {
                EditorGUI.SelectableLabel(position, value);
            } else {
                int lines = position.height <= EditorGUIUtility.singleLineHeight ? 1 : CountLines(value);
                if(lines > 1)
                    EditorGUI.PrefixLabel(ScaleRect(position, heightScale: 0, offsetHeight: EditorGUIUtility.singleLineHeight), new GUIContent(label));
                value = lines > 1 ?
                    EditorGUI.TextArea(ScaleRect(position, offsetY: EditorGUIUtility.singleLineHeight, offsetHeight: -EditorGUIUtility.singleLineHeight), value) :
                    EditorGUI.TextField(position, label, value);
            }
            return value;
        }

        internal static UnityObject ObjectField(GUIContent label, UnityObject value, Type objectType, bool allowScreenObjs, bool readOnly, params GUILayoutOption[] options) {
            if(!readOnly)
                return EditorGUILayout.ObjectField(label, value, objectType, allowScreenObjs, options);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            var _opts = options.ToList();
            _opts.Add(GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if(GUILayout.Button(EditorGUIUtility.ObjectContent(value, objectType), EditorStyles.objectField, _opts.ToArray()))
                ClickObject(value);
            EditorGUILayout.EndHorizontal();
            return value;
        }

        internal static UnityObject ObjectField(Rect position, GUIContent label, UnityObject value, Type objectType, bool allowScreenObjs, bool readOnly) {
            if(!readOnly)
                return EditorGUI.ObjectField(position, label, value, objectType, allowScreenObjs);
            EditorGUI.PrefixLabel(ScaleRect(position, widthScale: 0.5F), label);
            if(GUI.Button(ScaleRect(position, 0.5F, widthScale: 0.5F), EditorGUIUtility.ObjectContent(value, objectType), EditorStyles.objectField))
                ClickObject(value);
            return value;
        }

        static void ClickObject(UnityObject obj) {
            var newClickTime = EditorApplication.timeSinceStartup;
            if(newClickTime - clickTime < 0.3 && obj != null)
                Selection.activeObject = obj;
            clickTime = newClickTime;
            EditorGUIUtility.PingObject(obj);
        }

        static int CountLines(string str) {
            if(string.IsNullOrEmpty(str))
                return 1;
            int cursor = 0, count = 0, length = str.Length, i = -1;
            bool isCR = false;
            while(cursor < length) {
                i = str.IndexOf('\r', cursor);
                if(i >= 0) {
                    count++;
                    isCR = true;
                    cursor = i + 1;
                    continue;
                }
                i = str.IndexOf('\n', cursor);
                if(i >= 0) {
                    if(!isCR || i != 0)
                        count++;
                    isCR = false;
                    cursor = i + 1;
                    continue;
                }
                break;
            }
            return Math.Max(1, count);
        }

        internal static bool AssignValue(MemberInfo info, object target, object value) {
            try {
                var fieldInfo = info as FieldInfo;
                var propertyInfo = info as PropertyInfo;
                if(fieldInfo != null && !fieldInfo.IsInitOnly && !fieldInfo.IsLiteral)
                    fieldInfo.SetValue(target, value);
                else if(propertyInfo != null && propertyInfo.CanWrite)
                    propertyInfo.SetValue(target, value, null);
                else
                    return false;
            } catch {
                return false;
            }
            return true;
        }

        internal static bool IsReadOnly(MemberInfo info) {
            var fieldInfo = info as FieldInfo;
            if(fieldInfo != null)
                return fieldInfo.IsInitOnly || fieldInfo.IsLiteral;
            var propertyInfo = info as PropertyInfo;
            if(propertyInfo != null)
                return !propertyInfo.CanWrite;
            return false;
        }

        internal static bool FetchValue(MemberInfo info, object target, out object value) {
            value = null;
            try {
                var fieldInfo = info as FieldInfo;
                var propertyInfo = info as PropertyInfo;
                if(fieldInfo != null)
                    value = fieldInfo.GetValue(target);
                else if(propertyInfo != null && propertyInfo.CanRead)
                    value = propertyInfo.GetValue(target, null);
                else
                    return false;
            } catch(Exception ex) {
                value = ex;
                return false;
            }
            return true;
        }

        internal static int ObjIdOrHashCode(object obj) {
            var unityObj = obj as UnityObject;
            if(unityObj != null)
                return unityObj.GetInstanceID();
            if(obj != null)
                return obj.GetHashCode();
            return 0;
        }

        internal static bool IsInterface(Type type, Type interfaceType) {
            foreach(var iType in type.GetInterfaces())
                if(iType == interfaceType || (iType.IsGenericType && iType.GetGenericTypeDefinition() == interfaceType))
                    return true;
            return false;
        }

        internal static GUIStyle GetGUIStyle(string styleName) {
            return GUI.skin.FindStyle(styleName) ?? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle(styleName);
        }

        [MenuItem("Window/Inspector+")]
        public static void ShowInspectorPlus() {
            EditorWindow.GetWindow(typeof(InspectorPlus));
        }
    }
}