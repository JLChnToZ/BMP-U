using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace BananaBeats.Visualization {
    public class SpinningSystem: JobComponentSystem {
        protected override JobHandle OnUpdate(JobHandle jobHandle) {
            float time = Time.DeltaTime;
            jobHandle = Entities
                .ForEach((ref Rotation rotation, in Spinning spinning) =>
                    rotation.Value = math.mul(rotation.Value, quaternion.Euler(spinning.velocity * time)))
                .Schedule(jobHandle);
            return jobHandle;
        }
    }
}
