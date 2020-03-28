using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace BananaBeats.Visualization {
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class SetEndNoteTimeSystem: JobComponentSystem {
        public static float DropFrom { get; set; } = 10;
        public static Entity LnEnd { get; set; }

        private static readonly Queue<Data> queue = new Queue<Data>();
        private EntityCommandBufferSystem cmdBufSystem;

        protected override void OnCreate() =>
            cmdBufSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();

        protected override JobHandle OnUpdate(JobHandle jobHandle) {
            if(queue.Count == 0) return jobHandle;

            var cmdBuffer = cmdBufSystem.CreateCommandBuffer().ToConcurrent();
            var lnEnd = LnEnd;
            var dropFrom = DropFrom;

            using(var map = new NativeHashMap<int, float2>(queue.Count, Allocator.TempJob)) {
                while(queue.Count > 0) {
                    var data = queue.Dequeue();
                    map.TryAdd(data.id, new float2(data.time, data.scale));
                }
                jobHandle = Entities
                    .WithReadOnly(map)
                    .ForEach((Entity entity, int entityInQueryIndex, in Note note) => {
                        if(map.TryGetValue(note.id, out float2 payload)) {
                            cmdBuffer.AddComponent(entityInQueryIndex, entity, new LongNoteEnd {
                                pos = payload.x,
                                scale = payload.y,
                            });
                            var noteEnd = cmdBuffer.Instantiate(entityInQueryIndex, lnEnd);
                            cmdBuffer.AddComponent(entityInQueryIndex, noteEnd, new Note {
                                channel = note.channel,
                                id = note.id,
                            });
                            cmdBuffer.AddComponent(entityInQueryIndex, noteEnd, new NoteDisplay {
                                pos = payload.x,
                                scale = payload.y,
                            });
                            if(dropFrom > 0)
                                cmdBuffer.AddComponent(entityInQueryIndex, noteEnd, new Drop {
                                    from = dropFrom,
                                });
                        }
                    })
                    .Schedule(jobHandle);
                jobHandle.Complete();
            }
            return jobHandle;
        }

        public static void Append(int id, TimeSpan time, float scale) => queue.Enqueue(new Data {
            id = id,
            time = (float)time.Ticks / TimeSpan.TicksPerSecond,
            scale = scale
        });

        private struct Data {
            public int id;
            public float time;
            public float scale;
        }
    }
}
