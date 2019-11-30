using System;
using System.Runtime.CompilerServices;
using Unity.Jobs;
using Unity.Entities;

namespace BananaBeats.Visualization {
    internal static class Helper {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JobHandle Chain<T>(this JobHandle jobHandle, ComponentSystemBase system, T job)
            where T : struct, JobForEachExtensions.IBaseJobForEach =>
            job.Schedule(system, jobHandle);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JobHandle Chain(this JobHandle jobHandle, EntityCommandBufferSystem system) {
            system.AddJobHandleForProducer(jobHandle);
            return jobHandle;
        }
    }
}
