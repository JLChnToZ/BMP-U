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
                float time = NoteDisplayScroll.time;
                float scale = NoteDisplayScroll.scale;
                float endPos = NoteDisplayScroll.fixedEndTimePos;
                var refStartPos = new NativeArray<float3>(NoteDisplayScroll.refStartPos, Allocator.Temp);
                var refEndPos = new NativeArray<float3>(NoteDisplayScroll.refEndPos, Allocator.Temp);

                jobHandle = Entities
                    .WithAll<Note, NoteDisplay, Translation>()
                    .WithNone<Catched>()
                    .ForEach((ref Translation translation, in Note note, in NoteDisplay pos) =>
                        translation.Value = math.lerp(refEndPos[note.channel], refStartPos[note.channel], (pos.pos - time) * pos.scale * scale))
                    .Schedule(jobHandle);

                jobHandle = Entities
                    .WithAll<Note, NoteDisplay, Translation>().WithAll<Catched>()
                    .WithNone<FadeOut>()
                    .ForEach((ref Translation translation, in Note note, in NoteDisplay pos) =>
                        translation.Value = math.lerp(refEndPos[note.channel], refStartPos[note.channel], math.max(0, (pos.pos - time) * pos.scale * scale)))
                    .Schedule(jobHandle);

                jobHandle = Entities
                    .WithAll<Note, LongNoteStart, LineSegment>()
                    .WithNone<Catched>()
                    .ForEach((ref LineSegment line, in Note note, in LongNoteStart pos) =>
                        line.from = math.lerp(refEndPos[note.channel], refStartPos[note.channel], (pos.pos - time) * pos.scale * scale))
                    .Schedule(jobHandle);

                jobHandle = Entities
                    .WithAll<Note, LineSegment>()
                    .WithNone<LongNoteEnd>()
                    .ForEach((ref LineSegment line, in Note note) =>
                        line.to = math.lerp(refEndPos[note.channel], refStartPos[note.channel], endPos))
                    .Schedule(jobHandle);

                jobHandle = Entities
                    .WithAll<Note, LineSegment, LongNoteEnd>()
                    .ForEach((ref LineSegment line, in Note note, in LongNoteEnd pos) =>
                        line.to = math.lerp(refEndPos[note.channel], refStartPos[note.channel], (pos.pos - time) * pos.scale * scale))
                    .Schedule(jobHandle);

                jobHandle = Entities
                    .WithAll<Note, LongNoteEnd, LineSegment>()
                    .ForEach((ref LineSegment line, in Note note, in LongNoteStart pos) =>
                        line.from = math.lerp(refEndPos[note.channel], refStartPos[note.channel], math.max(0, (pos.pos - time) * pos.scale * scale)))
                    .Schedule(jobHandle);
            }
            return jobHandle;
        }
    }
}