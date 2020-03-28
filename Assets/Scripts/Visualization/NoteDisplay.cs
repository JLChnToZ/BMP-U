using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using E7.ECS.LineRenderer;

namespace BananaBeats.Visualization {

    public struct Note: IComponentData {
        public int id;
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
        private static readonly Dictionary<NoteType, Entity> prefabs = new Dictionary<NoteType, Entity>();

        private static readonly ComponentType[] lnBodyType = new ComponentType[] {
            typeof(LineSegment),
            typeof(LineStyle),
            typeof(Note),
            typeof(LongNoteStart),
        };

        private static readonly EntityQueryDesc clearType = new EntityQueryDesc {
            Any = new ComponentType[] {
                typeof(NoteDisplay),
                typeof(LongNoteStart),
            },
        };

        public static Material LongNoteMaterial { get; set; }

        public static float LongNoteLineWidth { get; set; } = 1;

        public static float DropFrom {
            get => SetEndNoteTimeSystem.DropFrom;
            set => SetEndNoteTimeSystem.DropFrom = value;
        }

        private static int nextId;

        private static World world;
        public static World World {
            get {
                if(world == null) world = World.DefaultGameObjectInjectionWorld;
                return world;
            }
            set { world = value ?? World.DefaultGameObjectInjectionWorld; }
        }

        internal static EntityManager EntityManager => World.EntityManager;

        internal static EntityCommandBuffer GetCommandBuffer() =>
            World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>().CreateCommandBuffer();

        public static void ConvertPrefab(GameObject prefab, NoteType noteType) {
            if(World == null) return;
            var entity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, GameObjectConversionSettings.FromWorld(World, null));
            prefabs[noteType] = entity;
            switch(noteType) {
                case NoteType.LongEnd:
                    SetEndNoteTimeSystem.LnEnd = entity;
                    break;
            }
        }

        public static int Spawn(int channel, TimeSpan time, NoteType noteType, float scale = 1) {
            var cmdBuf = GetCommandBuffer();
            var pos = (float)time.Ticks / TimeSpan.TicksPerSecond;
            bool hasLongNoteBody = false;
            Entity noteStart;
            Entity longNoteBody;
            switch(noteType) {
                case NoteType.Normal:
                    noteStart = cmdBuf.Instantiate(prefabs[NoteType.Normal]);
                    longNoteBody = default;
                    break;
                case NoteType.LongStart:
                    noteStart = cmdBuf.Instantiate(prefabs[NoteType.LongStart]);
                    longNoteBody = cmdBuf.CreateEntity(World.EntityManager.CreateArchetype(lnBodyType));
                    hasLongNoteBody = true;
                    break;
                case NoteType.Fake:
                    noteStart = cmdBuf.Instantiate(prefabs[NoteType.Fake]);
                    longNoteBody = default;
                    break;
                default:
                    throw new ArgumentException("Invalid note type.", nameof(noteType));
            }
            int id = nextId++;
            cmdBuf.AddComponent(noteStart, new Note {
                channel = channel,
                id = id,
            });
            cmdBuf.AddComponent(noteStart, new NoteDisplay {
                pos = pos,
                scale = scale,
            });
            if(DropFrom > 0)
                cmdBuf.AddComponent(noteStart, new Drop {
                    from = DropFrom,
                });
            if(hasLongNoteBody) {
                cmdBuf.SetComponent(longNoteBody, new Note {
                    channel = channel,
                    id = id,
                });
                cmdBuf.SetComponent(longNoteBody, new LongNoteStart {
                    pos = pos,
                    scale = scale,
                });
                cmdBuf.SetSharedComponent(longNoteBody, new LineStyle {
                    material = LongNoteMaterial,
                });
                cmdBuf.SetComponent(longNoteBody, new LineSegment {
                    lineWidth = LongNoteLineWidth,
                });
            }
            return id;
        }

        public static void SetEndNoteTime(int id, TimeSpan time, float scale = 1) =>
            SetEndNoteTimeSystem.Append(id, time, scale);

        public static void HitNote(int id, bool isEnd) =>
            HitNoteSystem.Append(id, isEnd);

        public static void Destroy(int id) =>
            DestroyNoteSystem.Append(id);

        public static void Clear() =>
            GetCommandBuffer()
            .DestroyEntity(EntityManager.CreateEntityQuery(clearType));

        public static void RegisterPosition(Vector3[] refStartPos, Vector3[] refEndPos) {
            NoteDisplayScroll.refStartPos = Array.ConvertAll(refStartPos, V3toF3);
            NoteDisplayScroll.refEndPos = Array.ConvertAll(refEndPos, V3toF3);
        }

        private static float3 V3toF3(Vector3 vector3) => vector3;
    }
}