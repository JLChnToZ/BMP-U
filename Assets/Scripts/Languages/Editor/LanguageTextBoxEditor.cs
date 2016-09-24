using UnityEngine;
using UnityEditor;

using TextBoxEditor = UnityEditor.UI.TextEditor;

[CustomEditor(typeof(LanguageTextBox))]
public class LanguageTextBoxEditor: TextBoxEditor {
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