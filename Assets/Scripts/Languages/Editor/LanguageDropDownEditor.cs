using UnityEngine;
using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(LanguageDropDown))]
public class LanguageDropDownEditor: DropdownEditor {
    SerializedProperty language;
    static GUIContent langLabel = new GUIContent("Language ID");

    protected override void OnEnable() {
        base.OnEnable();
        language = serializedObject.FindProperty("langId");
    }

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        EditorGUILayout.PropertyField(language, langLabel);
        serializedObject.ApplyModifiedProperties();
    }
}
