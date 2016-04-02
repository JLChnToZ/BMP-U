using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityObject = UnityEngine.Object;

namespace JLChnToZ.Toolset.Timing {
    public static class TweenTransform {
        static readonly AnimationCurve defaultCurve = AnimationCurve.Linear(0, 0, 1, 1);
        static readonly Dictionary<Transform, Coroutine> tPosDict = new Dictionary<Transform, Coroutine>();
        static readonly Dictionary<Transform, Coroutine> tRotDict = new Dictionary<Transform, Coroutine>();
        static readonly Dictionary<Transform, Coroutine> tSclDict = new Dictionary<Transform, Coroutine>();

        public static Coroutine Tween(this Transform target, Transform from, Transform to,
            float duration, AnimationCurve curve = null,
            bool position = true, bool rotation = true, bool scale = true, bool ignoreTimeScale = false) {
            if(target == null) throw new ArgumentNullException("target");
            if(to == null) throw new ArgumentNullException("to");
            if(curve == null) curve = defaultCurve;
            Coroutine c;
            if(position && tPosDict.TryGetValue(target, out c))
                CoroutineHolder.FindAndStopCoroutine(c);
            if(rotation && tRotDict.TryGetValue(target, out c))
                CoroutineHolder.FindAndStopCoroutine(c);
            if(scale && tSclDict.TryGetValue(target, out c))
                CoroutineHolder.FindAndStopCoroutine(c);
            c = CoroutineHolder.StartCoroutine(target, TweenRoute(
                target, from, to, duration, curve, position, rotation, scale, ignoreTimeScale
            ));
            if(position) tPosDict[target] = c;
            if(rotation) tRotDict[target] = c;
            if(scale) tSclDict[target] = c;
            return c;
        }

        public static Coroutine TweenPosition(this Transform target, Vector3 from, Vector3 to, float duration,
            AnimationCurve curve = null, bool ignoreTimeScale = false, bool local = false) {
            if(target == null) throw new ArgumentNullException("target");
            if(curve == null) curve = defaultCurve;
            Coroutine c;
            if(tPosDict.TryGetValue(target, out c))
                CoroutineHolder.FindAndStopCoroutine(c);
            c = CoroutineHolder.StartCoroutine(target, TweenPositionRoute(
                target, from, to, duration, curve, ignoreTimeScale, local
            ));
            tPosDict[target] = c;
            return c;
        }


        public static Coroutine TweenRotation(this Transform target, Quaternion from, Quaternion to,
            float duration, AnimationCurve curve = null, bool ignoreTimeScale = false, bool local = false) {
            if(target == null) throw new ArgumentNullException("target");
            if(curve == null) curve = defaultCurve;
            Coroutine c;
            if(tRotDict.TryGetValue(target, out c))
                CoroutineHolder.FindAndStopCoroutine(c);
            c = CoroutineHolder.StartCoroutine(target, TweenRotationRoute(
                target, from, to, duration, curve, ignoreTimeScale, local
            ));
            tRotDict[target] = c;
            return c;
        }

        public static Coroutine TweenScale(this Transform target, Vector3 from, Vector3 to,
            float duration, AnimationCurve curve = null, bool ignoreTimeScale = false) {
            if(target == null) throw new ArgumentNullException("target");
            if(curve == null) curve = defaultCurve;
            Coroutine c;
            if(tSclDict.TryGetValue(target, out c))
                CoroutineHolder.FindAndStopCoroutine(c);
            c = CoroutineHolder.StartCoroutine(target, TweenScaleRoute(
                target, from, to, duration, curve, ignoreTimeScale
            ));
            tSclDict[target] = c;
            return c;
        }

        public static Coroutine Tween(UnityAction<float> onChange, float from, float to, float duration,
            AnimationCurve curve = null, bool ignoreTimeScale = false, UnityObject parent = null) {
            if(curve == null) curve = defaultCurve;
            return CoroutineHolder.StartCoroutine(parent, TweenCustomRoute(
                onChange, from, to, duration, curve, ignoreTimeScale
            ));
        }

        public static Coroutine Tween(UnityAction<Vector2> onChange, Vector2 from, Vector2 to, float duration,
            AnimationCurve curve = null, bool ignoreTimeScale = false, UnityObject parent = null) {
            if(curve == null) curve = defaultCurve;
            return CoroutineHolder.StartCoroutine(parent, TweenCustomRoute(
                onChange, from, to, duration, curve, ignoreTimeScale
            ));
        }

        public static Coroutine Tween(UnityAction<Vector3> onChange, Vector3 from, Vector3 to, float duration,
            AnimationCurve curve = null, bool ignoreTimeScale = false, UnityObject parent = null) {
            if(curve == null) curve = defaultCurve;
            return CoroutineHolder.StartCoroutine(parent, TweenCustomRoute(
                onChange, from, to, duration, curve, ignoreTimeScale
            ));
        }

        public static Coroutine Tween(UnityAction<Vector4> onChange, Vector4 from, Vector4 to, float duration,
            AnimationCurve curve = null, bool ignoreTimeScale = false, UnityObject parent = null) {
            if(curve == null) curve = defaultCurve;
            return CoroutineHolder.StartCoroutine(parent, TweenCustomRoute(
                onChange, from, to, duration, curve, ignoreTimeScale
            ));
        }

        static IEnumerator TweenRoute(Transform target, Transform from, Transform to,
            float duration, AnimationCurve curve,
            bool position, bool rotation, bool scale, bool ignoreTimeScale) {
            float tDelta;
            bool hasFrom = true;
            Vector3 p = Vector3.zero, s = Vector3.zero;
            Quaternion r = Quaternion.identity;
            if(from == null) {
                p = target.position;
                r = target.rotation;
                s = target.localScale;
                hasFrom = false;
            }
            for(float t = 0; t < duration; t += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime) {
                tDelta = curve.Evaluate(t / duration);
                if(position)
                    TweenFramePosition(target, hasFrom ? from.position : p, to.position, tDelta, false);
                if(rotation)
                    TweenFrameRotation(target, hasFrom ? from.rotation : r, to.rotation, tDelta, false);
                if(scale)
                    TweenFrameScale(target, hasFrom ? from.localScale : s, to.localScale, tDelta);
                yield return null;
            }
            tDelta = curve.Evaluate(1);
            if(position)
                TweenFramePosition(target, hasFrom ? from.position : p, to.position, tDelta, false);
            if(rotation)
                TweenFrameRotation(target, hasFrom ? from.rotation : r, to.rotation, tDelta, false);
            if(scale)
                TweenFrameScale(target, hasFrom ? from.localScale : s, to.localScale, tDelta);
            yield break;
        }

        static IEnumerator TweenPositionRoute(Transform target, Vector3 from, Vector3 to,
            float duration, AnimationCurve curve, bool ignoreTimeScale, bool local) {
            for(float t = 0; t < duration; t += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime) {
                TweenFramePosition(target, from, to, curve.Evaluate(t / duration), local);
                yield return null;
            }
            TweenFramePosition(target, from, to, curve.Evaluate(1), local);
            yield break;
        }

        static IEnumerator TweenRotationRoute(Transform target, Quaternion from, Quaternion to,
            float duration, AnimationCurve curve, bool ignoreTimeScale, bool local) {
            for(float t = 0; t < duration; t += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime) {
                TweenFrameRotation(target, from, to, curve.Evaluate(t / duration), local);
                yield return null;
            }
            TweenFrameRotation(target, from, to, curve.Evaluate(1), local);
            yield break;
        }

        static IEnumerator TweenScaleRoute(Transform target, Vector3 from, Vector3 to,
            float duration, AnimationCurve curve, bool ignoreTimeScale) {
            for(float t = 0; t < duration; t += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime) {
                TweenFrameScale(target, from, to, curve.Evaluate(t / duration));
                yield return null;
            }
            TweenFrameScale(target, from, to, curve.Evaluate(1));
            yield break;
        }

        static IEnumerator TweenCustomRoute(UnityAction<float> onChange, float from, float to,
            float duration, AnimationCurve curve, bool ignoreTimeScale) {
            for(float t = 0; t < duration; t += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime) {
                onChange.Invoke(Mathf.Lerp(from, to, curve.Evaluate(t / duration)));
                yield return null;
            }
            onChange.Invoke(Mathf.Lerp(from, to, curve.Evaluate(1)));
            yield break;
        }

        static IEnumerator TweenCustomRoute(UnityAction<Vector2> onChange, Vector2 from, Vector2 to,
            float duration, AnimationCurve curve, bool ignoreTimeScale) {
            for(float t = 0; t < duration; t += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime) {
                onChange.Invoke(Vector2.Lerp(from, to, curve.Evaluate(t / duration)));
                yield return null;
            }
            onChange.Invoke(Vector2.Lerp(from, to, curve.Evaluate(1)));
            yield break;
        }

        static IEnumerator TweenCustomRoute(UnityAction<Vector3> onChange, Vector3 from, Vector3 to,
            float duration, AnimationCurve curve, bool ignoreTimeScale) {
            for(float t = 0; t < duration; t += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime) {
                onChange.Invoke(Vector3.Lerp(from, to, curve.Evaluate(t / duration)));
                yield return null;
            }
            onChange.Invoke(Vector3.Lerp(from, to, curve.Evaluate(1)));
            yield break;
        }

        static IEnumerator TweenCustomRoute(UnityAction<Vector4> onChange, Vector4 from, Vector4 to,
            float duration, AnimationCurve curve, bool ignoreTimeScale) {
            for(float t = 0; t < duration; t += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime) {
                onChange.Invoke(Vector4.Lerp(from, to, curve.Evaluate(t / duration)));
                yield return null;
            }
            onChange.Invoke(Vector4.Lerp(from, to, curve.Evaluate(1)));
            yield break;
        }

        static void TweenFramePosition(Transform target, Vector3 from, Vector3 to, float t, bool local) {
            if(local)
                target.localPosition = Vector3.Lerp(from, to, t);
            else
                target.position = Vector3.Lerp(from, to, t);
        }

        static void TweenFrameRotation(Transform target, Quaternion from, Quaternion to, float t, bool local) {
            if(local)
                target.localRotation = Quaternion.Lerp(from, to, t);
            else
                target.rotation = Quaternion.Lerp(from, to, t);
        }

        static void TweenFrameScale(Transform target, Vector3 from, Vector3 to, float t) {
            target.localScale = Vector3.Lerp(from, to, t);
        }
    }
}
