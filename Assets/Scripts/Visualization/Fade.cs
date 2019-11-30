using Unity.Jobs;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

using UnityTime = UnityEngine.Time;

namespace BananaBeats.Visualization {
    public class Fade: JobComponentSystem {
        private EntityCommandBufferSystem cmdBufSystem;

        protected override void OnCreate() =>
            cmdBufSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();

        [BurstCompile]
        private struct FadeJob: IJobForEach<FadeOut, NonUniformScale> {
            public float time;
            public float timeScale;

            public void Execute(ref FadeOut data, ref NonUniformScale nonUniformScale) {
                var scale = nonUniformScale.Value;
                data.life += time;
                scale.x = math.lerp(scale.x, 0, time * timeScale);
                scale.y = math.lerp(scale.y, 0, time * timeScale);
                nonUniformScale.Value = scale;
            }
        }

        [RequireComponentTag(typeof(NonUniformScale))]
        private struct DestroyJob: IJobForEachWithEntity<FadeOut> {
            public float maxTime;
            public EntityCommandBuffer.Concurrent cmdBuffer;

            public void Execute(Entity entity, int index, [ReadOnly] ref FadeOut data) {
                if(data.life > maxTime)
                    cmdBuffer.DestroyEntity(index, entity);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) =>
            inputDeps.Chain(this, new FadeJob {
                time = UnityTime.unscaledDeltaTime,
                timeScale = 10,
            }).Chain(this, new DestroyJob {
                maxTime = 1,
                cmdBuffer = cmdBufSystem.CreateCommandBuffer().ToConcurrent(),
            }).Chain(cmdBufSystem);
    }
}
