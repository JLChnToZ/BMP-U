﻿using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using E7.ECS.LineRenderer;

namespace BananaBeats.Visualization {
    public class NoteDisplayScroll: JobComponentSystem {
        public static float time;
        public static float3[] refStartPos, refEndPos;

        [BurstCompile, ExcludeComponent(typeof(Catched))]
        private struct ScrollNormalNotes: IJobForEach<NoteDisplay, Translation> {
            public float time;
            [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> refStartPos;
            [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> refEndPos;

            public void Execute([ReadOnly] ref NoteDisplay data, ref Translation translation) {
                var scaledPos = (data.pos - time) * data.scale;
                translation.Value = math.lerp(refEndPos[data.channel], refStartPos[data.channel], scaledPos);
            }
        }

        [BurstCompile, RequireComponentTag(typeof(Catched)), ExcludeComponent(typeof(FadeOut))]
        private struct ScrollCatchedNormalNotes: IJobForEach<NoteDisplay, Translation> {
            public float time;
            [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> refStartPos;
            [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> refEndPos;

            public void Execute([ReadOnly] ref NoteDisplay data, ref Translation translation) {
                var scaledPos = (data.pos - time) * data.scale;
                if(scaledPos < 0) scaledPos = 0;
                translation.Value = math.lerp(refEndPos[data.channel], refStartPos[data.channel], scaledPos);
            }
        }

        [BurstCompile, ExcludeComponent(typeof(Catched))]
        private struct ScrollLongNotes: IJobForEach<LongNoteDisplay, LineSegment> {
            public float fixedEndtime;
            public float time;
            [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> refStartPos;
            [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> refEndPos;

            public void Execute([ReadOnly] ref LongNoteDisplay data, ref LineSegment line) {
                var startPos = refStartPos[data.channel];
                var endPos = refEndPos[data.channel];
                line.from = math.lerp(endPos, startPos, (data.pos1 - time) * data.scale1);
                line.to = math.lerp(endPos, startPos, data.pos2 >= data.pos1 ? (data.pos2 - time) * data.scale2 : fixedEndtime);
            }
        }

        [BurstCompile, RequireComponentTag(typeof(Catched)), ExcludeComponent(typeof(FadeOut))]
        private struct ScrollCatchedLongNotes: IJobForEach<LongNoteDisplay, LineSegment> {
            public float fixedEndtime;
            public float time;
            [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> refStartPos;
            [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> refEndPos;

            public void Execute([ReadOnly] ref LongNoteDisplay data, ref LineSegment line) {
                var scaledPos1 = (data.pos1 - time) * data.scale1;
                var scaledPos2 = data.pos2 >= data.pos1 ? (data.pos2 - time) * data.scale2 : fixedEndtime;
                if(scaledPos1 < 0) scaledPos1 = 0;
                var startPos = refStartPos[data.channel];
                var endPos = refEndPos[data.channel];
                line.from = math.lerp(endPos, startPos, scaledPos1);
                line.to = math.lerp(endPos, startPos, scaledPos2);
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
            var scrollCatchedNormalNotes = new ScrollCatchedNormalNotes {
                time = time,
                refStartPos = new NativeArray<float3>(refStartPos, Allocator.TempJob),
                refEndPos = new NativeArray<float3>(refEndPos, Allocator.TempJob),
            };
            inputDeps = scrollCatchedNormalNotes.Schedule(this, inputDeps);
            var scrollLongNotes = new ScrollLongNotes {
                fixedEndtime = 3F,
                time = time,
                refStartPos = new NativeArray<float3>(refStartPos, Allocator.TempJob),
                refEndPos = new NativeArray<float3>(refEndPos, Allocator.TempJob),
            };
            inputDeps = scrollLongNotes.Schedule(this, inputDeps);
            var scrollCatchedLongNotes = new ScrollCatchedLongNotes {
                fixedEndtime = 3F,
                time = time,
                refStartPos = new NativeArray<float3>(refStartPos, Allocator.TempJob),
                refEndPos = new NativeArray<float3>(refEndPos, Allocator.TempJob),
            };
            inputDeps = scrollCatchedLongNotes.Schedule(this, inputDeps);
            time += Time.unscaledDeltaTime;
            return inputDeps;
        }
    }
}