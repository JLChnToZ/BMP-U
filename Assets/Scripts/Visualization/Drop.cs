using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
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
        public static float scale = 10F;

        [BurstCompile]
        private new struct Job: IJobForEach<Translation, Drop> {
            public float time;

            public void Execute(ref Translation translation, ref Drop drop) {
                drop.lerp = math.lerp(drop.lerp, 1, time);
                translation.Value.y = math.lerp(drop.from, translation.Value.y, drop.lerp);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            return new Job {
                time = Time.DeltaTime * scale,
            }.Schedule(this, inputDeps);
        }
    }
}
