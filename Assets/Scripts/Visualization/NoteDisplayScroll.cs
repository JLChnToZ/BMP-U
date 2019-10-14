using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

namespace BananaBeats.Visualization {
    public class NoteDisplayScroll: JobComponentSystem {

        public static float time;
        public static float3[] refStartPos, refEndPos;

        [BurstCompile]
        private struct Job: IJobForEach<NoteDisplay, Translation, NonUniformScale> {
            public float time;
            [DeallocateOnJobCompletion, NativeDisableParallelForRestriction]
            public NativeArray<float3> refStartPos;
            [DeallocateOnJobCompletion, NativeDisableParallelForRestriction]
            public NativeArray<float3> refEndPos;

            public void Execute([ReadOnly] ref NoteDisplay data, ref Translation translation, ref NonUniformScale nonUniformScale) {
                var scale = nonUniformScale.Value;
                var pos2 = data.pos2;
                if(float.IsInfinity(pos2))
                    pos2 = data.pos1 + 10;
                translation.Value = math.lerp(refEndPos[data.channel], refStartPos[data.channel], ((data.pos1 + pos2) / 2F - time) * data.scale);
                scale.z = math.max(1, math.abs(data.pos1 - pos2));
                nonUniformScale.Value = scale;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            if(refStartPos == null|| refEndPos == null)
                return inputDeps;
            var job = new Job {
                time = time,
                refStartPos = new NativeArray<float3>(refStartPos, Allocator.TempJob),
                refEndPos = new NativeArray<float3>(refEndPos, Allocator.TempJob),
            };
            time += Time.unscaledDeltaTime;
            return job.Schedule(this, inputDeps);
        }
    }
}