using System;
using System.Linq;
using System.Collections.Generic;
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

        public static IObservable<BMSKeyLayout> Prepare(
            bool firstRun,
            IDictionary<BMSKeyLayout, ChannelToggle> cache,
            ChannelToggle prefab,
            RectTransform container) {
            if(cache == null) {
                firstRun = true;
                cache = new Dictionary<BMSKeyLayout, ChannelToggle>();
            }
            if(firstRun)
                for(int i = 11; i < 30; i++)
                    if(i % 10 != 0)
                        cache[NoteLayoutManager.ChannelToLayout(i)] = Instantiate(prefab, container).Init(i);
            return cache.Values
                .Select(channelToggle => channelToggle.ToggleChanged
                .Scan((BMSKeyLayout.None, BMSKeyLayout.None), ((BMSKeyLayout, BMSKeyLayout) o, BMSKeyLayout v) => (o.Item2, v)))
                .Merge()
                .Scan(BMSKeyLayout.None, (BMSKeyLayout o, (BMSKeyLayout, BMSKeyLayout) v) => (o & ~v.Item1) | v.Item2);
        }

        public static void UpdateToggles(
            IDictionary<BMSKeyLayout, ChannelToggle> cache,
            BMSKeyLayout layout,
            bool notify = true) {
            foreach(var kvp in cache) {
                bool isOn = (layout & kvp.Key) == kvp.Key;
                if(notify) kvp.Value.toggle.isOn = isOn;
                else kvp.Value.toggle.SetIsOnWithoutNotify(isOn);
            }
        }

        public static void ChangeTogglesEnabled(
            IDictionary<BMSKeyLayout, ChannelToggle> cache,
            BMSKeyLayout enabledLayout) {
            foreach(var kvp in cache)
                kvp.Value.toggle.interactable = (enabledLayout & kvp.Key) == kvp.Key;
        }
    }
}
