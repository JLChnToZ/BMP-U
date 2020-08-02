using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

namespace BananaBeats.Visualization {
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class Fade: JobComponentSystem {
        private EntityCommandBufferSystem cmdBufSystem;

        protected override void OnCreate() =>
            cmdBufSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();

        protected override JobHandle OnUpdate(JobHandle jobHandle) {
            var cmdBuffer = cmdBufSystem.CreateCommandBuffer().AsParallelWriter();
            float time = Time.DeltaTime;
            float timeScale = NoteDisplayManager.FadeSpeed;
            float maxTime = NoteDisplayManager.FadeLife;

            jobHandle = Entities
                .ForEach((ref FadeOut data, ref NonUniformScale nonUniformScale) => {
                    var scale = nonUniformScale.Value;
                    data.life += time;
                    scale = math.lerp(scale, 0, time * timeScale);
                    nonUniformScale.Value = scale;
                })
                .Schedule(jobHandle);

            jobHandle = Entities
                .WithAll<NonUniformScale>()
                .ForEach((Entity entity, int entityInQueryIndex, in FadeOut data) => {
                    if(data.life > maxTime) cmdBuffer.DestroyEntity(entityInQueryIndex, entity);
                })
                .Schedule(jobHandle);

            jobHandle.Complete();
            return jobHandle;
        }
    }
}
