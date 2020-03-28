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
            WorldInjector.OnInitWorld += Init;
#if UNITY_EDITOR
            if(!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;
#endif
            try {
                Init();
            } catch(Exception ex) {
#if UNITY_EDITOR || DEBUG
                Debug.LogException(ex);
#endif
            }
        }

        protected void OnDestroy() {
            WorldInjector.OnInitWorld -= Init;
        }

        public void Init() {
            if(notePrefabs != null)
                foreach(var m in notePrefabs)
                    NoteDisplayManager.ConvertPrefab(m.prefab, m.noteType);
#if UNITY_EDITOR
            NoteDisplayManager.LongNoteMaterial = Instantiate(longNoteBodyMaterial);
#else
            NoteDisplayManager.LongNoteMaterial = longNoteBodyMaterial;
#endif
            NoteDisplayManager.DropFrom = dropFrom;
            EntityDropSystem.scale = dropSpeed;
#if UNITY_EDITOR
            NoteLaneManager.LaneMaterial = Instantiate(laneMaterial);
#else
            NoteLaneManager.LaneMaterial = laneMaterial;
#endif
            NoteLaneManager.LaneLineWidth = laneLineWidth;
#if UNITY_EDITOR
            NoteLaneManager.GaugeMaterial = Instantiate(gaugeMaterial);
#else
            NoteLaneManager.GaugeMaterial = gaugeMaterial;
#endif
            NoteLaneManager.LaneBeatFlowGradient = laneBeatFlowGradiant;
            NoteLaneManager.LaneGaugeAnimSpeed = gaugeAnimationSpeed;
        }
    }
}
