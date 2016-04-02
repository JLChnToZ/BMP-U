using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace JLChnToZ.Voxels {
    public static class HelperFunctions {
        public static Vector2 ToVector2(this Vec2 vec2) {
            return new Vector2(vec2.i, vec2.j);
        }

        public static Vector3 ToVector3(this Vec3 vec3) {
            return new Vector3(vec3.i, vec3.j, vec3.k);
        }

        public static Vector4 ToVector4(this Vec4 vec4) {
            return new Vector4(vec4.i, vec4.j, vec4.k, vec4.l);
        }

        public static Vector3 VertexInterp(float isolevel, Vector3 p1, Vector3 p2, float valp1, float valp2) {
            if(Mathf.Abs(isolevel - valp1) < float.Epsilon)
                return p1;
            if(Mathf.Abs(isolevel - valp2) < float.Epsilon)
                return p2;
            if(Mathf.Abs(valp1 - valp2) < float.Epsilon)
                return p1;
            float mu = (isolevel - valp1) / (valp2 - valp1);
            var p = new Vector3();
            p.x = p1.x + mu * (p2.x - p1.x);
            p.y = p1.y + mu * (p2.y - p1.y);
            p.z = p1.z + mu * (p2.z - p1.z);
            return p;
        }

        public static Vector3 WeightedAverage(IList<Vector3> values, IList<float> weights) {
            if(values == null || weights == null || values.Count <= 0 || values.Count != weights.Count)
                return Vector3.zero;
            var x = MultiDimenIterator.Range(0, values.Count - 1, 1).WeightedAverage(i => values[i].x, i => weights[i]);
            var y = MultiDimenIterator.Range(0, values.Count - 1, 1).WeightedAverage(i => values[i].y, i => weights[i]);
            var z = MultiDimenIterator.Range(0, values.Count - 1, 1).WeightedAverage(i => values[i].z, i => weights[i]);
            return new Vector3(x, y, z);
        }

        public static float WeightedAverage<T>(this IEnumerable<T> records, Func<T, float> value, Func<T, float> weight) {
            float weightedValueSum = records.Sum(x => value(x) * weight(x));
            float weightSum = records.Sum(weight);

            if(Mathf.Abs(weightSum) > float.Epsilon)
                return weightedValueSum / weightSum;
            throw new DivideByZeroException();
        }
    }

}
