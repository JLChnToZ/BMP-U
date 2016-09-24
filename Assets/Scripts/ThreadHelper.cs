using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#else
using System.Collections;
using System.Collections.Generic;
using UnityObject = UnityEngine.Object;
using DisruptorUnity3d;
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
        if(threadHandler) return;
        try {
            var go = new GameObject {
                isStatic = true,
                hideFlags = HideFlags.HideAndDontSave
            };
            UnityObject.DontDestroyOnLoad(go);
            threadHandler = go.AddComponent<UnityThreadHandler>();
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
#if UNITY_EDITOR
        EditorApplication.CallbackFunction wrapped = null;
        wrapped = () => {
            EditorApplication.update -= wrapped;
            try {
                callback.Invoke();
            } catch(Exception ex) {
                Debug.LogException(ex);
            }
        };
        EditorApplication.update += wrapped;
#else
        threadHandler.Enqueue(callback);
#endif
    }

    public static float Theshold {
#if UNITY_EDITOR
        get { return 0; }
        set { }
#else
        get { return timeTheshold; }
        set {
            timeTheshold = value;
            if(threadHandler && threadHandler.threadCoroutine != null)
                threadHandler.threadCoroutine.theshold = timeTheshold;
        }
#endif
    }

#if UNITY_EDITOR
    static ThreadHelper() {
        EditorApplication.update += GetUnityThread;
    }

    private static void GetUnityThread() {
        if(unityThread == null)
            unityThread = Thread.CurrentThread;
        EditorApplication.update -= GetUnityThread;
    }
#else
    static float timeTheshold;
    static UnityThreadHandler threadHandler;

    class UnityThreadHandler: MonoBehaviour {
        public ThreadCoroutine threadCoroutine;

        void Awake() {
            if(unityThread == null)
                unityThread = Thread.CurrentThread;
        }

        void Update() {
            if(threadCoroutine != null && threadCoroutine.isWaiting && !threadCoroutine.isRunning) {
                threadCoroutine.isRunning = true;
                StartCoroutine(threadCoroutine);
            }
        }

        public void Enqueue(UnityAction method) {
            if(threadCoroutine == null)
                threadCoroutine = new ThreadCoroutine(timeTheshold);
            threadCoroutine.Enqueue(method);
        }
    }

    class ThreadCoroutine: IEnumerator {
        readonly RingBuffer<UnityAction> methodQueue = new RingBuffer<UnityAction>(10);
        public float theshold;
        public bool isWaiting, isRunning;

        public ThreadCoroutine(float theshold) {
            this.theshold = theshold;
        }

        public void Enqueue(UnityAction method) {
            if(method != null)
                methodQueue.Enqueue(method);
            isWaiting = true;
        }

        public object Current {
            get { return null; }
        }

        public bool MoveNext() {
            float startTime = Time.realtimeSinceStartup;
            while(methodQueue.Count > 0) {
                isWaiting = false;
                try {
                    methodQueue.Dequeue().Invoke();
                } catch(Exception ex) {
                    Debug.LogException(ex);
                }
                if(Time.realtimeSinceStartup - startTime >= (theshold <= 0 ? Time.maximumDeltaTime : theshold))
                    return true;
            }
            isRunning = false;
            return false;
        }

        public void Reset() { }
    }
#endif
}