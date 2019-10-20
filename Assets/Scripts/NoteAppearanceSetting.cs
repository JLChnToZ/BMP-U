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
        public Material gaugeMaterial;
        public Gradient laneBeatFlowGradiant;
        public float laneLineWidth = 0.1F;
        public float dropFrom = 10F;
        public float dropSpeed = 10F;
        public float gaugeAnimationSpeed = 10F;

        protected void OnEnable() {
#if UNITY_EDITOR
            if(!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;
#endif
            Init();
        }

        public void Init() {
            if(notePrefabs != null)
                foreach(var m in notePrefabs)
                    NoteDisplayManager.ConvertPrefab(m.prefab, m.noteType);
            NoteDisplayManager.LongNoteMaterial = longNoteBodyMaterial;
            NoteDisplayManager.DropFrom = dropFrom;
            EntityDropSystem.scale = dropSpeed;
            NoteLaneManager.LaneMaterial = laneMaterial;
            NoteLaneManager.LaneLineWidth = laneLineWidth;
            NoteLaneManager.GaugeMaterial = gaugeMaterial;
            NoteLaneManager.LaneBeatFlowGradient = laneBeatFlowGradiant;
            NoteLaneManager.LaneGaugeAnimSpeed = gaugeAnimationSpeed;
        }
    }
}
