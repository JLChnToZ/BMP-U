using System;
using BananaBeats.Visualization;
using UnityEngine;

namespace BananaBeats.Configs {
    public class NoteAppearanceSetting: ScriptableObject {
        [Serializable]
        public struct NotePrefab {
            public NoteType noteType;
            public GameObject prefab;
        }

        public NotePrefab[] notePrefabs;
        public Material longNoteBodyMaterial;
        public float longNoteBodyLineWidth = 1;
        public Material laneMaterial;
        public float laneLineWidth = 0.1F;

        protected void OnEnable() => Init();

        public void Init() {
            if(notePrefabs != null)
                foreach(var m in notePrefabs)
                    NoteDisplayManager.ConvertPrefab(m.prefab, m.noteType);
            NoteDisplayManager.LongNoteMaterial = longNoteBodyMaterial;
            NoteLaneManager.LaneMaterial = laneMaterial;
            NoteLaneManager.LaneLineWidth = laneLineWidth;
        }
    }
}
