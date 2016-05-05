using UnityEngine;
using UnityEditor;

namespace JLChnToZ.Toolset.Editor.ScriptTester {
    class InspectorChildWindow: EditorWindow {
        InspectorDrawer drawer;
        Vector2 scrollPos;
        bool updateProps;

        public static void Open(object target, bool showProps, bool showPrivate, bool showObsolete, bool showMethods, bool updateProps) {
            CreateInstance<InspectorChildWindow>().InternalOpen(target, showProps, showPrivate, showObsolete, showMethods, updateProps);
        }

        void InternalOpen(object target, bool showProps, bool showPrivate, bool showObsolete, bool showMethods, bool updateProps) {
#if UNITY_4
			title = string.Format("{0} - Inspector+", target);
#else
            titleContent = new GUIContent(string.Format("{0} - Inspector+", target));
#endif
            drawer = new InspectorDrawer(target, true, showProps, showPrivate, showObsolete, showMethods);
            drawer.OnRequireRedraw += Repaint;
            this.updateProps = updateProps;
            ShowUtility();
            UpdateValues();
        }

        void OnGUI() {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            updateProps = GUILayout.Toggle(updateProps, "Update Props", EditorStyles.toolbarButton);
            GUILayout.Space(8);
            drawer.searchText = EditorGUILayout.TextField(drawer.searchText, Helper.GetGUIStyle("ToolbarSeachTextField"));
            if(GUILayout.Button(GUIContent.none, Helper.GetGUIStyle(string.IsNullOrEmpty(drawer.searchText) ? "ToolbarSeachCancelButtonEmpty" : "ToolbarSeachCancelButton"))) {
                drawer.searchText = string.Empty;
                GUI.FocusControl(null);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.Space();
            drawer.Draw(false);
            GUILayout.FlexibleSpace();
            GUILayout.EndScrollView();
        }

        void OnInspectorUpdate() {
            if(EditorGUIUtility.editingTextField)
                return;
            UpdateValues();
        }

        void UpdateValues() {
            drawer.UpdateValues(updateProps);
        }
    }
}
