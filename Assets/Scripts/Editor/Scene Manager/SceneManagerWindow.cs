using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;

namespace JLChnToZ.Toolset.Editor {
    [ExecuteInEditMode]
    public class SceneManagerWindow: EditorWindow {
        Scene? lastScene = null;
        Scene? waitScene = null;
        bool hasPlayed = false;
        bool playScene = false;
        Vector2 scrollPos;
        Scene currentSceneData;

        [MenuItem("Window/Scene Manager")]
        public static void Run() {
            GetWindow<SceneManagerWindow>();
        }

        static Scene[] scenes = new Scene[0];

        void OnEnable() {
            titleContent = new GUIContent("Scene Manager");
            EditorApplication.playmodeStateChanged = OnPlayModeChanged;
            InitScenes(false);
        }

        void InitScenes(bool requireRepaint = true) {
            Array.Resize(ref scenes, SceneManager.sceneCount);
            for(int i = 0, l = SceneManager.sceneCount; i < l; i++)
                scenes[i] = SceneManager.GetSceneAt(i);

            if(requireRepaint)
                Repaint();
        }

        void OnPlayModeChanged() {
            Repaint();
            if(!EditorApplication.isPlaying) {
                if(!waitScene.HasValue && lastScene.HasValue) {
                    SwitchScene(lastScene.Value);
                    lastScene = null;
                }
            }
        }

        void OnInspectorUpdate() {
            if(SceneManager.GetActiveScene() != currentSceneData)
                Repaint();
            if(SceneManager.sceneCount != scenes.Length)
                InitScenes();
        }

        void OnGUI() {
            bool disabled = false;
            var activeScene = SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying) {
                if(activeScene == waitScene)
                    waitScene = null;
                disabled = true;
            } else if(activeScene == waitScene && playScene) {
                EditorApplication.isPlaying = true;
            }
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if(GUILayout.Button("Build Settings", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false))) {
                var buildPlayerWindow = Type.GetType("UnityEditor.BuildPlayerWindow, UnityEditor.dll");
                if(buildPlayerWindow != null) GetWindow(buildPlayerWindow, true);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.BeginVertical();
            EditorGUI.BeginDisabledGroup(disabled || scenes == null);
            for(int i = 0, l = scenes.Length; i < l; i++)
                DrawScene(scenes[i], i);
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        void DrawScene(Scene sceneData, int index) {
            EditorGUILayout.BeginHorizontal();
            var currentScene = SceneManager.GetActiveScene();
            bool isCurrentScene = currentScene == sceneData;
            if(isCurrentScene)
                currentSceneData = sceneData;
            GUI.changed = false;
            GUILayout.Toggle(isCurrentScene, GUIContent.none, EditorStyles.radioButton, GUILayout.ExpandWidth(false));
            GUILayout.Label(sceneData.name);
            if(GUI.changed && !isCurrentScene && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
                playScene = false;
                waitScene = sceneData;
                Repaint();
                SwitchScene(waitScene.Value);
                EditorApplication.isPlaying = false;
            }
            GUI.changed = false;
            EditorGUI.BeginDisabledGroup(sceneData.buildIndex < 0);
            if(GUILayout.Button("Play from this scene!", EditorStyles.miniButton, GUILayout.ExpandWidth(false)) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
                playScene = true;
                lastScene = currentScene;
                waitScene = sceneData;
                Repaint();
                SwitchScene(waitScene.Value);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            if(SceneManager.GetSceneAt(index) != sceneData)
                InitScenes();
        }

        void SwitchScene(Scene scene) {
            if(scene.buildIndex < 0)
                SceneManager.LoadScene(scene.name, LoadSceneMode.Single);
            else
                SceneManager.LoadScene(scene.buildIndex, LoadSceneMode.Single);
        }

        public static string AsSpacedCamelCase(string text) {
            var sb = new StringBuilder(text.Length * 2);
            sb.Append(char.ToUpper(text[0]));
            for(int i = 1, l = text.Length; i < l; i++) {
                if(char.IsUpper(text[i]) && text[i - 1] != ' ')
                    sb.Append(' ');
                sb.Append(text[i]);
            }
            return sb.ToString();
        }
    }
}
