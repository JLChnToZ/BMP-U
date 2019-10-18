using System;
using System.Collections.Generic;
using UnityEngine;

namespace BananaBeats.Visualization {
    public class BGADisplayManager: MonoBehaviour {
        public BGAConfig[] bgaConfigs;
        private BMSPlayer player;
        private readonly HashSet<BGADisplay> instaniatedBGADisplays = new HashSet<BGADisplay>();

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
    }

    [Serializable]
    public struct BGAConfig {
        public Renderer renderer;
        public int channel;
    }
}
