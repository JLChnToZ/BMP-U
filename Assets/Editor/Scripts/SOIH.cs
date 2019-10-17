﻿using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;

namespace JLChnToZ.Toolset.Editor {
    /// <summary>
    /// <see cref="ScriptableObject"/> instance creation helper class.
    /// </summary>
    public static class SOIH {
        static readonly Type scriptableObjType = typeof(ScriptableObject);

        /// <summary>
        /// Create an instance of <see cref="ScriptableObject"/> with <see cref="Type"/> specified to the asset database.
        /// </summary>
        /// <param name="type">The type object of the scriptable object.</param>
        /// <param name="name">Asset name, leave empty to auto generate.</param>
        /// <param name="autoSelect">Should auto selected the asset once created?</param>
        /// <param name="allowUserRename">Should allow user to customize the name during creation?</param>
        /// <returns>The new instance of scriptable object.</returns>
        public static ScriptableObject CreateAsset(Type type, string name = "", bool autoSelect = true, bool allowUserRename = false) {
            if(type == null) throw new ArgumentNullException("type");
            var asset = InternalCreateAsset(type, name, allowUserRename);
            FinalizeAsset();
            if(autoSelect) Selection.activeObject = asset;
            return asset;
        }

        /// <summary>
        /// Create an instance of <see cref="ScriptableObject"/> to the asset database.
        /// </summary>
        /// <typeparam name="TScriptableObj">The child type derived by <see cref="ScriptableObject"/>.</typeparam>
        /// <param name="name">Asset name, leave empty to auto generate.</param>
        /// <param name="autoSelect">Should auto selected the asset once created?</param>
        /// <param name="allowUserRename">Should allow user to customize the name during creation?</param>
        /// <returns>The new instance of scriptable object.</returns>
        public static TScriptableObj CreateAsset<TScriptableObj>(string name = "", bool autoSelect = true, bool allowUserRename = false) where TScriptableObj : ScriptableObject {
            var asset = InternalCreateAsset(typeof(TScriptableObj), name, allowUserRename);
            FinalizeAsset();
            if(autoSelect) Selection.activeObject = asset;
            return asset as TScriptableObj;
        }

        [MenuItem("Assets/Create/Scriptable Object Instance")]
        static void CreateAssetMenuItem() {
            var types = new HashSet<Type>(InternalGetSelectedSOTypes());
            var instances = new List<ScriptableObject>(types.Count);
            bool supportManualRename = types.Count == 1;
            foreach(var type in types)
                instances.Add(InternalCreateAsset(type, string.Empty, supportManualRename));
            FinalizeAsset();
            Selection.objects = instances.ToArray();
        }

        [MenuItem("Assets/Create/Scriptable Object Instance", validate = true)]
        static bool CreateAssetMenuItemEnabled() {
            using(IEnumerator<Type> types = InternalGetSelectedSOTypes().GetEnumerator())
                return types.MoveNext();
        }

        static ScriptableObject InternalCreateAsset(Type type, string name, bool allowUserRename) {
            var asset = ScriptableObject.CreateInstance(type);

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if(string.IsNullOrEmpty(path))
                path = "Assets";
            else if(!string.IsNullOrEmpty(Path.GetExtension(path)))
                path = path.Replace(Path.GetFileName(path), string.Empty);

            if(string.IsNullOrEmpty(name) || name.Trim().Length < 1)
                name = $"New {ObjectNames.NicifyVariableName(type.Name)}";

            string pathName = AssetDatabase.GenerateUniqueAssetPath($"{path}/{name}.asset");

            if(allowUserRename)
                ProjectWindowUtil.CreateAsset(asset, pathName);
            else
                AssetDatabase.CreateAsset(asset, pathName);

            return asset;
        }

        static void FinalizeAsset() {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
        }

        static IEnumerable<Type> InternalGetSelectedSOTypes() {
            foreach(var monoScript in Selection.GetFiltered<MonoScript>(SelectionMode.Assets)) {
                var type = monoScript.GetClass();
                if(type != null && type.IsSubclassOf(scriptableObjType))
                    yield return type;
            }
            yield break;
        }
    }
}
