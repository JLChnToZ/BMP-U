using UnityEngine;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

namespace BananaBeats.Visualization {
    public struct Drop: IComponentData {
        public float from;
        public float lerp;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class EntityDropSystem: JobComponentSystem {
        private EntityCommandBufferSystem cmdBufSystem;

        protected override void OnCreate() =>
            cmdBufSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();

        protected override JobHandle OnUpdate(JobHandle jobHandle) {
            float time = Time.DeltaTime * NoteDisplayManager.DropSpeed;
            var cmdBuffer = cmdBufSystem.CreateCommandBuffer().ToConcurrent();

            jobHandle = Entities
                .ForEach((Entity entity, int entityInQueryIndex, ref Translation translation, ref Drop drop) => {
                    drop.lerp = math.lerp(drop.lerp, 1, time);
                    if(drop.lerp >= 0.999F) {
                        translation.Value.y = drop.from;
                        cmdBuffer.RemoveComponent<Drop>(entityInQueryIndex, entity);
                    } else {
                        translation.Value.y = math.lerp(drop.from, translation.Value.y, drop.lerp);
                    }
                })
                .Schedule(jobHandle);

            jobHandle.Complete();
            return jobHandle;
        }
    }
}
