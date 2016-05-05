using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityObject = UnityEngine.Object;

namespace JLChnToZ.Toolset.Editor.ScriptTester {
    class InspectorPlus: EditorWindow, IHasCustomMenu {
        const string description = "The main purpose of this panel is provide a general way " +
            "to debugging and testing the game objects and scripts, " +
            "it has power to access everything exists in the game and editor, " +
            "even they are invisible to the global, " +
            "thus improper use may cause the game or the editor crash in some cases. " +
            "Use this at your own risk.";

        readonly List<InspectorDrawer[]> drawers = new List<InspectorDrawer[]>();
        string searchText;
        Vector2 scrollPos;
        bool autoUpdateValues = EditorPrefs.GetBool("inspectorplus_autoupdate", true);
        bool privateFields = EditorPrefs.GetBool("inspectorplus_private", true);
        bool forceUpdateProps = EditorPrefs.GetBool("inspectorplus_editupdate", false);
        bool showProps = EditorPrefs.GetBool("inspectorplus_props", true);
        bool showMethods = EditorPrefs.GetBool("inspectorplus_methods", true);
        bool locked = EditorPrefs.GetBool("inspectorplus_lock", false);
        bool showObsolete = EditorPrefs.GetBool("inspectorplus_obsolete", false);
        int[] instanceIds = new int[0];

        void OnEnable() {
#if UNITY_4
            title = "Inspector+";
#else
            titleContent = new GUIContent("Inspector+", EditorGUIUtility.FindTexture("UnityEditor.InspectorWindow"));
#endif
            OnFocus();
        }

        void OnFocus() {
            OnSelectionChange();
        }

        void OnGUI() {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUI.changed = false;
            GUILayout.Space(8);
            searchText = EditorGUILayout.TextField(searchText, Helper.GetGUIStyle("ToolbarSeachTextField"));
            if(GUILayout.Button(GUIContent.none, Helper.GetGUIStyle(string.IsNullOrEmpty(searchText) ? "ToolbarSeachCancelButtonEmpty" : "ToolbarSeachCancelButton"))) {
                searchText = string.Empty;
                GUI.FocusControl(null);
            }
            if(GUI.changed)
                IterateDrawers<ComponentMethodDrawer>(methodDrawer => methodDrawer.Filter = searchText);
            GUILayout.Space(8);
            GUILayout.EndHorizontal();
            GUI.changed = false;
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.HelpBox(description, MessageType.Info);
            foreach(var drawer in drawers.SelectMany(drawer => drawer)) {
                drawer.searchText = searchText;
                drawer.Draw();
            }
            GUILayout.FlexibleSpace();
            GUILayout.Space(EditorGUIUtility.singleLineHeight / 2);
            GUILayout.EndScrollView();
        }

        void OnInspectorUpdate() {
            if(!autoUpdateValues || EditorGUIUtility.editingTextField)
                return;
            UpdateValues();
        }

        void ShowButton(Rect rect) {
            GUI.Toggle(rect, locked, GUIContent.none, Helper.GetGUIStyle("IN LockButton"));
            if(GUI.changed)
                TriggerLock();
            GUI.changed = false;
        }

        public void AddItemsToMenu(GenericMenu menu) {
            menu.AddItem(new GUIContent("Refresh"), false, RefreshList);
            if(autoUpdateValues)
                menu.AddDisabledItem(new GUIContent("Update Values", "Auto Updating"));
            else
                menu.AddItem(new GUIContent("Update Values"), false, UpdateValues);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Lock Selection"), locked, TriggerLock);
            menu.AddItem(new GUIContent("Auto Update Values"), autoUpdateValues, () => {
                autoUpdateValues = !autoUpdateValues;
                EditorPrefs.SetBool("inspectorplus_autoupdate", autoUpdateValues);
            });
            menu.AddItem(new GUIContent("Update Properties in Edit Mode"), forceUpdateProps, () => {
                forceUpdateProps = !forceUpdateProps;
                UpdateValues();
                EditorPrefs.SetBool("inspectorplus_editupdate", forceUpdateProps);
            });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Show Properties"), showProps, () => {
                showProps = !showProps;
                RefreshList();
                EditorPrefs.SetBool("inspectorplus_props", showProps);
            });
            menu.AddItem(new GUIContent("Show Methods"), showMethods, () => {
                showMethods = !showMethods;
                RefreshList();
                EditorPrefs.SetBool("inspectorplus_methods", showMethods);
            });
            menu.AddItem(new GUIContent("Show Private Members"), privateFields, () => {
                privateFields = !privateFields;
                RefreshList();
                IterateDrawers<IReflectorDrawer>(methodDrawer => methodDrawer.AllowPrivateFields = privateFields);
                EditorPrefs.SetBool("inspectorplus_private", privateFields);
            });
            menu.AddItem(new GUIContent("Show Obsolete Members"), showObsolete, () => {
                showObsolete = !showObsolete;
                RefreshList();
                IterateDrawers<IReflectorDrawer>(methodDrawer => methodDrawer.AllowObsolete = showObsolete);
                EditorPrefs.SetBool("inspectorplus_obsolete", showObsolete);
            });
        }

        void RefreshList() {
            drawers.Clear();
            OnSelectionChange();
        }

        void TriggerLock() {
            locked = !locked;
            if(!locked)
                OnSelectionChange();
            EditorPrefs.SetBool("inspectorplus_lock", locked);
        }

        void OnSelectionChange() {
            if(!locked)
                instanceIds = Selection.instanceIDs;
            var pendingRemoveDrawers = new List<InspectorDrawer[]>();
            var pendingAddDrawers = new List<InspectorDrawer[]>();
            foreach(var drawer in drawers)
                if(drawer.Length <= 0 || drawer[0].target == null || !instanceIds.Contains(Helper.ObjIdOrHashCode(drawer[0].target)))
                    pendingRemoveDrawers.Add(drawer);
            drawers.RemoveAll(pendingRemoveDrawers.Contains);
            foreach(var instanceID in instanceIds)
                if(drawers.FindIndex(drawer => Helper.ObjIdOrHashCode(drawer[0].target) == instanceID) < 0)
                    pendingAddDrawers.Add(CreateDrawers(instanceID));
            drawers.AddRange(pendingAddDrawers);
            UpdateValues();
        }

        InspectorDrawer[] CreateDrawers(int instanceID) {
            var target = EditorUtility.InstanceIDToObject(instanceID);
            if(target == null)
                return new InspectorDrawer[0];
            var ret = new List<InspectorDrawer>();
            try {
                ret.Add(CreateDrawer(target, true));
            } catch(Exception ex) {
                Debug.LogException(ex);
            }
            var gameObject = target as GameObject;
            if(gameObject != null)
                foreach(var component in gameObject.GetComponents(typeof(Component))) {
                    try {
                        ret.Add(CreateDrawer(component, false));
                    } catch(Exception ex) {
                        Debug.LogException(ex);
                    }
                }
            return ret.ToArray();
        }

        InspectorDrawer CreateDrawer(UnityObject target, bool shown) {
            var drawer = new InspectorDrawer(target, shown, showProps, privateFields, showObsolete, showMethods);
            drawer.OnRequireRedraw += Repaint;
            return drawer;
        }

        void IterateDrawers<T>(Action<T> each) where T : IReflectorDrawer {
            foreach(var methodDrawer in drawers.SelectMany(drawer => drawer).SelectMany(drawer => drawer.drawer).OfType<T>())
                each(methodDrawer);
        }

        void UpdateValues() {
            UpdateValues(forceUpdateProps || EditorApplication.isPlaying);
        }

        void UpdateValues(bool updateProps) {
            foreach(var drawerGroup in drawers.SelectMany(drawer => drawer))
                drawerGroup.UpdateValues(updateProps);
            Repaint();
        }
    }
}