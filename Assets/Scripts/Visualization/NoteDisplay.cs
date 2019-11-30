using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using E7.ECS.LineRenderer;

namespace BananaBeats.Visualization {

    public struct Note: IComponentData {
        public int channel;
    }

    public struct NoteDisplay: IComponentData {
        public float pos;
        public float scale;
    }

    public struct LongNoteStart: IComponentData {
        public float pos;
        public float scale;
    }

    public struct LongNoteEnd: IComponentData {
        public float pos;
        public float scale;
    }

    public struct Catched: IComponentData { }

    public struct FadeOut: IComponentData {
        public float life;
    }

    public enum NoteType: byte {
        Normal,
        LongStart,
        LongBody,
        LongEnd,
        Fake,
    }

    public static class NoteDisplayManager {
        private struct NoteEntiyInstance {
            public Entity noteStart;
            public bool hasNoteEnd;
            public Entity noteEnd;
            public bool hasLongNoteBody;
            public Entity longNoteBody;
        }

        private static readonly Dictionary<NoteType, Entity> prefabs = new Dictionary<NoteType, Entity>();
        private static readonly Dictionary<int, NoteEntiyInstance> instances = new Dictionary<int, NoteEntiyInstance>();

        public static Material LongNoteMaterial { get; set; }

        public static float LongNoteLineWidth { get; set; } = 1;

        public static float DropFrom { get; set; } = 10;

        private static readonly Lazy<EntityArchetype> longNoteBodyArchetype = new Lazy<EntityArchetype>(
            () => World.EntityManager.CreateArchetype(
                typeof(LineSegment),
                typeof(LineStyle),
                typeof(Note),
                typeof(LongNoteStart)
            )
        );

        private static int nextId;

        private static World world;
        public static World World {
            get {
                if(world == null) world = World.DefaultGameObjectInjectionWorld;
                return world;
            }
            set { world = value ?? World.DefaultGameObjectInjectionWorld; }
        }

        public static void ConvertPrefab(GameObject prefab, NoteType noteType) {
            prefabs[noteType] = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, GameObjectConversionSettings.FromWorld(World, null));
        }

        public static int Spawn(int channel, TimeSpan time, NoteType noteType, float scale = 1) {
            var entityManager = World.EntityManager;
            var entityInstance = new NoteEntiyInstance();
            var pos = (float)time.Ticks / TimeSpan.TicksPerSecond;
            switch(noteType) {
                case NoteType.Normal:
                    entityInstance.noteStart = entityManager.Instantiate(prefabs[NoteType.Normal]);
                    break;
                case NoteType.LongStart:
                    entityInstance.noteStart = entityManager.Instantiate(prefabs[NoteType.LongStart]);
                    entityInstance.longNoteBody = entityManager.CreateEntity(longNoteBodyArchetype.Value);
                    entityInstance.hasLongNoteBody = true;
                    break;
                case NoteType.Fake:
                    entityInstance.noteStart = entityManager.Instantiate(prefabs[NoteType.Fake]);
                    break;
                default:
                    throw new ArgumentException("Invalid note type.", nameof(noteType));
            }
            entityManager.AddComponentData(entityInstance.noteStart, new Note {
                channel = channel,
            });
            entityManager.AddComponentData(entityInstance.noteStart, new NoteDisplay {
                pos = pos,
                scale = scale,
            });
            if(DropFrom > 0) 
                entityManager.AddComponentData(entityInstance.noteStart, new Drop {
                    from = DropFrom,
                });
            if(entityInstance.hasLongNoteBody) {
                entityManager.SetComponentData(entityInstance.longNoteBody, new Note {
                    channel = channel,
                });
                entityManager.SetComponentData(entityInstance.longNoteBody, new LongNoteStart {
                    pos = pos,
                    scale = scale,
                });
                entityManager.SetSharedComponentData(entityInstance.longNoteBody, new LineStyle {
                    material = LongNoteMaterial,
                });
                entityManager.SetComponentData(entityInstance.longNoteBody, new LineSegment {
                    lineWidth = LongNoteLineWidth,
                });
            }
            int id = nextId++;
            instances[id] = entityInstance;
            return id;
        }

        public static void SetEndNoteTime(int id, TimeSpan time, float scale = 1) {
            if(!instances.TryGetValue(id, out var entityInstance) || !entityInstance.hasLongNoteBody || entityInstance.hasNoteEnd)
                return;
            var entityManager = World.EntityManager;
            var data = entityManager.GetComponentData<Note>(entityInstance.noteStart);
            var pos = (float)time.Ticks / TimeSpan.TicksPerSecond;
            entityManager.AddComponentData(entityInstance.longNoteBody, new LongNoteEnd {
                pos = pos,
                scale = scale,
            });
            entityInstance.noteEnd = entityManager.Instantiate(prefabs[NoteType.LongEnd]);
            entityInstance.hasNoteEnd = true;
            entityManager.AddComponentData(entityInstance.noteEnd, new Note {
                channel = data.channel,
            });
            entityManager.AddComponentData(entityInstance.noteEnd, new NoteDisplay {
                pos = pos,
                scale = scale,
            });
            if(DropFrom > 0)
                entityManager.AddComponentData(entityInstance.noteEnd, new Drop {
                    from = DropFrom,
                });
            instances[id] = entityInstance;
        }

        public static void HitNote(int id, bool isEnd) {
            if(!instances.TryGetValue(id, out var entityInstance))
                return;
            var entityManager = World.EntityManager;
            SetCatched(ref entityInstance.noteStart, entityManager);
            if(entityInstance.hasLongNoteBody)
                SetCatched(ref entityInstance.longNoteBody, entityManager);
            if(isEnd && entityInstance.hasNoteEnd)
                SetCatched(ref entityInstance.noteEnd, entityManager);
        }

        public static void Destroy(int id) {
            if(!instances.TryGetValue(id, out var entityInstance))
                return;
            var entityManager = World.EntityManager;
            SetFadeOut(ref entityInstance.noteStart, entityManager);
            if(entityInstance.hasLongNoteBody)
                entityManager.DestroyEntity(entityInstance.longNoteBody);
            if(entityInstance.hasNoteEnd)
                SetFadeOut(ref entityInstance.noteEnd, entityManager);
            instances.Remove(id);
        }

        public static void Clear() {
            instances.Clear();
            var entityManager = World.EntityManager;
            if(entityManager != null && entityManager.IsCreated) {
                entityManager.DestroyEntity(entityManager.CreateEntityQuery(typeof(NoteDisplay)));
                entityManager.DestroyEntity(entityManager.CreateEntityQuery(typeof(LongNoteStart)));
            }
        }

        private static void SetFadeOut(ref Entity entity, EntityManager entityManager = null) {
            if(entityManager == null)
                entityManager = World.EntityManager;
            if(!entityManager.HasComponent<NonUniformScale>(entity))
                entityManager.AddComponentData(entity, new NonUniformScale { Value = new float3(1) });
            if(!entityManager.HasComponent<FadeOut>(entity))
                entityManager.AddComponent<FadeOut>(entity);
        }

        private static void SetCatched(ref Entity entity, EntityManager entityManager = null) {
            if(entityManager == null)
                entityManager = World.EntityManager;
            if(!entityManager.HasComponent<Catched>(entity))
                entityManager.AddComponent<Catched>(entity);
        }

        public static void RegisterPosition(Vector3[] refStartPos, Vector3[] refEndPos) {
            NoteDisplayScroll.refStartPos = Array.ConvertAll(refStartPos, V3toF3);
            NoteDisplayScroll.refEndPos = Array.ConvertAll(refEndPos, V3toF3);
        }

        private static float3 V3toF3(Vector3 vector3) => vector3;
    }
}