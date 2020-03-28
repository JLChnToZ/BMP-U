using UnityEngine;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using E7.ECS.LineRenderer;

namespace BananaBeats.Visualization {
    using static NoteDisplayManager;

    public static class NoteLaneManager {
        private static readonly ComponentType[] createTypes = new ComponentType[] {
            typeof(LineSegment),
            typeof(LineStyle),
            typeof(NoteLane),
        };

        private static readonly ComponentType[] clearTypes = new ComponentType[] {
            typeof(NoteLane),
        };

        public static Material LaneMaterial { get; set; }

        public static Material GaugeMaterial { get; set; }

        public static Gradient LaneBeatFlowGradient { get; set; }

        public static float LaneLineWidth { get; set; } = 0.1F;

        public static float LaneGaugeAnimSpeed { get; set; } = 10F;

        public static void CreateLane(Vector3 pos1, Vector3 pos2) =>
            InternalCreate(pos1, pos2, LaneMaterial, GetCommandBuffer());

        public static void CreateGauge(Vector3 pos1, Vector3 pos2) {
            var cmdBuf = GetCommandBuffer();
            var instance = InternalCreate(pos1, pos2, GaugeMaterial, cmdBuf);
            cmdBuf.AddComponent(instance, new NoteLaneLerp {
                timeScale = LaneGaugeAnimSpeed,
                maxValue = pos2,
            });
        }

        private static Entity InternalCreate(Vector3 pos1, Vector3 pos2, Material material, EntityCommandBuffer cmdBuf) {
            var instance = cmdBuf.CreateEntity(EntityManager.CreateArchetype(createTypes));
            cmdBuf.SetComponent(instance, new LineSegment {
                from = pos1,
                to = pos2,
                lineWidth = LaneLineWidth,
            });
            cmdBuf.SetSharedComponent(instance, new LineStyle {
                material = material,
            });
            return instance;
        }

        public static void Clear() {
            GetCommandBuffer().DestroyEntity(EntityManager.CreateEntityQuery(clearTypes));
        }

        public static void SetBeatFlowEffect(float value) {
            if(LaneMaterial == null || LaneBeatFlowGradient == null)
                return;
            LaneMaterial.color = LaneBeatFlowGradient.Evaluate(value);
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class NoteLaneLerpSystem: JobComponentSystem {
        EntityCommandBufferSystem cmdBufSystem;
        protected override void OnCreate() =>
            cmdBufSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();

        protected override JobHandle OnUpdate(JobHandle jobHandle) {
            var cmdBuffer = cmdBufSystem.CreateCommandBuffer().ToConcurrent();
            var time = Time.DeltaTime;

            jobHandle = Entities
                .WithAll<NoteLane>()
                .ForEach((ref NoteLaneLerp lerp, ref LineSegment seg) => {
                    lerp.value += time * lerp.timeScale;
                    seg.to = math.lerp(seg.from, lerp.maxValue, math.min(1, lerp.value));
                })
                .Schedule(jobHandle);

            jobHandle = Entities
                .WithAll<NoteLane, LineSegment>()
                .ForEach((Entity entity, int entityInQueryIndex, in NoteLaneLerp lerp) => {
                    if(lerp.value >= 1) cmdBuffer.RemoveComponent<NoteLaneLerp>(entityInQueryIndex, entity);
                })
                .Schedule(jobHandle);

            jobHandle.Complete();
            return jobHandle;
        }
    }

    public struct NoteLane: IComponentData { }

    public struct NoteLaneLerp: IComponentData {
        public float value;
        public float3 maxValue;
        public float timeScale;
    }
}
