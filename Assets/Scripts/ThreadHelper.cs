using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
// A little hack to use same signature in editor mode.
using ThreadHandler = UnityEditor.EditorApplication;
using Action = UnityEditor.EditorApplication.CallbackFunction;
#else
using UnityObject = UnityEngine.Object;
#endif

/// <summary>
/// Helper class to make sure the code is running on Unity main thread.
/// </summary>
/// <remarks>
/// You can interact with Unity by calling its non-thread safe methods via this helper class on other thread.
/// </remarks>
public static class ThreadHelper {
    const string InitThreadErrorMsg = "Failed to create thread handler.\n" +
        "Please invoke InitThreadHandler() in Unity main thread " +
        "(Somewhere such as Awake, Start, Update, Coroutine functions in a MonoBehaviour) " +
        "before using the thread helper.";

    static Thread unityThread;

    /// <summary>
    /// Is current context inside the Unity main thread?
    /// </summary>
    public static bool IsInUnityThread {
        get {
#if !UNITY_EDITOR
            if(unityThread == null)
                InitThreadHandler();
#endif
            return unityThread != null && Thread.CurrentThread.ManagedThreadId == unityThread.ManagedThreadId;
        }
    }

    /// <summary>
    /// Initialize the thread handler.
    /// </summary>
    /// <remarks>
    /// This method must be invoked in Unity thread (Somewhere such as Awake, Start, Update,
    /// Coroutine functions in a <see cref="MonoBehaviour"/>) before you use other methods in this class.
    /// But for editor-only scripts, invocation of this method is not necessary.
    /// </remarks>
    public static void InitThreadHandler() {
#if !UNITY_EDITOR
        if(ThreadHandler) return;
        try {
            var go = new GameObject {
                isStatic = true,
                hideFlags = HideFlags.HideAndDontSave
            };
            UnityObject.DontDestroyOnLoad(go);
            ThreadHandler = go.AddComponent<UnityThreadHandler>();
        } catch {
            Debug.LogError(InitThreadErrorMsg);
        }
#endif
    }

    /// <summary>
    /// Run the code in Unity thread.
    /// </summary>
    /// <param name="callback">
    /// The delegate that contains code should be run on Unity thread.
    /// </param>
    public static void RunInUnityThread(UnityAction callback) {
        if(callback == null)
            throw new ArgumentNullException("callback");
        if(IsInUnityThread) {
            callback.Invoke();
            return;
        }
        Action wrapped = null;
        wrapped = () => {
            ThreadHandler.update -= wrapped;
            try {
                callback.Invoke();
            } catch(Exception ex) {
                Debug.LogException(ex);
            }
        };
        ThreadHandler.update += wrapped;
    }

#if UNITY_EDITOR
    static ThreadHelper() {
        ThreadHandler.update += GetUnityThread;
    }

    private static void GetUnityThread() {
        if(unityThread == null)
            unityThread = Thread.CurrentThread;
        ThreadHandler.update -= GetUnityThread;
    }
#else
    static UnityThreadHandler ThreadHandler;
    class UnityThreadHandler: MonoBehaviour {
        public event Action update;

        void Awake() {
            if(unityThread == null)
                unityThread = Thread.CurrentThread;
        }

        void Update() {
            if(update != null)
                update.Invoke();
        }
    }
#endif
}
