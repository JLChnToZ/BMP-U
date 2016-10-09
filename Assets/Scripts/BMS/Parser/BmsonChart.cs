using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using LitJson;

namespace BMS {
    // Reference: https://bmson-spec.readthedocs.io/en/master/doc/
    // Notes to modefiers / users: We are using a forked and patched version of the
    // LitJson library which provides several extensions:
    // https://github.com/JLChnToZ/LitJson
    public class BmsonChart: Chart {
        string rawBmsonContent;
        string modeHint;
        string chartName;
        int tickResoultion;
        JsonData bmsonData;

        public override string Title {
            get {
                if(string.IsNullOrEmpty(chartName))
                    return title;
                return string.Format("{0} [{1}]", title, chartName);
            }
        }

        public override string RawContent {
            get {
                if(!string.IsNullOrEmpty(rawBmsonContent))
                    return rawBmsonContent;
                if(bmsonData != null)
                    return JsonMapper.ToJson(bmsonData);
                return string.Empty;
            }
        }

        public string ChartName {
            get { return chartName; }
        }

        public string ModeHint {
            get { return modeHint; }
        }

        public BmsonChart(string jsonString) {
            rawBmsonContent = jsonString;
        }

        private BMSEvent DefaultReferencePoint {
            get {
                return new BMSEvent {
                    type = BMSEventType.BPM,
                    ticks = 0,
                    time = TimeSpan.Zero,
                    data2 = BitConverter.DoubleToInt64Bits(initialBPM)
                };
            }
        }

        public override void Parse(ParseType parseType) {
            ResetAllData(parseType);
            ParseJson();
            if((parseType & ParseType.Header) == ParseType.Header)
                ParseHeader();

            List<BMSEvent> bmev;
            if((parseType & ParseType.Content) == ParseType.Content) {
                bmev = new List<BMSEvent>();
                ParseBpmEvents(bmev);
                ParseStopEvents(bmev);
                ParseLineEvents(bmev);
                CalculateTimingPoints(bmev);
                AddEvents(bmev);

                ParseSoundChannels(bmev);
            } else {
                bmev = null;
            }
            ParseBGAEvents(parseType, bmev);

            base.Parse(parseType);
        }

        private void ParseJson() {
            if(bmsonData != null) return;
            bmsonData = JsonMapper.ToObject(rawBmsonContent);
        }

        private void ParseHeader() {
            IJsonWrapper info = bmsonData.GetChild("info");
            title = info.GetChild("title").AsString();
            subTitle = info.GetChild("subtitle").AsString();
            artist = info.GetChild("artist").AsString();
            subArtist = string.Join("\n",
                info.GetChild("subartists").GetChilds()
                .Select(entry => entry.AsString())
                .ToArray()
            );
            genre = info.GetChild("genre").AsString();
            modeHint = info.GetChild("mode_hint").AsString("beat-7k");
            chartName = info.GetChild("chart_name").AsString();
            playLevel = info.GetChild("level").AsInt32();
            initialBPM = info.GetChild("init_bpm").AsSingle();
            minBpm = Math.Min(minBpm, initialBPM);

            AddMetaImageResource(-2, info.GetChild("banner_image"));
            AddMetaImageResource(-1, info.GetChild("eyecatch_image"));
            AddMetaImageResource(-3, info.GetChild("back_image"));

            tickResoultion = info.GetChild("resolution").AsInt32(240);
        }

        private void AddMetaImageResource(long id, IJsonWrapper rawData) {
            string path = rawData.AsString();
            if(!string.IsNullOrEmpty(path))
                AddResource(ResourceType.bmp, id, path);
        }

        private void ParseLineEvents(List<BMSEvent> bmev) {
            int[] lines = bmsonData.GetChild("lines").GetChilds()
                .Select(line => line.GetChild("y").AsInt32()).ToArray();
            for(int i = 0, l = lines.Length - 1, lastTickLength = 0; i < l; i++) {
                int tickLength = lines[i + 1] - lines[i];
                if(tickLength != lastTickLength) {
                    bmev.InsertInOrdered(new BMSEvent {
                        type = BMSEventType.BeatReset,
                        ticks = lines[i],
                        data2 = BitConverter.DoubleToInt64Bits((double)tickLength / tickResoultion)
                    });
                    lastTickLength = tickLength;
                }
            }
        }

        private void ParseBpmEvents(List<BMSEvent> bmev) {
            bmev.InsertInOrdered(DefaultReferencePoint);
            foreach(IJsonWrapper bpmEvent in bmsonData.GetChild("bpm_events").GetChilds()) {
                double bpm = bpmEvent.GetChild("bpm").AsDouble();
                bmev.InsertInOrdered(new BMSEvent {
                    type = BMSEventType.BPM,
                    ticks = bpmEvent.GetChild("y").AsInt32(),
                    data2 = BitConverter.DoubleToInt64Bits(bpm)
                });
                minBpm = Math.Min((float)bpm, minBpm);
            }
        }

        private void ParseStopEvents(List<BMSEvent> bmev) {
            foreach(IJsonWrapper stopEvent in bmsonData.GetChild("stop_events").GetChilds())
                bmev.InsertInOrdered(new BMSEvent {
                    type = BMSEventType.STOP,
                    ticks = stopEvent.GetChild("y").AsInt32(),
                    data2 = stopEvent.GetChild("duration").AsInt64()
                });
        }

        private void CalculateTimingPoints(List<BMSEvent> bmev) {
            BMSEvent referencePoint = DefaultReferencePoint;
            for(int i = 0, l = bmev.Count; i < l; i++) {
                BMSEvent ev = bmev[i];
                ev.time = TicksToTime(referencePoint, ev.ticks);
                switch(ev.type) {
                    case BMSEventType.BPM:
                        if(referencePoint.data2 != ev.data2)
                            referencePoint = ev;
                        break;
                    case BMSEventType.STOP:
                        referencePoint.time = TicksToTime(referencePoint, (int)ev.data2);
                        break;
                }
                bmev[i] = ev;
            }
        }

        private void ParseSoundChannels(List<BMSEvent> bmev) {
            IList soundChannels = bmsonData.GetChild("sound_channels");
            if(soundChannels == null) return;
            for(int i = 0, l = soundChannels.Count; i < l; i++)
                ParseSoundChannel(bmev, soundChannels[i] as IJsonWrapper, i + 1);
        }

        private void ParseSoundChannel(List<BMSEvent> bmev, IJsonWrapper rawData, int index) {
            string name = rawData.GetChild("name").AsString();
            if(string.IsNullOrEmpty(name)) return;
            AddResource(ResourceType.wav, index, name);
            
            BMSEvent? lastEvent = null;
            TimeSpan slicedPosition = TimeSpan.Zero;
            IList<BMSEvent> events = Events;

            foreach(IJsonWrapper note in rawData.GetChild("notes").GetChilds()) {
                BMSEvent currentEvent;
                int ticks = note.GetChild("y").AsInt32();
                int channelId = GetChannelMap(note.GetChild("x").AsInt32());
                int length = note.GetChild("l").AsInt32();
                TimeSpan currentTime = CalculateTime(bmev, ticks);

                if(lastEvent.HasValue) {
                    currentEvent = lastEvent.Value;
                    int lastIndex = FindEventIndex(currentEvent);
                    if(note.GetChild("c").AsBoolean()) {
                        currentEvent.sliceEnd = slicedPosition = currentTime - currentEvent.time + currentEvent.sliceStart;
                    } else {
                        currentEvent.sliceEnd = TimeSpan.MaxValue;
                        slicedPosition = TimeSpan.Zero;
                    }
                    ReplaceEvent(lastIndex, currentEvent);
                }

                if(length > 0) {
                    TimeSpan endTime = CalculateTime(bmev, ticks + length);
                    lastEvent = currentEvent = new BMSEvent {
                        type = BMSEventType.LongNoteStart,
                        ticks = ticks,
                        time = currentTime,
                        data1 = channelId,
                        data2 = index,
                        time2 = endTime - currentTime,
                        sliceStart = slicedPosition,
                        sliceEnd = TimeSpan.MaxValue
                    };
                    AddEvent(currentEvent);
                    AddEvent(new BMSEvent {
                        type = BMSEventType.LongNoteEnd,
                        ticks = ticks + length,
                        time = endTime,
                        data1 = channelId,
                        data2 = index
                    });
                } else {
                    lastEvent = currentEvent = new BMSEvent {
                        type = channelId == 0 ? BMSEventType.WAV : BMSEventType.Note,
                        ticks = ticks,
                        time = currentTime,
                        data1 = channelId,
                        data2 = index,
                        sliceStart = slicedPosition,
                        sliceEnd = TimeSpan.MaxValue
                    };
                    AddEvent(currentEvent);
                }
            }
        }

        private void ParseBGAEvents(ParseType parseType, List<BMSEvent> bmev) {
            IJsonWrapper bga = bmsonData.GetChild("bga");

            if((parseType & ParseType.Resources) == ParseType.Resources)
                foreach(IJsonWrapper entry in bga.GetChild("bga_header").GetChilds())
                    AddResource(
                        ResourceType.bmp,
                        entry.GetChild("id").AsInt32(),
                        entry.GetChild("name").AsString()
                    );

            if((parseType & ParseType.Content) == ParseType.Content) {
                ParseBGALayer(bga.GetChild("bga_events"), 0, bmev);
                ParseBGALayer(bga.GetChild("layer_events"), 1, bmev);
                ParseBGALayer(bga.GetChild("poor_events"), -1, bmev);
            }
        }

        private void ParseBGALayer(IJsonWrapper rawData, int layerId, List<BMSEvent> bmev) {
            foreach(IJsonWrapper entry in rawData.GetChilds()) {
                int ticks = entry.GetChild("y").AsInt32();

                AddEvent(new BMSEvent {
                    type = BMSEventType.BMP,
                    ticks = ticks,
                    time = CalculateTime(bmev, ticks),
                    data1 = layerId,
                    data2 = entry.GetChild("id").AsInt64()
                });
            }
        }

        #region Helper functions for parsing bmson
        private TimeSpan CalculateTime(List<BMSEvent> bmev, int ticks) {
            int index = bmev.BinarySearchIndex(
                new BMSEvent { ticks = ticks },
                BinarySearchMethod.FloorClosest | BinarySearchMethod.LastExact,
                0, -1, TicksComparer.instance
            );

            BMSEvent lastReferencePoint;
            if(index >= 0) {
                lastReferencePoint = bmev[index];
                if(lastReferencePoint.type == BMSEventType.STOP) {
                    if(ticks == lastReferencePoint.ticks)
                        return lastReferencePoint.time;
                    bool foundBpmPoint = false;
                    for(int i = index; i >= 0; i--) {
                        if(bmev[i].type == BMSEventType.BPM) {
                            lastReferencePoint.data2 = bmev[i].data2;
                            foundBpmPoint = true;
                            break;
                        }
                    }
                    if(!foundBpmPoint)
                        lastReferencePoint.data2 = BitConverter.DoubleToInt64Bits(initialBPM);
                }
            } else {
                lastReferencePoint = DefaultReferencePoint;
            }

            return TicksToTime(lastReferencePoint, ticks);
        }

        private TimeSpan TicksToTime(BMSEvent referencePoint, int currentTicks) {
            return referencePoint.time + new TimeSpan(
                (long)Math.Round(
                    (double)(currentTicks - referencePoint.ticks) / tickResoultion /
                    GetDoubleValue(referencePoint) *
                    TimeSpan.TicksPerMinute
                )
            );
        }

        private int GetChannelMap(int x) {
            switch(modeHint) {
                case "beat-5k":
                case "beat-7k":
                case "beat-10k":
                case "beat-14k":
                    //  1  2  3  4  5  6  7  8(Sc)  => 11 12 13 14 15 18 19 16
                    //  9 10 11 12 13 14 15 16(Sc2) => 21 22 23 24 25 28 29 26
                    switch(x) {
                        case 0: return 0;
                        case 14: case 15:
                            return x + 14;
                        case 6: case 7: case 9: case 10:
                        case 11: case 12: case 13:
                            return x + 12;
                        case 16:
                            return 25;
                        case 8:
                            return 16;
                        default:
                            return x + 10;
                    }
                case "popn-5k":
                case "popn-9k":
                    //  1  2  3  4  5  6  7  8  9 => 11 12 13 14 15 22 23 24 25
                    switch(x) {
                        case 0: return 0;
                        case 6: case 7: case 8: case 9:
                            return x + 16;
                        default:
                            return x + 10;
                    }
                default:
                    if(x == 0) return 0;
                    return x + 10;
            }
        }

        private static double GetDoubleValue(BMSEvent bpmEvent) {
            return BitConverter.Int64BitsToDouble(bpmEvent.data2);
        }

        private class TicksComparer: Comparer<BMSEvent> {
            public static readonly TicksComparer instance = new TicksComparer();

            private TicksComparer() { }

            public override int Compare(BMSEvent x, BMSEvent y) {
                return x.ticks.CompareTo(y.ticks);
            }
        }
        #endregion
    }
}
