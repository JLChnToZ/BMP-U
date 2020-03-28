using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace BananaBeats.Visualization {
    public class HitNoteSystem: JobComponentSystem {
        private static readonly Queue<Data> queue = new Queue<Data>();
        private EntityCommandBufferSystem cmdBufSystem;

        protected override void OnCreate() =>
            cmdBufSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();

        protected override JobHandle OnUpdate(JobHandle jobHandle) {
            if(queue.Count == 0)
                return jobHandle;
            var cmdBuffer = cmdBufSystem.CreateCommandBuffer().ToConcurrent();
            using(var map = new NativeHashMap<int, bool>(queue.Count, Allocator.TempJob)) {
                while(queue.Count > 0) {
                    var data = queue.Dequeue();
                    map.TryAdd(data.id, data.isEnd);
                }

                jobHandle = Entities
                    .WithReadOnly(map)
                    .WithNone<LongNoteEnd>()
                    .ForEach((Entity entity, int entityInQueryIndex, in Note note) => {
                        if(map.ContainsKey(note.id))
                            cmdBuffer.AddComponent<Catched>(entityInQueryIndex, entity);
                    })
                    .Schedule(jobHandle);

                jobHandle = Entities
                    .WithReadOnly(map)
                    .WithAll<LongNoteEnd>()
                    .ForEach((Entity entity, int entityInQueryIndex, in Note note) => {
                        if(map.TryGetValue(note.id, out bool isEnd) && isEnd)
                            cmdBuffer.AddComponent<Catched>(entityInQueryIndex, entity);
                    })
                    .Schedule(jobHandle);

                jobHandle.Complete();
            }
            return jobHandle;
        }

        public static void Append(int id, bool isEnd) => queue.Enqueue(new Data {
            id = id,
            isEnd = isEnd,
        });

        private struct Data {
            public int id;
            public bool isEnd;
        }
    }
}
