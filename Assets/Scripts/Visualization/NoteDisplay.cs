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
        public float pos1;
        public float pos2;
        public float scale;
    }

    public static class NoteDisplayEntity {
        private static readonly Dictionary<int, Entity> instances = new Dictionary<int, Entity>();
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

        public static void ConvertEntity(GameObject prefab) {
            noteEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, World);
        }

        public static int Create(int channel, TimeSpan time1, float scale = 1, bool isLongNote = false) {
            var entityManager = World.EntityManager;
            var entity = entityManager.Instantiate(noteEntity);
            var pos1 = (float)time1.Ticks / TimeSpan.TicksPerSecond;
            entityManager.AddComponent(entity, typeof(NonUniformScale));
            entityManager.AddComponentData(entity, new NoteDisplay {
                channel = channel,
                pos1 = pos1,
                pos2 = isLongNote ? float.PositiveInfinity : pos1,
                scale = scale,
            });
            int id = nextId++;
            instances[id] = entity;
            return id;
        }

        public static void RegisterLongNoteEnd(int id, TimeSpan time2) {
            if(!instances.TryGetValue(id, out var note))
                return;
            var entityManager = World.EntityManager;
            var data = entityManager.GetComponentData<NoteDisplay>(note);
            data.pos2 = (float)time2.Ticks / TimeSpan.TicksPerSecond;
            entityManager.SetComponentData(note, data);
        }

        public static void HitNote(int id) {

        }

        public static void DestroyNote(int id) {
            if(!instances.TryGetValue(id, out var note))
                return;
            instances.Remove(id);
            World.EntityManager.DestroyEntity(note);
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