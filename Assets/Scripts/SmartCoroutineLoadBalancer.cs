using System;
using System.Collections;
using System.Threading;
using UnityEngine;

using ThreadPriority = System.Threading.ThreadPriority;

public class SmartCoroutineLoadBalancer {
    public const float defaultTheshold = 0;

    static readonly object forceLoadObj = new object();

    public static object ForceLoadYieldInstruction {
        get { return forceLoadObj; }
    }

    public static Coroutine StartCoroutine(MonoBehaviour component, IEnumerator routine, float theshold = defaultTheshold) {
        if(routine == null) return null;
        return component.StartCoroutine(new SmartCoroutine(routine, theshold));
    }

    private class SmartCoroutine: IEnumerator, IDisposable {
        readonly IEnumerator route;
        readonly float theshold;
        bool next;
        object current;

        public SmartCoroutine(IEnumerator route, float theshold) {
            this.route = route;
            this.theshold = theshold;
        }

        public object Current {
            get { return current; }
        }

        public bool MoveNext() {
            DateTime startTime = DateTime.UtcNow;
            while(next = route.MoveNext()) {
                current = route.Current;
                var subRoute = current as IEnumerator;
                if(subRoute != null) {
                    current = new SmartCoroutine(subRoute, theshold);
                    break;
                }
                if(current != null)
                    break;
                if((DateTime.UtcNow - startTime).ToAccurateSecondF() >= (theshold <= 0 ? Time.maximumDeltaTime : theshold))
                    break;
            }
            return next;
        }

        public void Reset() {
            next = false;
            route.Reset();
        }

        public void Dispose() {
            var disposable = route as IDisposable;
            if(disposable != null)
                disposable.Dispose();
        }
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
