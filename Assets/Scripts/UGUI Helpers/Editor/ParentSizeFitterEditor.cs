using UnityEditor;
using UnityEditor.UI;

namespace JLChnToZ.Toolset.UI.Editor {
    [CustomEditor(typeof(ParentSizeFitter), true)]
    [CanEditMultipleObjects]
    public class ParentSizeFitterEditor: SelfControllerEditor {
        SerializedProperty horizontalFit, verticalFit;
        new SerializedProperty target;

        protected virtual void OnEnable() {
            horizontalFit = serializedObject.FindProperty("m_HorizontalFit");
            verticalFit = serializedObject.FindProperty("m_VerticalFit");
            target = serializedObject.FindProperty("target");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUILayout.PropertyField(horizontalFit, true);
            EditorGUILayout.PropertyField(verticalFit, true);
            EditorGUILayout.PropertyField(target, true);
            serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }
    }
}