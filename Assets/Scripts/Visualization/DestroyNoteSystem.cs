using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using E7.ECS.LineRenderer;
using Unity.Mathematics;
using Unity.Transforms;

namespace BananaBeats.Visualization {
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class DestroyNoteSystem: JobComponentSystem {
        private static readonly Queue<int> queue = new Queue<int>();
        private EntityCommandBufferSystem cmdBufSystem;

        protected override void OnCreate() =>
            cmdBufSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();

        protected override JobHandle OnUpdate(JobHandle jobHandle) {
            if(queue.Count == 0) return jobHandle;

            var cmdBuffer = cmdBufSystem.CreateCommandBuffer().ToConcurrent();

            using(var map = new NativeHashMap<int, bool>(queue.Count, Allocator.TempJob)) {
                while(queue.Count > 0)
                    map.TryAdd(queue.Dequeue(), true);

                jobHandle = Entities
                    .WithReadOnly(map)
                    .WithAll<LongNoteStart, LineSegment>()
                    .ForEach((Entity entity, int entityInQueryIndex, in Note note) => {
                        if(map.ContainsKey(note.id))
                            cmdBuffer.DestroyEntity(entityInQueryIndex, entity);
                    })
                    .Schedule(jobHandle);

                jobHandle = Entities
                    .WithReadOnly(map)
                    .WithNone<LineSegment, FadeOut>()
                    .ForEach((Entity entity, int entityInQueryIndex, in Note note) => {
                        if(map.ContainsKey(note.id)) {
                            cmdBuffer.AddComponent<FadeOut>(entityInQueryIndex, entity);
                            cmdBuffer.AddComponent(entityInQueryIndex, entity, new NonUniformScale {
                                Value = new float3(1),
                            });
                        }
                    })
                    .Schedule(jobHandle);

                jobHandle.Complete();
            }
            return jobHandle;
        }

        public static void Append(int id) => queue.Enqueue(id);
    }
}
