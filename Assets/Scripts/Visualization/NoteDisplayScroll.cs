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

        [BurstCompile, ExcludeComponent(typeof(FadeOut))]
        private struct ScrollNormalNotes: IJobForEach<NoteDisplay, Translation> {
            public float time;
            [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> refStartPos;
            [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> refEndPos;

            public void Execute([ReadOnly] ref NoteDisplay data, ref Translation translation) {
                var scaledPos = (data.pos - time) * data.scale;
                if(data.catched && scaledPos < 0) scaledPos = 0;
                translation.Value = math.lerp(refEndPos[data.channel], refStartPos[data.channel], scaledPos);
            }
        }

        [BurstCompile, ExcludeComponent(typeof(FadeOut))]
        private struct ScrollLongNotes: IJobForEach<LongNoteDisplay, Translation, NonUniformScale> {
            public float fixedEndtime;
            public float time;
            [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> refStartPos;
            [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> refEndPos;

            public void Execute([ReadOnly] ref LongNoteDisplay data, ref Translation translation, ref NonUniformScale nonUniformScale) {
                var scale = nonUniformScale.Value;
                var scaledPos1 = (data.pos1 - time) * data.scale1;
                var scaledPos2 = (data.pos2 >= data.pos1 ? data.pos2 - time : fixedEndtime) * data.scale2;
                if(data.catched && scaledPos1 < 0) scaledPos1 = 0;
                translation.Value = math.lerp(refEndPos[data.channel], refStartPos[data.channel], (scaledPos1 + scaledPos2) / 2F);
                scale.z = math.abs(scaledPos1 - scaledPos2);
                nonUniformScale.Value = scale;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            if(refStartPos == null|| refEndPos == null)
                return inputDeps;
            var scrollNormalNotes = new ScrollNormalNotes {
                time = time,
                refStartPos = new NativeArray<float3>(refStartPos, Allocator.TempJob),
                refEndPos = new NativeArray<float3>(refEndPos, Allocator.TempJob),
            };
            inputDeps = scrollNormalNotes.Schedule(this, inputDeps);
            var scrollLongNotes = new ScrollLongNotes {
                fixedEndtime = 3F,
                time = time,
                refStartPos = new NativeArray<float3>(refStartPos, Allocator.TempJob),
                refEndPos = new NativeArray<float3>(refEndPos, Allocator.TempJob),
            };
            time += Time.unscaledDeltaTime;
            inputDeps = scrollLongNotes.Schedule(this, inputDeps);
            return inputDeps;
        }
    }
}