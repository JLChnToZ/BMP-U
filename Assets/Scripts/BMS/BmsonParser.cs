using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using LitJson;
using BMS.Bmson;

namespace BMS {
    public partial class BMSManager: MonoBehaviour {
        void ParseBmson() {
            IJsonWrapper info = bmsonContent.GetChild("info");
            if(parseHeader) {
                title = info.GetChild("title").AsString();
                string chartName = info.GetChild("chart_name").AsString();
                if(!string.IsNullOrEmpty(chartName))
                    title = string.Concat(title, " [", chartName, "]");
                artist = info.GetChild("artist").AsString();
                IJsonWrapper rawSubArtists = info.GetChild("subArtists");
                if(rawSubArtists != null)
                    subArtist = string.Join("\n", rawSubArtists.GetChilds().Select(a => a.AsString()).ToArray());
                playLevel = info.GetChild("level").AsInt32();
                bpm = currentBPM = minimumBPM = info.GetChild("init_bpm").AsSingle(130);
                stageFilePath = info.GetChild("back_image").AsString();
                bannerFilePath = info.GetChild("banner_image").AsString();
            }
            if(parseBody) {
                string modeHint = info.GetChild("mode_hint").AsString("beat-7k");
                int resolution = info.GetChild("resolution").AsInt32(240);
                int[] barLines = bmsonContent.GetChild("lines").GetChilds().Select(y => y.GetChild("y").AsInt32()).ToArray();
                Array.Sort(barLines);

                List<KeyValuePair<int, float>> bpms = new List<KeyValuePair<int, float>>();
                bpms.InsertInOrdered(
                    bmsonContent
                    .GetChild("bpm_events")
                    .GetChilds()
                    .Select(x => new KeyValuePair<int, float>(
                        x.GetChild("y").AsInt32(),
                        x.GetChild("bpm").AsSingle()
                    )),
                    KeyComparer<int, float>.Default
                );
                bpms.Insert(0, new KeyValuePair<int, float>(0, bpm));
                foreach(var kv in bmsonContent.GetChild("sound_channels").GetChildsKeyValuePair()) {
                    int index = int.Parse(kv.Key);
                    IJsonWrapper channel = kv.Value;
                    wavObjects.Add(index, new ResourceObject(index, ResourceType.wav, channel.GetChild("name").AsString()));
                }
            }
        }
    }
}
