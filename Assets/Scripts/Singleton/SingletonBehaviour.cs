using System;
using System.Text;
using UnityEngine;

namespace JLChnToZ.Toolset.Singleton {
    public abstract class SingletonBehaviour<T>: MonoBehaviour where T : SingletonBehaviour<T> {
        static T instance;

        public static T Instance {
            get { return instance; }
        }

#if UNITY_EDITOR
        static string formattedTypeName;

        static string FormattedTypeName {
            get {
                if(string.IsNullOrEmpty(formattedTypeName)) {
                    string rawName = typeof(T).Name;
                    int nameLength = rawName.Length;
                    var sb = new StringBuilder(nameLength * 2);
                    sb.Append(char.ToUpper(rawName[0]));
                    for(int i = 1; i < nameLength; i++) {
                        char current = rawName[i], prev = rawName[i - 1];
                        if((char.IsUpper(current) && char.IsLetter(prev)) ||
                            (char.IsDigit(current) && char.IsLetter(prev)))
                            sb.Append(' ');
                        if(char.IsLetter(current) && !char.IsLetter(prev)) {
                            sb.Append(' ');
                            sb.Append(char.ToUpper(current));
                        } else
                            sb.Append(current);
                    }
                    formattedTypeName = sb.ToString();
                }
                return formattedTypeName;
            }
        }
#endif

        public static T GetOrCreate(bool dontDestroyOnLoad = true) {
            T _instance;
            if(TryGetInstance(out _instance))
                return _instance;

            // Step 2: If not found any instance, create a game object with a new instance in it.
            var container = new GameObject {
#if UNITY_EDITOR
                name = string.Format("[Singleton] {0}", FormattedTypeName)
#endif
            };
            if((instance = _instance = container.AddComponent<T>()) == null) {
#if UNITY_EDITOR
                Debug.LogErrorFormat(container, "Failed to create {0}.", FormattedTypeName);
#endif
                Destroy(container);
            } else if(dontDestroyOnLoad)
                DontDestroyOnLoad(container);

            SingletonDock<T>.Instance = _instance;

            return _instance;
        }

        public static bool TryGetInstance(out T _instance) {
            if(instance != null) {
                _instance = instance;
                return true;
            }

            instance = SingletonDock<T>.Instance;
            if(instance != null) {
                _instance = instance;
                return true;
            }

            _instance = null;
            return false;
        }

        protected SingletonBehaviour() {
            if(!(this is T))
                throw new InvalidCastException("T must be the class itself.");
        }

        protected virtual void Awake() {
            RegisterInstance();
        }

        protected virtual void OnDestroy() {
            UnregisterInstance();
        }

        protected bool RegisterInstance() {
            if(instance != (T)this && instance != null) {
#if UNITY_EDITOR
                Debug.LogErrorFormat(this, "Singleton: {0} has been already created.", FormattedTypeName);
#endif
                Destroy(gameObject);
                return false;
            }
            SingletonDock<T>.Instance = instance = (T)this;
            return true;
        }

        protected bool UnregisterInstance() {
            if(SingletonDock<T>.Instance == this)
                SingletonDock<T>.Unregister();

            if(instance != this)
                return false;
            instance = null;
            return true;
        }
    }
}
