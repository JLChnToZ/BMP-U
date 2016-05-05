using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

using Random = System.Random;
using ThreadPriority = System.Threading.ThreadPriority;

namespace BMS {
    public partial class BMSManager: MonoBehaviour {
        static Dictionary<int, int> channelAdvancedMapping;

        string resourcePath;
        string title, subTitle, artist, genre, subArtist, comments;
        int playerCount;
        float bpm, currentBPM;
        int playLevel, rank, lnType;
        float volume;
        bool stageFileLoaded;
        string stageFilePath;
        Texture stageFile;
        TimeSpan duration;

        public string Title { get { return title; } }
        public string Artist { get { return artist; } }
        public string SubArtist { get { return subArtist; } }
        public string Comments { get { return comments; } }
        public string Genre { get { return genre; } }
        public int PlayerCount { get { return playerCount; } }
        public float BPM { get { return isStarted ? currentBPM : bpm; } }
        public float PlayLevel { get { return playLevel; } }
        public int Rank { get { return rank; } }
        public float Volume { get { return volume; } }
        public Texture StageFile { get { return stageFile; } }
        public int LongNoteType { get { return lnType; } }
        public TimeSpan Duration { get { return duration; } }
        public string StageFilePath { get { return stageFilePath; } }
        public bool StageFileLoaded { get { return stageFileLoaded; } }

        readonly Dictionary<int, TimeLine> timeLines = new Dictionary<int, TimeLine>();
        readonly Dictionary<TimeSpan, float> bpms = new Dictionary<TimeSpan, float>();
        TimeSpanHandle<float> beatResetHelper;
        TimeSpanHandle<float> bpmChangeHelper;
        
        bool parseHeader, parseBody, parseAll;
        ResourceObject stageFileObject;
        Thread loadBMSThread;

        public event Action OnStageFileLoaded;

        void StopPreviousBMSLoadJob() {
            if(loadBMSThread == null) return;
            if(loadBMSThread.IsAlive)
                loadBMSThread.Abort();
        }

        void ReloadTimeline(bool parseHeader, bool parseBody, bool direct) {
            StopPreviousBMSLoadJob();
            this.parseHeader = parseHeader;
            this.parseBody = parseBody;
            bmsLoaded = false;
            if(!direct) {
                loadBMSThread = new Thread(ReloadTimelineInThread) {
                    Priority = ThreadPriority.BelowNormal
                };
                if(parseHeader)
                    stageFileLoaded = false;
                StartCoroutine(ReloadTimelineHelperCoroutine(parseHeader, parseBody));
                loadBMSThread.Start();
            } else {
                ReloadTimelineInThread();
            }
        }

        IEnumerator ReloadTimelineHelperCoroutine(bool header, bool body) {
            if(header) {
                stageFileObject = null;
                while(stageFileObject == null && !bmsLoaded) yield return null;
                if(stageFileObject != null) {
                    yield return StartCoroutine(new ResourceLoader(resourcePath).LoadResource(stageFileObject));
                    stageFile = stageFileObject.texture;
                    stageFileLoaded = true;
                    if(OnStageFileLoaded != null)
                        OnStageFileLoaded.Invoke();
                } else
                    stageFileLoaded = true;
            }
            yield break;
        }

        void ReloadTimelineInThread() {
            try {
                if(parseHeader) {
                    title = artist = genre = "Unknown";
                    subTitle = subArtist = comments = string.Empty;
                    playerCount = 1;
                    bpm = currentBPM = 130;
                    playLevel = rank = 0;
                    volume = 1;
                    lnType = 1;
                }
                if(parseBody) {
                    duration = TimeSpan.Zero;
                    timeLines.Clear();
                    bpms.Clear();
                    preTimingHelper = new TimingHelper(preEventOffset);
                    mainTimingHelper = new TimingHelper();
                    preTimingHelper.OnIndexChange += OnPreEvent;
                    mainTimingHelper.OnIndexChange += OnEventUpdate;
                }
                timePosition = TimeSpan.Zero;
                char splitter;
                string[] parameters;
                string cName, cName2, param1, param2;
                bool hasIfBlockExecuted = false;
                int randomParam = 0, ifBlockLevel = 0, skipIfBlockLevel = 0;
                int verse, verseLength, i, channel, value;
                var bpmObjects = new Dictionary<int, float>();
                var bpmMapping = new Dictionary<MeasureBeat, float>();
                var timeSigMapping = new Dictionary<int, float>();
                var random = new Random();
                int randomGroup = -1, randomIndex = -1;
                TimeLine timeLine;
                foreach(var line in bmsContent) {
                    if(string.IsNullOrEmpty(line) || line[0] != '#') continue;
                    splitter = line.IndexOf(':', 1) >= 0 ? ':' : ' ';
                    parameters = line.Split(splitter);
                    cName = parameters[0];
                    param1 = parameters.Length > 1 ? line.Substring(line.IndexOf(splitter) + 1) : string.Empty;
                    if(ifBlockLevel > 0 && skipIfBlockLevel > 0) {
                        switch(cName.ToLower()) {
                            case "#if":
                                ifBlockLevel++;
                                break;
                            case "#elseif":
                                if(ifBlockLevel == skipIfBlockLevel && !hasIfBlockExecuted && randomParam == (randomIndex = int.Parse(param1))) {
                                    skipIfBlockLevel = 0;
                                    hasIfBlockExecuted = true;
                                }
                                break;
                            case "#else":
                                randomIndex = -1;
                                if(ifBlockLevel == skipIfBlockLevel && !hasIfBlockExecuted) {
                                    skipIfBlockLevel = 0;
                                    hasIfBlockExecuted = true;
                                }
                                break;
                            case "#endif":
                                if(ifBlockLevel == skipIfBlockLevel)
                                    skipIfBlockLevel = 0;
                                ifBlockLevel--;
                                hasIfBlockExecuted = false;
                                break;
                        }
                        if(!parseAll)
                            continue;
                    }
                    if(parseHeader)
                        switch(cName.ToLower()) {
                            case "#title": title = param1; continue;
                            case "#artist": artist = param1; continue;
                            case "#subtitle": subTitle += (subTitle.Length > 0 ? "\n" : "") + param1; break;
                            case "#subartist": subArtist += (subArtist.Length > 0 ? "\n" : "") + param1; break;
                            case "#comment": comments += (comments.Length > 0 ? "\n" : "") + param1; break;
                            case "#bpm": float.TryParse(param1, out bpm); continue;
                            case "#genre": genre = param1; continue;
                            case "#player": int.TryParse(param1, out playerCount); continue;
                            case "#playlevel": int.TryParse(param1, out playLevel); continue;
                            case "#rank": int.TryParse(param1, out rank); continue;
                            case "#volwav": if(float.TryParse(param1, out volume)) volume /= 100F; continue;
                            case "#stagefile": stageFileObject = new ResourceObject(-1, ResourceType.bmp, stageFilePath = param1); continue;
                            case "#lntype": int.TryParse(param1, out lnType); continue;
                        }

                    if(!parseBody) continue;

                    switch(cName.ToLower()) {
                        // If/random handling
                        case "#random":
                        case "#setrandom":
                            if(int.TryParse(param1, out randomParam))
                                randomParam = random.Next(randomParam) + 1;
                            randomGroup++;
                            continue;
                        case "#if":
                            ifBlockLevel++;
                            hasIfBlockExecuted = randomParam == (randomIndex = int.Parse(param1));
                            if(!hasIfBlockExecuted)
                                skipIfBlockLevel = ifBlockLevel;
                            continue;
                        case "#elseif":
                            if(hasIfBlockExecuted) {
                                skipIfBlockLevel = ifBlockLevel;
                            } else {
                                hasIfBlockExecuted = randomParam == (randomIndex = int.Parse(param1));
                                if(!hasIfBlockExecuted)
                                    skipIfBlockLevel = ifBlockLevel;
                            }
                            continue;
                        case "#else":
                            randomIndex = -1;
                            if(hasIfBlockExecuted)
                                skipIfBlockLevel = ifBlockLevel;
                            continue;
                        case "#endif":
                            ifBlockLevel--;
                            skipIfBlockLevel = 0;
                            hasIfBlockExecuted = false;
                            continue;
                    }
                    if(cName.Length > 2) {
                        cName2 = cName.Substring(1, 3);
                        param2 = cName.Substring(4);
                    } else {
                        cName2 = cName;
                        param2 = string.Empty;
                    }
                    switch(cName2.ToLower()) {
                        case "wav": GetDataObject(ResourceType.wav, Base36.Decode(param2), param1); continue;
                        case "bmp": GetDataObject(ResourceType.bmp, Base36.Decode(param2), param1); continue;
                        case "bpm":
                            if(cName.ToLower() == "bpm") float.TryParse(param1, out bpm);
                            else bpmObjects[Base36.Decode(param2)] = float.Parse(param1);
                            continue;
                        case "bga":
                            Vector2 pos1 = new Vector2(float.Parse(parameters[2]), float.Parse(parameters[3]));
                            Vector2 pos2 = new Vector2(float.Parse(parameters[4]), float.Parse(parameters[5]));
                            var bga = new BGAObject {
                                index = Base36.Decode(parameters[1]),
                                clipArea = new Rect(pos1, pos2 - pos1),
                                offset = new Vector2(float.Parse(parameters[6]), float.Parse(parameters[7]))
                            };
                            bgaObjects.Add(Base36.Decode(param2), bga);
                            continue;
                    }
                    if(!int.TryParse(cName2, out verse)) continue;
                    if((channel = GetChannelNumberById(param2)) < 0) continue;
                    param1 = Regex.Replace(param1, "\\s+", string.Empty);
                    verseLength = param1.Length / 2;
                    switch(channel) {
                        case 2: // Time Signature
                            timeSigMapping[verse] = float.Parse(param1);
                            if(!timeSigMapping.ContainsKey(verse + 1))
                                timeSigMapping[verse + 1] = 1; // Reset on next measure
                            continue;
                    }
                    timeLine = GetTimeLine(channel);
                    for(i = 0; i < verseLength; i++) {
                        value = Base36.Decode(param1.Substring(i * 2, 2));
                        if(value > 0) timeLine.AddRawKeyFrame(verse, (float)i / verseLength, value, ifBlockLevel > 0 ? randomGroup : -1, ifBlockLevel > 0 ? randomIndex : -1);
                    }
                }

                if(!parseBody)
                    return;

                // Finalize data
                float bpmValue;
                int bpmValueInt;
                foreach(var bpmChange in GetTimeLine(3).GetRawKeyframes())
                    if(bpmChange.Value > 0) {
                        if(!bpmObjects.TryGetValue(bpmChange.Value, out bpmValue)) {
                            if(!int.TryParse(Base36.Encode(bpmChange.Value), NumberStyles.HexNumber, null, out bpmValueInt))
                                continue;
                            bpmValue = bpmValueInt;
                        }
                        bpmMapping[bpmChange.MeasureBeat] = bpmValue;
                    }
                foreach(var bpmChange in GetTimeLine(8).GetRawKeyframes())
                    if(bpmChange.Value > 0) {
                        if(!bpmObjects.TryGetValue(bpmChange.Value, out bpmValue))
                            continue;
                        bpmMapping[bpmChange.MeasureBeat] = bpmValue;
                    }
                var measures = new HashSet<MeasureBeat>();
                foreach(var tl in timeLines.Values)
                    measures.UnionWith(tl.GetAllMeasureBeats());
                measures.UnionWith(bpmMapping.Keys);
                foreach(var ts in timeSigMapping.Keys)
                    measures.Add(new MeasureBeat(ts, 0));
                var results = ConvertToTimingPoints(timeSigMapping, bpmMapping, measures, 1, bpm);
                foreach(var timeLineKV in timeLines) {
                    timeLineKV.Value.Normalize(results);
                    mainTimingHelper.AddTimelineHandle(timeLineKV.Value, timeLineKV.Key);
                    preTimingHelper.AddTimelineHandle(timeLineKV.Value, timeLineKV.Key);
                    if(timeLineKV.Key < 30 || (timeLineKV.Key > 50 && timeLineKV.Key < 70)) {
                        var kfs = timeLineKV.Value.KeyFrames;
                        if(kfs.Count > 0 && kfs[kfs.Count - 1].TimePosition > duration)
                            duration = kfs[kfs.Count - 1].TimePosition;
                    }
                }
                bpms[TimeSpan.Zero] = bpm;
                foreach(var bpmObj in bpmMapping)
                    bpms[results[bpmObj.Key]] = bpmObj.Value;
                bpmChangeHelper = new TimeSpanHandle<float>(bpms);
                bpmChangeHelper.OnNotified += OnBpmChange;
                var timeSigns = new Dictionary<TimeSpan, float>();
                timeSigns[TimeSpan.Zero] = 1;
                foreach(var tsObj in timeSigMapping)
                    timeSigns[results[new MeasureBeat(tsObj.Key, 0, tsObj.Value)]] = tsObj.Value / 4;
                beatResetHelper = new TimeSpanHandle<float>(timeSigns);
                beatResetHelper.OnNotified += OnBeatReset;
            } catch(ThreadAbortException) {
                Debug.LogWarning("BMS parsing aboarted.");
            } catch(Exception ex) {
                Debug.LogException(ex);
            } finally {
                bmsLoaded = true;
                if(OnBMSLoaded != null)
                    OnBMSLoaded.Invoke();
            }
        }

        /*
            Special mapping for channels
            01 = 1, 02 = 2, ..., 09 = 9,
            0A = 1010, 0B = 1011, ... 0Z = 1035,
            11 = 11, 12 = 12, ... 19 = 19,
            1A = 1110, 1B = 1111, ..., 1Z = 1135,
            ...,
            2A = 1210, 2B = 1211, ..., 2Z = 1235,
            ...,
            Z1 = 351, Z2 = 352, ..., Z9 = 359,
            ZA = 4510, ZB = 4511, ..., ZZ = 4535
            Illegal channel format: -99
        */
        static int GetChannelNumberById(string channel) {
            if(string.IsNullOrEmpty(channel) || channel.Length > 2) return -99;
            int channelRaw = Base36.Decode(channel);
            if(channelRaw < 0) return -99;
            int digit1 = channelRaw % 36, digit2 = channelRaw / 36;
            int result = digit1 > 9 ? (digit2 * 100 + digit1 + 1000) : (digit2 * 10 + digit1);
            int result2;
            return channelAdvancedMapping.TryGetValue(result, out result2) ? result2 : result;
        }

        static void InitChannelMap() {
            var chn = channelAdvancedMapping = new Dictionary<int, int>();
        }

        static Dictionary<MeasureBeat, TimeSpan> ConvertToTimingPoints(
            Dictionary<int, float> timeSignatureObject,
            Dictionary<MeasureBeat, float> bpmObject,
            ICollection<MeasureBeat> calculationSource,
            float defaultTimeSignature, float defaultBPM) {

            var tsList = new List<KeyValuePair<int, float>>(timeSignatureObject);
            var bpmList = new List<KeyValuePair<MeasureBeat, float>>(bpmObject);
            var sources = new List<MeasureBeat>(calculationSource);
            var normalized = new Dictionary<MeasureBeat, MeasureBeat>();
            var timingPoints = new List<TimingPoint>();
            var result = new Dictionary<MeasureBeat, TimeSpan>(sources.Count);

            // Add the bpm change measure points to the mapping list.
            bool hasTimeSign = tsList.Count > 0;
            tsList.Sort(KeyValuePairComparer<int, float>.Default);
            bpmList.Sort(KeyValuePairComparer<MeasureBeat, float>.Default);

            {
                int index = 0;
                float currentTimeSign = defaultTimeSignature;
                MeasureBeat source;
                var newBPMs = new List<KeyValuePair<MeasureBeat, float>>(bpmList.Count);
                foreach(var kv in bpmList) {
                    source = kv.Key;
                    if(hasTimeSign) {
                        while(index < tsList.Count && source.Measure >= tsList[index].Key) index++;
                        if(index > 0) currentTimeSign = tsList[index - 1].Value;
                    }
                    newBPMs.Add(new KeyValuePair<MeasureBeat, float>(source.Align(currentTimeSign), kv.Value));
                }
                bpmList = newBPMs;
            }

            sources.Sort();

            // Normalize the measure beats as they are not yet aligned to time signature.
            {
                int index = 0;
                float currentTimeSign = defaultTimeSignature;
                foreach(var source in sources) {
                    if(hasTimeSign) {
                        while(index < tsList.Count && source.Measure >= tsList[index].Key) index++;
                        if(index > 0) currentTimeSign = tsList[index - 1].Value;
                    }
                    normalized[source] = source.Align(currentTimeSign);
                }
            }

            // Merge time signatures and bpm changes into a single sorted list
            var startTP = new TimingPoint {
                measureBeat = new MeasureBeat(0, 0, 1),
                bpm = defaultBPM,
                timeSignature = defaultTimeSignature,
                timePosition = TimeSpan.Zero,
                hasBPM = true,
                hasTimeSignature = true
            };

            {
                foreach(var timeS in tsList)
                    timingPoints.Add(new TimingPoint {
                        hasTimeSignature = true,
                        timeSignature = timeS.Value,
                        measureBeat = new MeasureBeat(timeS.Key, 0, timeS.Value)
                    });
                foreach(var bpm in bpmList)
                    timingPoints.Add(new TimingPoint {
                        hasBPM = true,
                        bpm = bpm.Value,
                        measureBeat = bpm.Key
                    });
                timingPoints.Sort();
                var previousTP = startTP;
                foreach(var tp in timingPoints) {
                    if(!tp.hasBPM) tp.bpm = previousTP.bpm;
                    if(!tp.hasTimeSignature) tp.timeSignature = previousTP.timeSignature;
                    tp.timePosition = previousTP.ConvertMeasureBeat(tp.measureBeat);
                    tp.hasBPM = true;
                    tp.hasTimeSignature = true;
                    previousTP = tp;
                }
            }

            // Convert all measure beats to time spans.
            {
                int index = 0;
                var tp = startTP;
                foreach(var source in sources) {
                    while(index < timingPoints.Count && normalized[source] >= timingPoints[index].measureBeat) index++;
                    if(index > 0) tp = timingPoints[index - 1];
                    result[source] = tp.ConvertMeasureBeat(normalized[source]);
                }
            }

            return result;
        }

    }
}
