using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using UnityObject = UnityEngine.Object;

public static class HelperFunctions {
    public static float TimeLerpHelper(float startTime, float endTime, float currentTime) {
        return (currentTime - startTime) / (endTime - startTime);
    }

    public static float TimeLerpHelper(TimeSpan startTime, TimeSpan endTime, TimeSpan currentTime) {
        return (float)((currentTime - startTime).Ticks) / (endTime - startTime).Ticks;
    }

    public static Color ColorFromHSL(float h, float s, float l, float a = 1) {
        float v = (l <= 0.5F) ? (l * (1 + s)) : (l + s - l * s);
        if(v > 0) {
            h *= 6;
            int sextant = Mathf.FloorToInt(h);
            float m = l + l - v;
            float vsf = (v - m) * (h - sextant);
            switch(sextant % 6) {
                case 0:
                    return new Color(v, m + vsf, m, a);
                case 1:
                    return new Color(v - vsf, v, m, a);
                case 2:
                    return new Color(m, v, m + vsf, a);
                case 3:
                    return new Color(m, v - vsf, v, a);
                case 4:
                    return new Color(m + vsf, m, v, a);
                case 5:
                    return new Color(v, m, v - vsf, a);
            }
        }
        return new Color(l, l, l, a);
    }

    public static T Last<T>(this T[] source, int offset = -1) {
        return source[source.Length + offset];
    }

    public static void LookAt(this Transform transform, Vector2 worldPosition, float fineTuneAngle = 0) {
        transform.rotation = GetDirection((Vector2)transform.position, worldPosition, fineTuneAngle);
    }

    public static Quaternion GetDirection(Vector2 currentPoint, Vector2 lookAtPoint, float fineTuneAngle = 0) {
        Vector2 direction = lookAtPoint - currentPoint;
        direction.Normalize();
        if(direction == Vector2.zero) {
            return Quaternion.identity;
        }
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + fineTuneAngle;
        return Quaternion.AngleAxis(angle, Vector3.forward);
    }

    public static T[] GenericGetComponents<T>(this GameObject gameObject) where T : class {
        var result = new List<T>();
        foreach(var component in gameObject.GetComponents(typeof(Component))) {
            var target = component as T;
            if(target != null)
                result.Add(target);
        }
        return result.ToArray();
    }

    public static T Choose<T>(int index, params T[] items) {
        if(index < 0 || index >= items.Length)
            return default(T);
        return items[index];
    }

    public static void ResetAndAddListener(this UnityEvent uEvent, UnityAction uAction) {
        uEvent.RemoveAllListeners();
        uEvent.AddListener(uAction);
    }

    public static void DestroyAllChildren(this Transform parent) {
        if(parent.childCount <= 0)
            return;
        foreach(Transform child in parent) {
            DestroyAllChildren(child);
            UnityObject.Destroy(child.gameObject);
        }
    }

    public static void RemoveMatch<T>(this ICollection<T> source, Func<T, bool> predicate) {
        var temp = new HashSet<T>();
        foreach(T item in source)
            if(predicate.Invoke(item))
                temp.Add(item);
        foreach(T item in temp)
            source.Remove(item);
    }

    public static int mod(this int x, int m) {
        return (int)(((float)x % m + m) % m);
    }

    public static float mod(this float x, float m) {
        return (x % m + m) % m;
    }
}
