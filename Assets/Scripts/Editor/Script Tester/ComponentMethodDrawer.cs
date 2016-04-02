using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityObject = UnityEngine.Object;

namespace JLChnToZ.Toolset.Editor.ScriptTester {
    class ComponentMethodDrawer: IReflectorDrawer {
        object component;
        readonly List<ComponentMethod> methods = new List<ComponentMethod>();
        AnimBool showMethodOptions;
        AnimBool showMethodSelector;
        AnimBool showResultSelector;
        string[] methodNames;
        int selectedMethodIndex;
        ConstructorInfo selectedCtor;
        MethodInfo selectedMethod;
        ParameterInfo[] parameterInfo;
        MethodPropertyDrawer[] parameters;
        MethodPropertyDrawer result;
        Exception thrownException;
        string filter;
        Type ctorType;
        bool titleFolded = true, paramsFolded = true, resultFolded = true,
            drawHeader = true, privateFields = true, obsolete = true, ctorMode = false;

        public event Action OnRequireRedraw;

        public bool ShouldDrawHeader {
            get { return drawHeader; }
            set {
                drawHeader = value;
                paramsFolded &= value;
                resultFolded &= value;
            }
        }

        public bool Changed {
            get { return false; }
        }

        public object Value {
            get { return result == null ? null : result.Value; }
        }

        public bool AllowPrivateFields {
            get {
                return privateFields;
            }
            set {
                privateFields = value;
                InitComponentMethods(false);
            }
        }

        public bool AllowObsolete {
            get {
                return obsolete;
            }
            set {
                obsolete = value;
                InitComponentMethods(false);
            }
        }

        public MemberInfo Info {
            get { return ctorMode ? selectedCtor as MemberInfo : selectedMethod as MemberInfo; }
        }

        public bool IsComponentNull() {
            return component == null;
        }

        public ComponentMethodDrawer() {
            showMethodSelector = new AnimBool(false);
            showMethodOptions = new AnimBool(false);
            showResultSelector = new AnimBool(false);
            showMethodSelector.valueChanged.AddListener(RequireRedraw);
            showMethodOptions.valueChanged.AddListener(RequireRedraw);
            showResultSelector.valueChanged.AddListener(RequireRedraw);
        }

        public ComponentMethodDrawer(object target)
            : this() {
            component = target;
            drawHeader = false;
            showMethodSelector.value = true;
            InitComponentMethods();
        }

        public ComponentMethodDrawer(Type type)
            : this() {
            ctorMode = true;
            ctorType = type;
            drawHeader = false;
            showMethodSelector.value = true;
            InitComponentMethods();
        }

        public string Filter {
            get { return filter; }
            set {
                filter = value;
                InitComponentMethods(false);
            }
        }

        public void Call() {
            if(selectedMethod == null || ctorMode ? ctorType == null : component == null || parameters == null)
                return;
            try {
                thrownException = null;
                var requestData = parameters.Select(d => d.Value).ToArray();
                if(selectedCtor != null) {
                    var returnData = selectedCtor.Invoke(requestData);
                    result = new MethodPropertyDrawer(selectedCtor.ReflectedType, "Constructed object", returnData, privateFields, obsolete);
                } else {
                    var returnData = selectedMethod.Invoke(component, requestData);
                    result = selectedMethod.ReturnType == typeof(void) ?
                    null :
                    new MethodPropertyDrawer(selectedMethod.ReturnType, "Return data", returnData, privateFields, obsolete);
                }
                for(int i = 0; i < Math.Min(parameters.Length, requestData.Length); i++) {
                    parameters[i].Value = requestData[i];
                    if(parameters[i].ReferenceMode)
                        Helper.AssignValue(parameters[i].RefFieldInfo, parameters[i].Component, requestData[i]);
                }
            } catch(Exception ex) {
                thrownException = ex.InnerException ?? ex;
                Debug.LogException(thrownException);
                throw;
            }
        }

        public void Draw() {
            if(drawHeader) {
                EditorGUI.BeginDisabledGroup(component == null);
                titleFolded = EditorGUILayout.InspectorTitlebar(titleFolded, component as UnityObject) || component == null;
                EditorGUI.EndDisabledGroup();
            }
            GUI.changed = false;
            if(component == null || titleFolded || !drawHeader) {
                if(drawHeader) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginVertical();
                    component = EditorGUILayout.ObjectField("Target", component as UnityObject, typeof(UnityObject), true);
                }
                if(component != null || ctorMode) {
                    if(GUI.changed) {
                        InitComponentMethods();
                        GUI.changed = false;
                    }
                    showMethodSelector.target = true;
                } else
                    showMethodSelector.target = false;
                if(EditorGUILayout.BeginFadeGroup(showMethodSelector.faded))
                    DrawComponent();
                EditorGUILayout.EndFadeGroup();
                showResultSelector.target = (!ctorMode && result != null) || thrownException != null;
                if(EditorGUILayout.BeginFadeGroup(showResultSelector.faded))
                    DrawResult();
                EditorGUILayout.EndFadeGroup();
                if(drawHeader) {
                    EditorGUILayout.EndVertical();
                    EditorGUI.indentLevel--;
                }
            }
        }

        void AddComponentMethod(Type type) {
            BindingFlags flag = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
            if(privateFields)
                flag |= BindingFlags.NonPublic;
            methods.AddRange(
                type.GetConstructors(flag)
                .Where(t => obsolete || !Attribute.IsDefined(t, typeof(ObsoleteAttribute)))
                .Where(t => string.IsNullOrEmpty(filter) || t.Name.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0)
                .Select(m => new ComponentMethod {
                    ctorInfo = m
                })
            );
            flag &= ~BindingFlags.Instance;
            methods.AddRange(
                type.GetMethods(flag)
                .Where(t => obsolete || !Attribute.IsDefined(t, typeof(ObsoleteAttribute)))
                .Where(t => t.ReturnType == type)
                .Where(t => string.IsNullOrEmpty(filter) || t.Name.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0)
                .Select(m => new ComponentMethod {
                    method = m,
                    target = null
                })
            );
        }

        void AddComponentMethod(object target) {
            BindingFlags flag = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
            if(privateFields)
                flag |= BindingFlags.NonPublic;
            methods.AddRange(
                target.GetType().GetMethods(flag)
                .Where(t => obsolete || !Attribute.IsDefined(t, typeof(ObsoleteAttribute)))
                .Where(t => string.IsNullOrEmpty(filter) || t.Name.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0)
                .Select(m => new ComponentMethod {
                    method = m,
                    target = target
                })
            );
        }

        void InitComponentMethods(bool resetIndex = true) {
            methods.Clear();
            if(ctorMode)
                AddComponentMethod(ctorType);
            else
                AddComponentMethod(component);
            if(ctorMode)
                methodNames = methods.Select((m, i) => GetMethodNameFormatted(m, i)).ToArray();
            else if(drawHeader) {
                var gameObject = component as GameObject;
                if(gameObject != null)
                    foreach(var c in gameObject.GetComponents(typeof(Component)))
                        AddComponentMethod(c);
                methodNames = methods.Select((m, i) => string.Format(
                    "{0} ({1})/{2}",
                    m.target.GetType().Name,
                    Helper.ObjIdOrHashCode(m.target),
                    GetMethodNameFormatted(m, i)
                )).ToArray();
            } else {
                methodNames = methods.Select((m, i) => GetMethodNameFormatted(m, i)).ToArray();
            }
            if(!resetIndex && selectedMethod != null) {
                selectedMethodIndex = methods.FindIndex(m => m.method == selectedMethod);
                if(selectedMethodIndex >= 0)
                    return;
            }
            selectedMethodIndex = -1;
            selectedMethod = null;
            selectedCtor = null;
            parameterInfo = null;
            parameters = null;
            result = null;
            thrownException = null;
        }

        string GetMethodNameFormatted(ComponentMethod m, int i) {
            string name;
            MethodBase method;
            if(m.ctorInfo != null) {
                method = m.ctorInfo;
                name = "[Constructor]";
            } else {
                method = m.method;
                name = Helper.GetMemberName(method as MemberInfo).Replace('_', ' ');
            }
            var result = string.Format("{0:000} {1} ({2})", i + 1, name, Helper.JoinStringList(null, method.GetParameters().Select(x => x.ParameterType.Name), ", "));
            return result;
        }

        void InitMethodParams() {
            selectedCtor = methods[selectedMethodIndex].ctorInfo;
            if(selectedCtor != null) {
                selectedMethod = null;
                component = null;
                parameterInfo = selectedCtor.GetParameters();
            } else {
                selectedMethod = methods[selectedMethodIndex].method;
                component = methods[selectedMethodIndex].target;
                parameterInfo = selectedMethod.GetParameters();
            }
            parameters = new MethodPropertyDrawer[parameterInfo.Length];
            for(int i = 0; i < parameterInfo.Length; i++) {
                var info = parameterInfo[i];
                parameters[i] = new MethodPropertyDrawer(info.ParameterType, info.Name, info.IsOptional ? info.DefaultValue : null, privateFields, obsolete);
                parameters[i].OnRequireRedraw += RequireRedraw;
            }
            result = null;
            thrownException = null;
        }

        void DrawComponent() {
            selectedMethodIndex = EditorGUILayout.Popup(ctorMode ? "Constructor" : "Method", selectedMethodIndex, methodNames);
            if(selectedMethodIndex >= 0) {
                if(GUI.changed) {
                    InitMethodParams();
                    GUI.changed = false;
                }
                showMethodOptions.target = true;
            } else
                showMethodOptions.target = false;
            if(EditorGUILayout.BeginFadeGroup(showMethodOptions.faded))
                DrawMethod();
            EditorGUILayout.EndFadeGroup();
        }

        void DrawMethod() {
            if(paramsFolded = EditorGUILayout.Foldout(paramsFolded, selectedCtor != null ? "Constructor" : selectedMethod.Name)) {
                GUI.changed = false;
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical();
                if(selectedCtor != null ? selectedCtor.ContainsGenericParameters : selectedMethod.ContainsGenericParameters)
                    EditorGUILayout.HelpBox("Generic method is not supported.", MessageType.Warning);
                else {
                    if(parameterInfo.Length == 0)
                        EditorGUILayout.HelpBox("There is no parameters required for this method.", MessageType.Info);
                    foreach(var drawer in parameters)
                        drawer.Draw();
                }
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
        }

        void DrawResult() {
            if(resultFolded = EditorGUILayout.Foldout(resultFolded, "Result")) {
                GUI.changed = false;
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical();
                if(result != null && !ctorMode)
                    result.Draw(true);
                if(thrownException != null)
                    EditorGUILayout.HelpBox(thrownException.Message, MessageType.Error);
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
        }

        void RequireRedraw() {
            if(OnRequireRedraw != null)
                OnRequireRedraw();
        }
    }
}
