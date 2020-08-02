using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;

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

    public struct LongNotePos: IComponentData {
        public float from;
        public float to;
    }

    public struct LongNoteEnd: IComponentData {
        public float pos;
        public float scale;
    }

    public struct Catched: IComponentData { }

    public struct FadeOut: IComponentData {
        public float life;
    }

    public struct Spinning: IComponentData {
        public float3 velocity;
    }

    public enum NoteType: byte {
        Normal,
        LongStart,
        LongBody,
        LongEnd,
        Fake,
    }

    public static class NoteDisplayManager {
        private static class StaticTypes {
            public static readonly EntityQueryDesc clearType = new EntityQueryDesc {
                    Any = new ComponentType[] {
                    typeof(NoteDisplay),
                    typeof(LongNoteStart),
                },
            };
        }

        private static readonly Dictionary<NoteType, Entity> prefabs = new Dictionary<NoteType, Entity>();
        private static int nextId;
        private static World world;

        internal static float3[] refStartPos, refEndPos;

        internal static float4[] mappedColors = new float4[20];

        public static Material LongNoteMaterial { get; set; }

        public static float LongNoteLineWidth { get; set; } = 1;

        public static float FixedEndTimePos { get; set; } = 10;

        public static float ScrollSpeed { get; set; } = 1;

        public static float ScrollPos { get; set; }

        public static float DropFrom { get; set; } = 10;

        public static float DropSpeed { get; set; } = 10;

        public static float FadeSpeed { get; set; } = 10;

        public static float LnNoEndExtendSpeed { get; set; } = 1F;

        public static float LnExtendSpeed { get; set; } = 27.5F;

        public static Vector3 SpinningVelocity { get; set; } = new Vector3(2, 3, 4);

        public static float FadeLife { get; set; } = 1;

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

        internal static Entity LnEnd => prefabs[NoteType.LongEnd];

        public static void ConvertPrefab(GameObject prefab, NoteType noteType) {
            if(World == null) return;
            var entity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, GameObjectConversionSettings.FromWorld(World, null));
            prefabs[noteType] = entity;
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
                    longNoteBody = cmdBuf.Instantiate(prefabs[NoteType.LongBody]);
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
            cmdBuf.SetComponent(noteStart, new Translation {
                Value = new float3(0, 0, float.NegativeInfinity),
            });
            cmdBuf.AddComponent(noteStart, new Note {
                channel = channel,
                id = id,
            });
            cmdBuf.AddComponent(noteStart, new NoteDisplay {
                pos = pos,
                scale = scale,
            });
            cmdBuf.AddComponent(noteStart, new MaterialColor {
                Value = mappedColors[channel],
            });
            if(DropFrom > 0)
                cmdBuf.AddComponent(noteStart, new Drop {
                    from = DropFrom,
                });
            if(hasLongNoteBody) {
                cmdBuf.AddComponent(longNoteBody, new Note {
                    channel = channel,
                    id = id,
                });
                cmdBuf.AddComponent(longNoteBody, new LongNoteStart {
                    pos = pos,
                    scale = scale,
                });
                if(DropFrom > 0)
                    cmdBuf.AddComponent(longNoteBody, new Drop {
                        from = DropFrom,
                    });
                cmdBuf.AddComponent(longNoteBody, new MaterialColor {
                    Value = mappedColors[channel],
                });
                cmdBuf.AddComponent<LongNotePos>(longNoteBody);
                cmdBuf.AddComponent<NonUniformScale>(longNoteBody);
            } else
                cmdBuf.AddComponent(noteStart, new Spinning {
                    velocity = SpinningVelocity * scale,
                });
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
            .DestroyEntity(EntityManager.CreateEntityQuery(StaticTypes.clearType));

        public static void RegisterPosition(Vector3[] refStartPos, Vector3[] refEndPos) {
            NoteDisplayManager.refStartPos = Array.ConvertAll(refStartPos, V3toF3);
            NoteDisplayManager.refEndPos = Array.ConvertAll(refEndPos, V3toF3);
        }

        public static void RegisterColors(Color[] mappedColors) {
            NoteDisplayManager.mappedColors = Array.ConvertAll(mappedColors, ColorToF4);
        }

        private static float3 V3toF3(Vector3 vector3) => vector3;

        private static float4 ColorToF4(Color color) => (Vector4)color;
    }
}