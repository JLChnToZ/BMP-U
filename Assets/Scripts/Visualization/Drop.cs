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
        public static float scale = 10F;

        protected override JobHandle OnUpdate(JobHandle jobHandle) {
            float time = Time.DeltaTime * scale;

            jobHandle = Entities
                .ForEach((ref Translation translation, ref Drop drop) => {
                    drop.lerp = math.lerp(drop.lerp, 1, time);
                    translation.Value.y = math.lerp(drop.from, translation.Value.y, drop.lerp);
                })
                .Schedule(jobHandle);

            jobHandle.Complete();
            return jobHandle;
        }
    }
}
