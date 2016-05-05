using System;
using System.Collections;
using System.Threading;
using UnityEngine;

using ThreadPriority = System.Threading.ThreadPriority;

public class SmartCoroutineLoadBalancer {
    public const float defaultTheshold = 60F;

    static readonly object forceLoadObj = new object();

    public static object ForceLoadYieldInstruction {
        get { return forceLoadObj; }
    }

    public static Coroutine StartCoroutine(MonoBehaviour component, IEnumerator routine, float theshold = defaultTheshold) {
        if(routine == null) return null;
        return component.StartCoroutine(SmartCoroutine(routine, theshold));
    }

    private static IEnumerator SmartCoroutine(IEnumerator routine, float theshold) {
        theshold = theshold > 0 ? 1 / theshold : float.NegativeInfinity;
        object current;
        DateTime previous = DateTime.Now, snapshot;
        while(routine.MoveNext()) {
            current = routine.Current;
            snapshot = DateTime.Now;
            if(current != null || (float)(snapshot - previous).Ticks / TimeSpan.TicksPerSecond >= theshold) {
                previous = snapshot;
                yield return current == forceLoadObj ? null : current;
            }
        }
        yield break;
    }
}

public class ThreadJobCoroutineRunner<T> {
    readonly Func<T> threadJob;
    T result;

    public T Result {
        get { return result; }
    }

    public ThreadPriority priority = ThreadPriority.Normal;

    public ThreadJobCoroutineRunner(Func<T> threadJob) {
        if(threadJob == null)
            throw new ArgumentNullException("threadJob");
        this.threadJob = threadJob;
    }

    public Coroutine StartCoroutine(MonoBehaviour component) {
        return component.StartCoroutine(ThreadJobCoroutine());
    }

    public IEnumerator ThreadJobCoroutine() {
        var thread = new Thread(ThreadJob) {
            Priority = priority
        };
        thread.Start();
        while(thread.IsAlive)
            yield return null;
    }

    void ThreadJob() {
        result = threadJob.Invoke();
    }
}
