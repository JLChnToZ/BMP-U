using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;

namespace JLChnToZ.Toolset.Timing {
    internal class UpdateValueEvent: UnityEvent<float, float> { }

    public class LerpDelayHelper {
        readonly MonoBehaviour parent;

        Coroutine lerpCoroutine;

        readonly UnityEvent onUpdate;
        public UnityEvent OnUpdate {
            get { return onUpdate; }
        }

        readonly UpdateValueEvent onUpdateValue;
        public UnityEvent<float, float> OnUpdateValue {
            get { return onUpdateValue; }
        }

        bool intMode;
        public bool IntegerMode {
            get { return intMode; }
            set { intMode = value; }
        }

        float lerpDelta;
        public float LerpDelta {
            get { return lerpDelta; }
            set { lerpDelta = value; }
        }

        float currentValue;
        public float CurrentValue {
            get { return currentValue; }
            set { currentValue = value; }
        }

        float targetValue;
        public float TargetValue {
            get { return targetValue; }
            set {
                targetValue = value;
                if(!IsReached && lerpCoroutine == null)
                    lerpCoroutine = parent.StartCoroutine(StartLerp());
            }
        }

        bool IsReached {
            get { return Mathf.Abs(targetValue - currentValue) < float.Epsilon; }
        }

        public LerpDelayHelper(MonoBehaviour parent) {
            if(parent == null) throw new ArgumentNullException("parent");
            this.parent = parent;
            this.onUpdate = new UnityEvent();
            this.onUpdateValue = new UpdateValueEvent();
            this.lerpDelta = 1;
        }

        public void Stop(bool changeImmediately = false) {
            if(lerpCoroutine != null) {
                parent.StopCoroutine(lerpCoroutine);
                lerpCoroutine = null;
                if(changeImmediately) ToTargetValue();
            }
        }

        IEnumerator StartLerp() {
            yield return new WaitForEndOfFrame();
            float oldValue;
            while(!IsReached) {
                oldValue = currentValue;
                currentValue = Mathf.Lerp(currentValue, targetValue, Time.deltaTime * lerpDelta);
                if(intMode) currentValue = targetValue >= currentValue ? Mathf.Ceil(currentValue) : Mathf.Floor(currentValue);
                onUpdate.Invoke();
                onUpdateValue.Invoke(oldValue, currentValue);
                yield return new WaitForEndOfFrame();
            }
            lerpCoroutine = null;
            ToTargetValue();
            yield break;
        }

        bool ToTargetValue() {
            if(!IsReached) {
                currentValue = targetValue;
                onUpdate.Invoke();
                return true;
            }
            currentValue = targetValue;
            return false;
        }
    }
}
