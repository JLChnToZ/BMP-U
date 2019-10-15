using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

namespace BananaBeats.Visualization {
    public class Fade: JobComponentSystem {

        BeginInitializationEntityCommandBufferSystem cmdBufSystem;

        protected override void OnCreate() {
            cmdBufSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }

        [BurstCompile]
        private struct Job: IJobForEachWithEntity<FadeOut, NonUniformScale> {
            public float time;
            public float timeScale;
            public float maxTime;
            public EntityCommandBuffer.Concurrent cmdBuffer;

            public void Execute(Entity entity, int index, ref FadeOut data, ref NonUniformScale nonUniformScale) {
                var scale = nonUniformScale.Value;
                data.life += time;
                scale.x = math.lerp(scale.x, 0, time * timeScale);
                scale.y = math.lerp(scale.y, 0, time * timeScale);
                nonUniformScale.Value = scale;
                if(data.life > maxTime)
                    cmdBuffer.DestroyEntity(index, entity);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            var job = new Job {
                time = Time.unscaledDeltaTime,
                maxTime = 1,
                timeScale = 10,
                cmdBuffer = cmdBufSystem.CreateCommandBuffer().ToConcurrent(),
            };
            inputDeps = job.Schedule(this, inputDeps);
            cmdBufSystem.AddJobHandleForProducer(inputDeps);
            return inputDeps;
        }
    }
}
