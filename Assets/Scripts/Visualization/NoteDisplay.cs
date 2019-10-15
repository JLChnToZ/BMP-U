using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

namespace BananaBeats.Visualization {
    public struct NoteDisplay: IComponentData {
        public int channel;
        public bool catched;
        public float pos;
        public float scale;
    }

    public struct LongNoteDisplay: IComponentData {
        public int channel;
        public bool catched;
        public float pos1;
        public float pos2;
        public float scale1;
        public float scale2;
    }

    public struct FadeOut: IComponentData {
        public float life;
    }

    public static class NoteDisplayEntity {
        private static readonly Dictionary<int, Entity> notes = new Dictionary<int, Entity>();
        private static readonly Dictionary<int, Entity> endNotes = new Dictionary<int, Entity>();
        private static readonly Dictionary<int, Entity> longNotes = new Dictionary<int, Entity>();

        private static int nextId;

        public static World World {
            get {
                if(world == null) world = World.Active;
                return world;
            }
            set { world = value ?? World.Active; }
        }
        private static World world;

        private static Entity noteEntity;
        private static Entity longNoteEntity;

        public static void ConvertNoteEntity(GameObject prefab) {
            noteEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, World);
        }

        public static void ConvertLongNoteEntity(GameObject prefab) {
            longNoteEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, World);
        }

        public static int CreateNote(int channel, TimeSpan time1, float scale = 1, bool isLongNote = false) {
            var entityManager = World.EntityManager;
            var noteEntity = entityManager.Instantiate(NoteDisplayEntity.noteEntity);
            var pos = (float)time1.Ticks / TimeSpan.TicksPerSecond;
            entityManager.AddComponentData(noteEntity, new NoteDisplay {
                channel = channel,
                pos = pos,
                scale = scale,
            });
            int id = nextId++;
            notes[id] = noteEntity;
            if(isLongNote) {
                var lnEntity = entityManager.Instantiate(longNoteEntity);
                entityManager.AddComponentData(lnEntity, new LongNoteDisplay {
                    channel = channel,
                    pos1 = pos,
                    pos2 = pos - 1,
                    scale1 = scale,
                });
                longNotes[id] = lnEntity;
            }
            return id;
        }

        public static void RegisterLongNoteEnd(int id, TimeSpan time2, float scale = 1) {
            if(!longNotes.TryGetValue(id, out var note))
                return;
            var entityManager = World.EntityManager;
            var data = entityManager.GetComponentData<LongNoteDisplay>(note);
            var pos = (float)time2.Ticks / TimeSpan.TicksPerSecond;
            data.pos2 = pos;
            data.scale2 = scale;
            entityManager.SetComponentData(note, data);
            var noteEntity = entityManager.Instantiate(NoteDisplayEntity.noteEntity);
            entityManager.AddComponentData(noteEntity, new NoteDisplay {
                channel = data.channel,
                pos = pos,
                scale = scale,
            });
            endNotes[id] = noteEntity;
        }

        public static void HitNote(int id, bool isLongEnd = false) {
            var entityManager = World.EntityManager;
            if(notes.TryGetValue(id, out var note)) {
                var data = entityManager.GetComponentData<NoteDisplay>(note);
                data.catched = true;
                entityManager.SetComponentData(note, data);
            }
            if(longNotes.TryGetValue(id, out var longNote)) {
                var data = entityManager.GetComponentData<LongNoteDisplay>(longNote);
                data.catched = true;
                entityManager.SetComponentData(longNote, data);
            }
            if(isLongEnd && endNotes.TryGetValue(id, out var endNote)) {
                var data = entityManager.GetComponentData<NoteDisplay>(endNote);
                data.catched = true;
                entityManager.SetComponentData(endNote, data);
            }
        }

        public static void DestroyNote(int id) {
            var entityManager = World.EntityManager;
            if(notes.TryGetValue(id, out var note)) {
                notes.Remove(id);
                SetFadeOut(note, entityManager);
            }
            if(longNotes.TryGetValue(id, out var longNote)) {
                longNotes.Remove(id);
                SetFadeOut(longNote, entityManager);
            }
            if(endNotes.TryGetValue(id, out var endNote)) {
                endNotes.Remove(id);
                SetFadeOut(endNote, entityManager);
            }
        }

        private static void SetFadeOut(Entity entity, EntityManager entityManager = null) {
            if(entityManager == null)
                entityManager = World.EntityManager;
            if(!entityManager.HasComponent<NonUniformScale>(entity))
                entityManager.AddComponentData(entity, new NonUniformScale { Value = new float3(1) });
            if(!entityManager.HasComponent<FadeOut>(entity))
                entityManager.AddComponent<FadeOut>(entity);
        }

        public static void RegisterPosition(Vector3[] refStartPos, Vector3[] refEndPos) {
            NoteDisplayScroll.refStartPos = Array.ConvertAll(refStartPos, V3toF3);
            NoteDisplayScroll.refEndPos = Array.ConvertAll(refEndPos, V3toF3);
        }

        public static void SetTime(TimeSpan time) {
            NoteDisplayScroll.time = (float)time.Ticks / TimeSpan.TicksPerSecond;
        }

        private static float3 V3toF3(Vector3 vector3) => vector3;
    }

}