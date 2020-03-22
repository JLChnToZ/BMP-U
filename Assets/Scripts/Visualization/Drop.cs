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

        protected override JobHandle OnUpdate(JobHandle jobHandle) {
            jobHandle = Entities
                .WithAll<Translation, Drop>()
                .ForEach((ref Translation translation, ref Drop drop) =>
                    translation.Value.y = math.lerp(drop.from, translation.Value.y, drop.lerp))
                .Schedule(jobHandle);
            return jobHandle;
        }
    }
}
