using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using E7.ECS.LineRenderer;

namespace BananaBeats.Visualization {
    public static class NoteLaneManager {
        private static readonly Lazy<EntityArchetype> noteLaneArchetype = new Lazy<EntityArchetype>(
            () => NoteDisplayManager.World.EntityManager.CreateArchetype(
                typeof(LineSegment),
                typeof(LineStyle),
                typeof(NoteLane)
            )
        );

        public static Material LaneMaterial { get; set; }

        public static float LaneLineWidth { get; set; } = 0.1F;

        public static void Create(Vector3 pos1, Vector3 pos2) {
            var entityManager = NoteDisplayManager.World.EntityManager;
            var instance = entityManager.CreateEntity(noteLaneArchetype.Value);
            entityManager.SetComponentData(instance, new LineSegment {
                from = pos1,
                to = pos2,
                lineWidth = LaneLineWidth,
            });
            entityManager.SetSharedComponentData(instance, new LineStyle {
                material = LaneMaterial,
            });
        }

        public static void Clear() {
            var entityManager = NoteDisplayManager.World.EntityManager;
            if(entityManager != null && entityManager.IsCreated)
                entityManager.DestroyEntity(entityManager.CreateEntityQuery(typeof(NoteLane)));
        }
    }

    public struct NoteLane: IComponentData { }
}
