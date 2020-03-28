using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

using UnityTime = UnityEngine.Time;

namespace BananaBeats.Visualization {
    public class Fade: JobComponentSystem {
        private static readonly float timeScale = 10;
        private static readonly float maxTime = 1;
        private EntityCommandBufferSystem cmdBufSystem;

        protected override void OnCreate() =>
            cmdBufSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();

        protected override JobHandle OnUpdate(JobHandle jobHandle) {
            var cmdBuffer = cmdBufSystem.CreateCommandBuffer().ToConcurrent();
            float time = UnityTime.unscaledDeltaTime;

            jobHandle = Entities
                .ForEach((ref FadeOut data, ref NonUniformScale nonUniformScale) => {
                    var scale = nonUniformScale.Value;
                    data.life += time;
                    scale.x = math.lerp(scale.x, 0, time * timeScale);
                    scale.y = math.lerp(scale.y, 0, time * timeScale);
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
