using UnityEngine;
using UnityEngine.UI;

namespace BananaBeats.UI {
    public class LayoutPresetEntry: MonoBehaviour {
        public Text display;
        public int channelId;

        public LayoutPresetEntry Init(int channelId) {
            display.text = channelId.ToString();
            this.channelId = channelId;
            return this;
        }
    }
}
