using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using E7.ECS.LineRenderer;

namespace BananaBeats.Visualization {
    [UpdateInGroup(typeof(SimulationSystemGroup)), UpdateBefore(typeof(EntityDropSystem))]
    public class NoteDisplayScroll: JobComponentSystem {
        public static float time;
        public static float fixedEndTimePos = 10F;
        public static float3[] refStartPos, refEndPos;

        [BurstCompile, ExcludeComponent(typeof(Catched))]
        private struct ScrollNormalNotes: IJobForEach<Note, NoteDisplay, Translation> {
            public float time;
            [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> refStartPos;
            [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> refEndPos;

            public void Execute([ReadOnly] ref Note note, [ReadOnly] ref NoteDisplay pos, ref Translation translation) =>
                translation.Value = math.lerp(refEndPos[note.channel], refStartPos[note.channel], (pos.pos - time) * pos.scale);
        }

        [BurstCompile, RequireComponentTag(typeof(Catched)), ExcludeComponent(typeof(FadeOut))]
        private struct ScrollCatchedNormalNotes: IJobForEach<Note, NoteDisplay, Translation> {
            public float time;
            [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> refStartPos;
            [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> refEndPos;

            public void Execute([ReadOnly] ref Note note, [ReadOnly] ref NoteDisplay pos, ref Translation translation) =>
                translation.Value = math.lerp(refEndPos[note.channel], refStartPos[note.channel], math.max(0, (pos.pos - time) * pos.scale));
        }

        [BurstCompile, ExcludeComponent(typeof(Catched))]
        private struct ScrollLongNoteStart: IJobForEach<Note, LongNoteStart, LineSegment> {
            public float time;
            [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> refStartPos;
            [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> refEndPos;

            public void Execute([ReadOnly] ref Note note, [ReadOnly] ref LongNoteStart pos, ref LineSegment line) =>
                line.from = math.lerp(refEndPos[note.channel], refStartPos[note.channel], (pos.pos - time) * pos.scale);
        }

        [BurstCompile, ExcludeComponent(typeof(LongNoteEnd))]
        private struct ScrollLongNoteNoEnd: IJobForEach<Note, LineSegment> {
            public float endPos;
            [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> refStartPos;
            [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> refEndPos;

            public void Execute([ReadOnly] ref Note note, ref LineSegment line) =>
                line.to = math.lerp(refEndPos[note.channel], refStartPos[note.channel], endPos);
        }

        [BurstCompile]
        private struct ScrollLongNoteEnd: IJobForEach<Note, LongNoteEnd, LineSegment> {
            public float time;
            [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> refStartPos;
            [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> refEndPos;

            public void Execute([ReadOnly] ref Note note, [ReadOnly] ref LongNoteEnd pos, ref LineSegment line) =>
                line.to = math.lerp(refEndPos[note.channel], refStartPos[note.channel], (pos.pos - time) * pos.scale);
        }

        [BurstCompile, RequireComponentTag(typeof(Catched)), ExcludeComponent(typeof(FadeOut))]
        private struct ScrollCatchedLongNoteStart: IJobForEach<Note, LongNoteStart, LineSegment> {
            public float time;
            [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> refStartPos;
            [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
            public NativeArray<float3> refEndPos;

            public void Execute([ReadOnly] ref Note note, [ReadOnly] ref LongNoteStart pos, ref LineSegment line) =>
                line.from = math.lerp(refEndPos[note.channel], refStartPos[note.channel], math.max(0, (pos.pos - time) * pos.scale));
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            if(refStartPos == null || refEndPos == null)
                return inputDeps;
            {
                var job = new ScrollNormalNotes {
                    time = time,
                    refStartPos = new NativeArray<float3>(refStartPos, Allocator.TempJob),
                    refEndPos = new NativeArray<float3>(refEndPos, Allocator.TempJob),
                };
                inputDeps = job.Schedule(this, inputDeps);
            }
            {
                var job = new ScrollCatchedNormalNotes {
                    time = time,
                    refStartPos = new NativeArray<float3>(refStartPos, Allocator.TempJob),
                    refEndPos = new NativeArray<float3>(refEndPos, Allocator.TempJob),
                };
                inputDeps = job.Schedule(this, inputDeps);
            }
            {
                var job = new ScrollLongNoteStart {
                    time = time,
                    refStartPos = new NativeArray<float3>(refStartPos, Allocator.TempJob),
                    refEndPos = new NativeArray<float3>(refEndPos, Allocator.TempJob),
                };
                inputDeps = job.Schedule(this, inputDeps);
            }
            {
                var job = new ScrollLongNoteEnd {
                    time = time,
                    refStartPos = new NativeArray<float3>(refStartPos, Allocator.TempJob),
                    refEndPos = new NativeArray<float3>(refEndPos, Allocator.TempJob),
                };
                inputDeps = job.Schedule(this, inputDeps);
            }
            {
                var job = new ScrollLongNoteNoEnd {
                    endPos = fixedEndTimePos,
                    refStartPos = new NativeArray<float3>(refStartPos, Allocator.TempJob),
                    refEndPos = new NativeArray<float3>(refEndPos, Allocator.TempJob),
                };
                inputDeps = job.Schedule(this, inputDeps);
            }
            {
                var job = new ScrollCatchedLongNoteStart {
                    time = time,
                    refStartPos = new NativeArray<float3>(refStartPos, Allocator.TempJob),
                    refEndPos = new NativeArray<float3>(refEndPos, Allocator.TempJob),
                };
                inputDeps = job.Schedule(this, inputDeps);
            }
            return inputDeps;
        }
    }
}