using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;

namespace JLChnToZ.Toolset.Editor {
    [ExecuteInEditMode]
    public class SceneManagerWindow: EditorWindow {
        static readonly GUIContent homeMarker = new GUIContent("⌂");
        static readonly GUIContent playFromThisScene = new GUIContent("►");
        static readonly List<SceneData> sceneDatas = new List<SceneData>();

        EditorBuildSettingsScene[] buildScenes;
        Scene currentScene;
        SceneData currentSceneData;
        ReorderableList listDisplay;
        Vector2 scrollPos;
        bool requireRefresh, isPlaying;

        [SerializeField]
        bool hasPlayed, playScene;

        [SerializeField]
        string lastScene, startScene, waitScene;

        public static string AsSpacedCamelCase(string text) {
            var sb = new StringBuilder(text.Length * 2);
            sb.Append(char.ToUpper(text[0]));
            for(int i = 1, l = text.Length; i < l; i++) {
                if(char.IsUpper(text[i]) && text[i - 1] != ' ' && !char.IsUpper(text[i - 1]))
                    sb.Append(' ');
                sb.Append(text[i]);
            }
            return sb.ToString();
        }

        [MenuItem("Window/Scene Manager")]
        public static SceneManagerWindow Open() {
            return GetWindow<SceneManagerWindow>();
        }

        static bool CompareSceneData(EditorBuildSettingsScene lhs, EditorBuildSettingsScene rhs) {
            return lhs.enabled == rhs.enabled && lhs.path == rhs.path;
        }

        static bool IsCurrentScene(SceneData sceneData, Scene? currentScene = null) {
            var scene = currentScene.HasValue ? currentScene.Value : SceneManager.GetActiveScene();
            return EditorApplication.isPlaying ? sceneData.sceneIndex == scene.buildIndex : (sceneData.editorSceneData != null && scene.path == sceneData.editorSceneData.path);
        }

        static EditorWindow OpenInternalWindow(string typeName, bool isUtility = false, bool ignoreCase = false) {
            var windowType = Type.GetType(typeName, false, ignoreCase);
            return windowType != null && windowType.IsSubclassOf(typeof(EditorWindow)) ? GetWindow(windowType, isUtility) : null;
        }

        void DrawElement(Rect rect, int index, bool isActive, bool isFocused) {
            EditorGUILayout.BeginHorizontal();
            var sceneData = sceneDatas[index];
            string scenePath = sceneData.editorSceneData.path;
            bool isEnabled = sceneData.editorSceneData.enabled;
            bool isCurrentScene = IsCurrentScene(sceneData, currentScene);
            bool isCurrentStartScene = startScene == scenePath;
            bool isCurrentLastScene = hasPlayed && lastScene == scenePath;
            if(isCurrentScene)
                currentSceneData = sceneData;
            EditorGUI.BeginDisabledGroup(isPlaying && !isEnabled);
            EditorGUI.BeginChangeCheck();
            GUI.Toggle(
                new Rect(rect.x, rect.y, rect.width - 44, rect.height - 1),
                isCurrentScene,
                sceneData.namePrettified,
                EditorStyles.radioButton
            );
            if(EditorGUI.EndChangeCheck() && !isCurrentScene) {
                if(isPlaying) {
                    SceneManager.LoadScene(sceneData.sceneIndex);
                } else if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
                    playScene = false;
                    waitScene = scenePath;
                    Repaint();
                    EditorSceneManager.OpenScene(waitScene);
                    EditorApplication.isPlaying = false;
                }
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(isPlaying);
            EditorGUI.BeginChangeCheck();
            isEnabled = GUI.Toggle(
                new Rect(rect.x + rect.width - 44, rect.y, 24, rect.height - 1),
                isEnabled,
                string.Format("{0}{1}", isEnabled ? sceneData.sceneIndex.ToString() : "-", isCurrentLastScene ? "*" : string.Empty),
                EditorStyles.miniButtonLeft
            );
            if(EditorGUI.EndChangeCheck()) {
                sceneData.editorSceneData.enabled = isEnabled;
                buildScenes[index] = sceneData.editorSceneData;
                EditorBuildSettings.scenes = buildScenes;
                requireRefresh = true;
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginChangeCheck();
            GUI.Toggle(
                new Rect(rect.x + rect.width - 20, rect.y, 20, rect.height - 1),
                isCurrentStartScene,
                isPlaying && !isCurrentStartScene ? homeMarker : playFromThisScene,
                EditorStyles.miniButtonRight
            );
            if(EditorGUI.EndChangeCheck()) {
                if(isPlaying) {
                    hasPlayed = true;
                    lastScene = scenePath;
                    waitScene = null;
                    EditorApplication.isPlaying = false;
                } else if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
                    playScene = true;
                    lastScene = currentScene.path;
                    startScene = waitScene = scenePath;
                    Repaint();
                    EditorSceneManager.OpenScene(waitScene);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        void Refresh() {
            var currentScene = SceneManager.GetActiveScene();
            int buildIndex = -1;
            buildScenes = EditorBuildSettings.scenes;
            sceneDatas.Clear();
            sceneDatas.AddRange(buildScenes.Select(editorBuildScene => {
                var sceneData = new SceneData(editorBuildScene, editorBuildScene.enabled ? ++buildIndex : -1);
                if(currentScene.path == editorBuildScene.path)
                    currentSceneData = sceneData;
                return sceneData;
            }));
        }

        void DrawHeader(Rect rect) {
            GUI.Label(rect, "Available Scenes", EditorStyles.boldLabel);
        }


        void OnAdd(ReorderableList list) {
            currentScene = SceneManager.GetActiveScene();
            if(!currentScene.IsValid()) return;
            buildScenes = EditorBuildSettings.scenes;
            if(buildScenes.Any(buildScene => buildScene.path == currentScene.path))
                return;
            var newBuildScene = new EditorBuildSettingsScene(currentScene.path, false);
            int index = buildScenes.Length;
            Array.Resize(ref buildScenes, index + 1);
            buildScenes[index] = newBuildScene;
            EditorBuildSettings.scenes = buildScenes;
            Refresh();
            Repaint();
        }

        void OnEnable() {
            titleContent = new GUIContent("Scene Manager");
            EditorApplication.playmodeStateChanged = OnPlayModeChanged;
            Refresh();
            listDisplay = new ReorderableList(sceneDatas, typeof(SceneData)) {
                elementHeight = EditorGUIUtility.singleLineHeight + 1F,
                drawHeaderCallback = DrawHeader,
                drawElementCallback = DrawElement,
                drawFooterCallback = DrawFooter,
                onAddCallback = OnAdd,
                onReorderCallback = OnReordered,
                onRemoveCallback = OnRemove,
            };
        }

        void DrawFooter(Rect rect) {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            ReorderableList.defaultBehaviours.DrawFooter(rect, listDisplay);
            EditorGUI.EndDisabledGroup();
        }

        void OnGUI() {
            currentScene = SceneManager.GetActiveScene();
            if(isPlaying = EditorApplication.isPlayingOrWillChangePlaymode) {
                if(currentScene.path == waitScene)
                    waitScene = null;
            } else if(playScene && !string.IsNullOrEmpty(waitScene) && currentScene.path == waitScene) {
                EditorApplication.isPlaying = true;
                playScene = false;
                hasPlayed = true;
            }
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if(GUILayout.Button("Build Settings", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                OpenInternalWindow("UnityEditor.BuildPlayerWindow, UnityEditor.dll", true);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.BeginVertical(EditorStyles.inspectorFullWidthMargins);
            EditorGUI.BeginDisabledGroup(sceneDatas == null);
            EditorGUILayout.Space();
            requireRefresh = false;
            listDisplay.draggable = !isPlaying;
            listDisplay.DoLayoutList();
            if(requireRefresh) {
                Refresh();
                Repaint();
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        void OnInspectorUpdate() {
            if(!IsCurrentScene(currentSceneData))
                Repaint();
            if(buildScenes.Length != sceneDatas.Count) {
                Refresh();
                Repaint();
            }
        }

        void OnLostFocus() {
            listDisplay.index = -1;
        }

        void OnPlayModeChanged() {
            Repaint();
            if(EditorApplication.isPlaying) {
                if(string.IsNullOrEmpty(startScene))
                    startScene = SceneManager.GetActiveScene().path;
            } else {
                if(hasPlayed && string.IsNullOrEmpty(waitScene) && !string.IsNullOrEmpty(lastScene)) {
                    EditorSceneManager.OpenScene(lastScene);
                    lastScene = null;
                }
                hasPlayed = false;
                startScene = null;
            }
        }

        void OnRemove(ReorderableList list) {
            int index = list.index;
            if(index < 0) return;
            sceneDatas.RemoveAt(index);
            Array.Resize(ref buildScenes, sceneDatas.Count);
            for(int i = 0, l = sceneDatas.Count; i < l; i++)
                buildScenes[i] = sceneDatas[i].editorSceneData;
            EditorBuildSettings.scenes = buildScenes;
            Refresh();
            Repaint();
        }

        void OnReordered(ReorderableList list) {
            buildScenes = EditorBuildSettings.scenes;
            for(int i = 0, l = sceneDatas.Count; i < l; i++)
                buildScenes[i] = sceneDatas[i].editorSceneData;
            EditorBuildSettings.scenes = buildScenes;
            Refresh();
            Repaint();
        }

        [Serializable]
        struct SceneData {
            public readonly EditorBuildSettingsScene editorSceneData;
            public readonly bool enabled;
            public readonly GUIContent namePrettified;
            public readonly string nameRaw;
            public readonly int sceneIndex;
            public SceneData(EditorBuildSettingsScene editorSceneData, int sceneIndex) {
                this.sceneIndex = sceneIndex;
                this.editorSceneData = editorSceneData;
                nameRaw = Path.GetFileNameWithoutExtension(editorSceneData.path);
                enabled = editorSceneData.enabled;
                namePrettified = new GUIContent(AsSpacedCamelCase(nameRaw));
            }
        }
    }
}
