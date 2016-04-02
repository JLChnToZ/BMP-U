using System;
using System.Collections.Generic;

namespace JLChnToZ.Toolset.Singleton {
    static class SingletonDock {
        static readonly Dictionary<Type, object> dock;

        static SingletonDock() {
            dock = new Dictionary<Type, object>();
        }

        public static bool IsRegistered<T>() {
            return dock.ContainsKey(typeof(T));
        }

        public static bool Register<T>(T instance, bool overwrite = false) {
            var type = typeof(T);
            if(!overwrite && dock.ContainsKey(type))
                return false;
            dock[type] = instance;
            return true;
        }

        public static bool Unregister<T>() {
            return dock.Remove(typeof(T));
        }

        public static T Get<T>(bool defaultIfNotFound = false) {
            var type = typeof(T);
            object boxedObject;
            if(!dock.TryGetValue(type, out boxedObject)) {
                if(defaultIfNotFound)
                    return default(T);
                throw new KeyNotFoundException(string.Format("Type {0} is not registered.", type));
            }
            return (T)boxedObject;
        }
    }

    public static class SingletonDock<T> {
        public static bool IsRegistered {
            get { return SingletonDock.IsRegistered<T>(); }
        }

        public static T Instance {
            get { return SingletonDock.Get<T>(true); }
            set { SingletonDock.Register(value, true); }
        }

        public static T StrictInstance {
            get { return SingletonDock.Get<T>(); }
            set {
                if(!SingletonDock.Register(value))
                    throw new InvalidOperationException(string.Format("Type {0} has been already registered.", typeof(T)));
            }
        }

        public static bool Unregister() {
            return SingletonDock.Unregister<T>();
        }
    }

    public abstract class Singleton<T>: IDisposable where T : Singleton<T> {
        public static T Instance {
            get { return SingletonDock.Get<T>(true); }
        }

        protected Singleton() {
            var instance = Instance;
            if(instance != null && instance != this)
                throw new Exception(string.Format("Singleton instance {0} already exists.", typeof(T)));
            SingletonDock.Register<T>(this as T, true);
        }

        public virtual void Dispose() {
            if(this == Instance)
                SingletonDock.Unregister<T>();
        }
    }
}
