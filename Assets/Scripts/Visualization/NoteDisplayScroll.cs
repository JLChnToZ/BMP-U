using UnityEngine;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using E7.ECS.LineRenderer;

namespace BananaBeats.Visualization {
    [UpdateInGroup(typeof(SimulationSystemGroup)), UpdateBefore(typeof(EntityDropSystem))]
    public class NoteDisplayScroll: JobComponentSystem {
        public static float time;
        public static float scale = 1;
        public static float fixedEndTimePos = 10F;
        public static float3[] refStartPos, refEndPos;

        protected override JobHandle OnUpdate(JobHandle jobHandle) {
            if(refStartPos != null && refEndPos != null) {
                float endPos = fixedEndTimePos;
                using(var refStartPos = new NativeArray<float3>(NoteDisplayScroll.refStartPos, Allocator.TempJob))
                using(var refEndPos = new NativeArray<float3>(NoteDisplayScroll.refEndPos, Allocator.TempJob)) {
                    jobHandle = Entities
                        .WithNone<Catched>()
                        .ForEach((ref Translation translation, in Note note, in NoteDisplay pos) =>
                            translation.Value = math.lerp(refEndPos[note.channel], refStartPos[note.channel], (pos.pos - time) * pos.scale * scale))
                        .Schedule(jobHandle);

                    jobHandle = Entities
                        .WithAll<Catched>()
                        .WithNone<FadeOut>()
                        .ForEach((ref Translation translation, in Note note, in NoteDisplay pos) =>
                            translation.Value = math.lerp(refEndPos[note.channel], refStartPos[note.channel], math.max(0, (pos.pos - time) * pos.scale * scale)))
                        .Schedule(jobHandle);

                    jobHandle = Entities
                        .WithNone<Catched>()
                        .ForEach((ref LineSegment line, in Note note, in LongNoteStart pos) =>
                            line.from = math.lerp(refEndPos[note.channel], refStartPos[note.channel], (pos.pos - time) * pos.scale * scale))
                        .Schedule(jobHandle);

                    jobHandle = Entities
                        .WithNone<LongNoteEnd>()
                        .ForEach((ref LineSegment line, in Note note) =>
                            line.to = math.lerp(refEndPos[note.channel], refStartPos[note.channel], endPos))
                        .Schedule(jobHandle);

                    jobHandle = Entities
                        .ForEach((ref LineSegment line, in Note note, in LongNoteEnd pos) =>
                            line.to = math.lerp(refEndPos[note.channel], refStartPos[note.channel], (pos.pos - time) * pos.scale * scale))
                        .Schedule(jobHandle);

                    jobHandle = Entities
                        .ForEach((ref LineSegment line, in Note note, in LongNoteStart pos) =>
                            line.from = math.lerp(refEndPos[note.channel], refStartPos[note.channel], math.max(0, (pos.pos - time) * pos.scale * scale)))
                        .Schedule(jobHandle);

                    jobHandle.Complete();
                }
            }
            return jobHandle;
        }
    }
}