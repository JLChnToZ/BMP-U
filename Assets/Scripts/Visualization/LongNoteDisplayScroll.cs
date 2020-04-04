using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

namespace BananaBeats.Visualization {
    using static NoteDisplayManager;

    [UpdateInGroup(typeof(SimulationSystemGroup)), UpdateBefore(typeof(EntityDropSystem))]
    public class LongNoteDisplayScroll: JobComponentSystem {
        protected override JobHandle OnUpdate(JobHandle jobHandle) {
            if(refStartPos != null && refEndPos != null) {
                float endPos = FixedEndTimePos;
                float deltaTime = Time.DeltaTime;
                float time = ScrollPos;
                float scale = ScrollSpeed;
                float dropLerp = deltaTime * LnNoEndExtendSpeed;
                float extendLerp = deltaTime * LnExtendSpeed;
                using(var refStartPos = new NativeArray<float3>(NoteDisplayManager.refStartPos, Allocator.TempJob))
                using(var refEndPos = new NativeArray<float3>(NoteDisplayManager.refEndPos, Allocator.TempJob)) {
                    jobHandle = Entities
                        .WithNone<Catched>()
                        .ForEach((ref LongNotePos lnPos, in Note note, in LongNoteStart pos) =>
                            lnPos.from = (pos.pos - time) * pos.scale * scale)
                        .Schedule(jobHandle);

                    jobHandle = Entities
                        .WithAll<Catched>()
                        .WithNone<FadeOut>()
                        .ForEach((ref LongNotePos lnPos, in Note note, in LongNoteStart pos) =>
                            lnPos.from = math.max(0, (pos.pos - time) * pos.scale * scale))
                        .Schedule(jobHandle);

                    jobHandle = Entities
                        .WithAll<LongNoteStart, Drop>()
                        .WithNone<LongNoteEnd>()
                        .ForEach((ref LongNotePos lnPos, in Note note) =>
                            lnPos.to = lnPos.from)
                        .Schedule(jobHandle);

                    jobHandle = Entities
                        .WithAll<LongNoteStart>()
                        .WithNone<LongNoteEnd, Drop>()
                        .ForEach((ref LongNotePos lnPos, in Note note) =>
                            lnPos.to = math.lerp(lnPos.to, endPos, dropLerp))
                        .Schedule(jobHandle);

                    jobHandle = Entities
                        .ForEach((ref LongNotePos lnPos, in Note note, in LongNoteEnd pos) =>
                            lnPos.to = math.lerp(lnPos.to, (pos.pos - time) * pos.scale * scale, extendLerp))
                        .Schedule(jobHandle);

                    jobHandle = Entities
                        .WithReadOnly(refStartPos)
                        .WithReadOnly(refEndPos)
                        .ForEach((ref Translation t, ref Rotation r, ref NonUniformScale s, in Note note, in LongNotePos lnPos) => {
                            var from = math.lerp(refEndPos[note.channel], refStartPos[note.channel], lnPos.from);
                            var to = math.lerp(refEndPos[note.channel], refStartPos[note.channel], lnPos.to);
                            t.Value = (from + to) / 2;
                            r.Value = quaternion.LookRotation(to - from, new float3(0, 1, 0));
                            s.Value = new float3(1, 1, math.distance(from, to));
                        })
                        .Schedule(jobHandle);

                    jobHandle.Complete();
                }
            }
            return jobHandle;
        }
    }
}
