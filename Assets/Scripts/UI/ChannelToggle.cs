using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using BMS;
using BananaBeats.Layouts;

namespace BananaBeats.UI {
    public class ChannelToggle: MonoBehaviour {
        public Toggle toggle;
        public Text description;
        public int channelId;

        public IObservable<BMSKeyLayout> ToggleChanged =>
            toggle.onValueChanged
                .AsObservable()
                .TakeUntilDestroy(this)
                .Select(value => value ? NoteLayoutManager.ChannelToLayout(channelId) : BMSKeyLayout.None);

        public ChannelToggle Init(int channelId) {
            this.channelId = channelId;
            description.text = channelId.ToString();
            return this;
        }
    }
}
