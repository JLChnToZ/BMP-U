using System;
using System.Collections.Generic;
using UnityEngine;

namespace BananaBeats.Visualization {
    public class BGADisplayManager: MonoBehaviour {
        public BGAConfig[] bgaConfigs;
        public Renderer dimBackground;
        private BMSPlayer player;
        private readonly HashSet<BGADisplay> instaniatedBGADisplays = new HashSet<BGADisplay>();
        private Material dimMaterial;

        public float DimBackground {
            get => dimBackground.sharedMaterial.color.a;
            set {
                if(dimMaterial == null) {
#if UNITY_EDITOR
                    dimMaterial = new Material(dimBackground.sharedMaterial);
                    dimBackground.sharedMaterial = dimMaterial;
#else
                    dimMaterial = dimBackground.sharedMaterial;
#endif
                }
                var color = dimMaterial.color;
                color.a = value;
                dimMaterial.color = color;
            }
        }

        public void Load(BMSPlayer player) {
            if(player == this.player) return;
            if(bgaConfigs != null) {
                Clear();
                foreach(var bgaCfg in bgaConfigs)
                    instaniatedBGADisplays.Add(new BGADisplay(player, bgaCfg.renderer, bgaCfg.channel));
                this.player = player;
            }
        }

        public void Clear() {
            if(instaniatedBGADisplays.Count > 0) {
                foreach(var handler in instaniatedBGADisplays)
                    handler.Dispose();
                instaniatedBGADisplays.Clear();
            }
        }

        protected void OnDestroy() => Clear();

        public void ApplyConfig(BMSGameConfig config) {
            DimBackground = config.backgroundDim;
        }
    }

    [Serializable]
    public struct BGAConfig {
        public Renderer renderer;
        public int channel;
    }
}
