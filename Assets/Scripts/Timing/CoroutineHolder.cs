using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityObject = UnityEngine.Object;

namespace JLChnToZ.Toolset.Timing {
    /// <summary>
    /// Standalone coroutine holder.
    /// </summary>
    /// <remarks>
    /// It will be auto destroyed when all coroutines are finished and/or terminated externally.
    /// </remarks>
    public class CoroutineHolder: MonoBehaviour {
        class State {
            public Coroutine coroutine;
            public bool ended;
        }

        static readonly HashSet<CoroutineHolder> holders = new HashSet<CoroutineHolder>();
        static GameObject tempGO;
        bool isTempGameObject;
        bool isDestroying;
        object objLock;
        Dictionary<Coroutine, IEnumerator> coroutines;

        private CoroutineHolder() {
            objLock = new object();
            coroutines = new Dictionary<Coroutine, IEnumerator>();
            holders.Add(this);
        }

        ~CoroutineHolder() {
            OnDestroy();
        }

        void OnDestroy() {
            holders.Remove(this);
        }

        /// <summary>
        /// Count of how many coroutines is binded into current holder
        /// </summary>
        public int CoroutineCount {
            get { return Count(); }
        }

        /// <summary>
        /// Starts a coroutine
        /// </summary>
        /// <param name="route">Routine</param>
        /// <returns>
        /// Coroutine pointer,
        /// <c>null</c> if the coroutine is ended immediately after it runs.
        /// </returns>
        public new Coroutine StartCoroutine(IEnumerator route) {
            if(route == null) throw new ArgumentNullException("route");
            lock (objLock) {
                var state = new State();
                state.coroutine = base.StartCoroutine(Route(state, route));
                if(state.ended || state.coroutine == null) {
                    Count();
                    return null;
                }
                coroutines.Add(state.coroutine, route);
                return state.coroutine;
            }
        }

        /// <summary>
        /// Starts a coroutine
        /// </summary>
        /// <param name="routeFunc">Method which will return a enumerator route.</param>
        /// <returns>
        /// Coroutine pointer,
        /// <c>null</c> if the coroutine is ended immediately after it runs.
        /// </returns>
        public Coroutine StartCoroutine(Func<IEnumerator> routeFunc) {
            return StartCoroutine(routeFunc.Invoke());
        }

        /// <summary>
        /// Stops a coroutine that creates with coroutine holder.
        /// </summary>
        /// <param name="route">Route enumerator</param>
        /// <returns><c>true</c> if success find the coroutine and stops it.</returns>
        /// <remarks>
        /// If no more coroutines binds with current coroutine holder,
        /// it will be auto destroyed.
        /// </remarks>
        public new bool StopCoroutine(IEnumerator route) {
            return CoroutineEnd(route, true);
        }

        /// <summary>
        /// Stops a coroutine that creates with coroutine holder.
        /// </summary>
        /// <param name="coroutine">Coroutine pointer</param>
        /// <returns><c>true</c> if success find the coroutine and stops it.</returns>
        /// <remarks>
        /// If no more coroutines binds with current coroutine holder,
        /// it will be auto destroyed.
        /// </remarks>
        public new bool StopCoroutine(Coroutine coroutine) {
            return CoroutineEnd(coroutine, true);
        }

        /// <summary>
        /// Stops all coroutines and remove current holder.
        /// </summary>
        public new void StopAllCoroutines() {
            lock (objLock) {
                base.StopAllCoroutines();
                coroutines.Clear();
                Count();
            }
        }

        IEnumerator Route(State state, IEnumerator route) {
            try {
                while(route.MoveNext()) yield return route.Current;
            } finally {
                var disposable = route as IDisposable;
                if(disposable != null) disposable.Dispose();
                state.ended = true;
                CoroutineEnd(state.coroutine, false);
            }
            yield break;
        }

        bool CoroutineEnd(IEnumerator route, bool forceStop) {
            if(!coroutines.ContainsValue(route)) return false;
            lock (objLock) {
                Coroutine coroutine = null;
                foreach(var kv in coroutines)
                    if(kv.Value.Equals(route)) {
                        coroutine = kv.Key;
                        break;
                    }
                if(coroutine == null) return false;
                if(forceStop) base.StopCoroutine(route);
                coroutines.Remove(coroutine);
                Count();
            }
            return true;
        }

        bool CoroutineEnd(Coroutine coroutine, bool forceStop) {
            if(!coroutines.ContainsKey(coroutine)) return false;
            lock (objLock) {
                if(forceStop) base.StopCoroutine(coroutine);
                coroutines.Remove(coroutine);
                Count();
            }
            return true;
        }

        int Count() {
            int count = coroutines.Count;
            if(count <= 0)
                base.StartCoroutine(DelayedDestroy());
            return count;
        }

        IEnumerator DelayedDestroy() {
            yield return new WaitForEndOfFrame();
            int count = coroutines.Count;
            if(count <= 0) {
                isDestroying = true;
                Destroy(isTempGameObject ? (UnityObject)gameObject : this);
                if(isTempGameObject) tempGO = null;
            }
        }

        /// <summary>
        /// Create a holder instance and/or temporary game object if needed and starts a coroutine.
        /// </summary>
        /// <param name="parent">
        /// Where the holder instance binds with,
        /// <c>null</c> to create temporary game object to hold it.
        /// </param>
        /// <param name="route">Routine</param>
        /// <returns>
        /// Coroutine pointer,
        /// <c>null</c> if the coroutine is ended immediately after it runs.
        /// </returns>
        public static Coroutine StartCoroutine(UnityObject parent, IEnumerator route) {
            bool tempCreateGO = false;
            GameObject go;
            var component = parent as Component;
            if(component != null) go = component.gameObject;
            else go = parent as GameObject;
            if(go == null) {
                tempGO = go = tempGO ?? new GameObject("Coroutine Holder");
                tempCreateGO = true;
            }
            if(route == null) throw new ArgumentNullException("route");
            var instance = go.GetComponent<CoroutineHolder>();
            if(instance == null || instance.isDestroying)
                instance = go.AddComponent<CoroutineHolder>();
            instance.isTempGameObject = tempCreateGO;
            return instance.StartCoroutine(route);
        }


        /// <summary>
        /// Create a holder instance and/or temporary game object if needed and starts a coroutine.
        /// </summary>
        /// <param name="parent">
        /// Where the holder instance binds with,
        /// <c>null</c> to create temporary game object to hold it.
        /// </param>
        /// <param name="routeFunc">Method which will return a enumerator route.</param>
        /// <returns>
        /// Coroutine pointer,
        /// <c>null</c> if the coroutine is ended immediately after it runs.
        /// </returns>
        public static Coroutine StartCoroutine(UnityObject parent, Func<IEnumerator> routeFunc) {
            return StartCoroutine(parent, routeFunc.Invoke());
        }

        /// <summary>
        /// Find and stop the coroutine in all created holder instances.
        /// </summary>
        /// <param name="coroutine">The coroutine that have to stop</param>
        /// <returns><c>true</c> if success find the coroutine and stops it.</returns>
        public static bool FindAndStopCoroutine(Coroutine coroutine) {
            if(coroutine == null) return false;
            var _holders = new CoroutineHolder[holders.Count];
            holders.CopyTo(_holders);
            foreach(var holder in _holders)
                if(holder != null && holder.StopCoroutine(coroutine))
                    return true;
            return false;
        }

        /// <summary>
        /// Find and stop the coroutine in all created holder instances.
        /// </summary>
        /// <param name="route">The route that have to stop</param>
        /// <returns><c>true</c> if success find the coroutine and stops it.</returns>
        public static bool FindAndStopCoroutine(IEnumerator route) {
            if(route == null) return false;
            var _holders = new CoroutineHolder[holders.Count];
            holders.CopyTo(_holders);
            foreach(var holder in _holders)
                if(holder != null && holder.StopCoroutine(route))
                    return true;
            return false;
        }

        /// <summary>
        /// Delay couple seconds then execute the action.
        /// </summary>
        /// <param name="parent">
        /// Where the coroutine holder instance binds with,
        /// <c>null</c> to create temporary game object to hold it.
        /// </param>
        /// <param name="delay">How many seconds to wait. If smaller or equals to zero, the callback action will be executed immediately in next update cycle.</param>
        /// <param name="action">What action should be executed after waiting</param>
        /// <returns>
        /// Coroutine pointer for this delayed action.
        /// You may call this with <see cref="StopCoroutine(Coroutine)"/> to stop it.
        /// </returns>
        public static Coroutine Delay(UnityObject parent, float delay, UnityAction action) {
            MonoBehaviour behaviour = parent as MonoBehaviour;
            if(behaviour == null) return StartCoroutine(parent, DelayRoute(delay, action));
            return behaviour.StartCoroutine(DelayRoute(delay, action));
        }

        /// <summary>
        /// Run the action in each delay.
        /// </summary>
        /// <param name="parent">
        /// Where the coroutine holder instance binds with,
        /// <c>null</c> to create temporary game object to hold it.
        /// </param>
        /// <param name="delay">How many seconds to wait. If smaller or equals to zero, the callback action will be executed immediately in next update cycle.</param>
        /// <param name="action">What action should be executed after waiting</param>
        /// <returns>
        /// Coroutine pointer for this delayed action.
        /// You may call this with <see cref="StopCoroutine(Coroutine)"/> to stop it.
        /// </returns>
        public static Coroutine SetInterval(UnityObject parent, float delay, UnityAction action) {
            MonoBehaviour behaviour = parent as MonoBehaviour;
            if(behaviour == null) return StartCoroutine(parent, IntervalCoroutine(delay, action));
            return behaviour.StartCoroutine(IntervalCoroutine(delay, action));
        }

        static IEnumerator DelayRoute(float delay, UnityAction action) {
            yield return delay > 0 ? new WaitForSeconds(delay) : null;
            action.Invoke();
        }

        static IEnumerator IntervalCoroutine(float delay, UnityAction action) {
            YieldInstruction instruction = delay > 0 ? new WaitForSeconds(delay) : null;
            while(true) {
                yield return instruction;
                action.Invoke();
            }
        }
    }

    public static partial class HelperFunctions {
        public static Coroutine Delay(this UnityObject source, float delay, UnityAction action) {
            return CoroutineHolder.Delay(source, delay, action);
        }

        public static Coroutine SetInterval(this UnityObject source, float delay, UnityAction action) {
            return CoroutineHolder.SetInterval(source, delay, action);
        }
    }
}
