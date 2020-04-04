using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

namespace BananaBeats.Visualization {
    using static NoteDisplayManager;

    [UpdateInGroup(typeof(SimulationSystemGroup)), UpdateBefore(typeof(EntityDropSystem))]
    public class NoteDisplayScroll: JobComponentSystem {
        protected override JobHandle OnUpdate(JobHandle jobHandle) {
            if(refStartPos != null && refEndPos != null) {
                float endPos = FixedEndTimePos;
                float time = ScrollPos;
                float scale = ScrollSpeed;
                using(var refStartPos = new NativeArray<float3>(NoteDisplayManager.refStartPos, Allocator.TempJob))
                using(var refEndPos = new NativeArray<float3>(NoteDisplayManager.refEndPos, Allocator.TempJob)) {
                    jobHandle = Entities
                        .WithReadOnly(refStartPos)
                        .WithReadOnly(refEndPos)
                        .WithNone<Catched>()
                        .ForEach((ref Translation translation, in Note note, in NoteDisplay pos) =>
                            translation.Value = math.lerp(refEndPos[note.channel], refStartPos[note.channel], (pos.pos - time) * pos.scale * scale))
                        .Schedule(jobHandle);

                    jobHandle = Entities
                        .WithReadOnly(refStartPos)
                        .WithReadOnly(refEndPos)
                        .WithAll<Catched>()
                        .WithNone<FadeOut>()
                        .ForEach((ref Translation translation, in Note note, in NoteDisplay pos) =>
                            translation.Value = math.lerp(refEndPos[note.channel], refStartPos[note.channel], math.max(0, (pos.pos - time) * pos.scale * scale)))
                        .Schedule(jobHandle);

                    jobHandle.Complete();
                }
            }
            return jobHandle;
        }
    }
}