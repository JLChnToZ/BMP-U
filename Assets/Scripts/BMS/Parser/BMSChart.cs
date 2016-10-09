using System;
using System.Linq;
using System.Collections.Generic;

using System.Text.RegularExpressions;
using System.Globalization;

namespace BMS {
    public class BMSChart: Chart {
        static readonly Regex spaceMatcher = new Regex("\\s+", RegexOptions.Compiled);
        static readonly Regex lineMatcher = new Regex("\\r\\n|\\r|\\n", RegexOptions.Compiled);
        string rawBmsContent;
        readonly List<string> bmsContent = new List<string>();
        int lnType;

        public BMSChart(string bmsContent) {
            rawBmsContent = bmsContent;
        }

        public override string RawContent {
            get {
                if(!string.IsNullOrEmpty(rawBmsContent))
                    return rawBmsContent;
                return string.Join("\n", bmsContent.ToArray());
            }
        }

        public int LnType {
            get { return lnType; }
        }

        public override void Parse(ParseType parseType) {
            ResetAllData(parseType);
            if((parseType & ParseType.Header) == ParseType.Header) {
                lnType = 1;
            }
            InitSources();

            Stack<IfBlock> ifStack;
            List<BMSEvent> bmev;
            Random random;
            if((parseType & ParseType.Content) == ParseType.Content) {
                ifStack = new Stack<IfBlock>();
                ifStack.Push(new IfBlock { parsing = true });
                random = new Random();
                bmev = new List<BMSEvent>();
            } else {
                ifStack = null;
                random = null;
                bmev = null;
            }

            foreach(string line in bmsContent) {
                string command, command2, param1, param2;

                // Command parsing
                int colonIndex = line.IndexOf(':', 1), spaceIndex = line.IndexOf(' ', 1);
                int spliiterIndex = colonIndex >= 0 && (spaceIndex > colonIndex || spaceIndex < 0) ? colonIndex : spaceIndex;
                if(spliiterIndex > 0) {
                    command = line.Substring(0, spliiterIndex);
                    if(spliiterIndex + 1 < line.Length)
                        param1 = line.Substring(spliiterIndex + 1).Trim();
                    else
                        param1 = string.Empty;
                } else {
                    command = line;
                    param1 = string.Empty;
                }
                command = command.ToLower();

                // Header part
                if((parseType & ParseType.Header) == ParseType.Header) {
                    if(ParseHeaderLine(command, param1))
                        continue;
                }

                // nnnXX pattern parsing
                if(command.Length > 4) {
                    int i = command.Length - 2;
                    command2 = command.Substring(1, i - 1);
                    param2 = command.Substring(i);
                } else {
                    command2 = command;
                    param2 = string.Empty;
                }

                // Resource part
                if((parseType & ParseType.Resources) == ParseType.Resources) {
                    if(ParseResourecLine(command2, param1, param2))
                        continue;
                }

                // Content part
                if((parseType & ParseType.Content) == ParseType.Content) {
                    if(CheckExecution(ifStack, random, command, param1))
                        continue;
                    if(ParseContentLine(command2, param1, param2, bmev))
                        continue;
                }
            }

            if((parseType & ParseType.Content) == ParseType.Content)
                PostProcessContent(bmev);

            base.Parse(parseType);
        }

        private void InitSources() {
            if(bmsContent.Count > 0 || string.IsNullOrEmpty(rawBmsContent))
                return;
            foreach(string line in lineMatcher.Split(rawBmsContent)) {
                string trimmed = line.Trim();
                if(!string.IsNullOrEmpty(trimmed) && trimmed[0] == '#')
                    bmsContent.Add(trimmed);
            }
            bmsContent.TrimExcess();
            rawBmsContent = null;
        }

        private bool ParseHeaderLine(string command, string strParam) {
            float floatParam;
            int intParam;
            switch(command) {
                case "#title":
                    title = strParam;
                    break;
                case "#artist":
                    artist = strParam;
                    break;
                case "#subtitle":
                    AppendString(ref subTitle, strParam);
                    break;
                case "#subartist":
                    AppendString(ref subArtist, strParam);
                    break;
                case "#comment":
                    AppendString(ref comments, strParam);
                    break;
                case "#bpm":
                    if(float.TryParse(strParam, out floatParam)) {
                        initialBPM = floatParam;
                        minBpm = Math.Min(minBpm, initialBPM);
                    }
                    break;
                case "#genre":
                    genre = strParam;
                    break;
                case "#player":
                    if(int.TryParse(strParam, out intParam))
                        playerCount = intParam;
                    break;
                case "#playlevel":
                    if(int.TryParse(strParam, out intParam))
                        playLevel = intParam;
                    break;
                case "#rank":
                    if(int.TryParse(strParam, out intParam))
                        rank = intParam;
                    break;
                case "#volwav":
                    if(float.TryParse(strParam, out floatParam))
                        volume = floatParam / 100F;
                    break;
                case "#stagefile":
                    AddResource(ResourceType.bmp, -1, strParam);
                    break;
                case "#banner":
                    AddResource(ResourceType.bmp, -2, strParam);
                    break;
                case "#lntype":
                    if(int.TryParse(strParam, out intParam))
                        lnType = intParam;
                    break;
                default: return false;
            }
            return true;
        }

        private bool ParseResourecLine(string command2, string strParam1, string strParam2) {
            switch(command2) {
                case "wav":
                    AddResource(ResourceType.wav, Base36.Decode(strParam2), strParam1);
                    break;
                case "bmp":
                    AddResource(ResourceType.bmp, Base36.Decode(strParam2), strParam1);
                    break;
                case "bga":
                    string[] strParams = strParam1.Split(' ');
                    AddResource(ResourceType.bga, Base36.Decode(strParam2), string.Empty, new object[] {
                        Base36.Decode(strParams[0]), // index
                        float.Parse(strParams[1]), // x1
                        float.Parse(strParams[2]), // x2
                        float.Parse(strParams[3]), // y1
                        float.Parse(strParams[4]), // y2
                        float.Parse(strParams[5]), // dx
                        float.Parse(strParams[6])  // dy
                    });
                    break;
                case "bpm":
                    AddResource(ResourceType.bpm, Base36.Decode(strParam2), string.Empty, float.Parse(strParam1));
                    break;
                case "stop":
                    AddResource(ResourceType.stop, Base36.Decode(strParam2), string.Empty, float.Parse(strParam1));
                    break;
                default: return false;
            }
            return true;
        }

        private bool ParseContentLine(string command2, string strParam1, string strParam2, List<BMSEvent> bmev) {
            int verse;
            if(!int.TryParse(command2, out verse)) return false;
            int channel = GetChannelNumberById(strParam2);
            if(channel < 0) return false;
            strParam1 = spaceMatcher.Replace(strParam1, string.Empty);
            int length = strParam1.Length / 2;
            BMSEventType evType;
            switch(channel) {
                case 1: evType = BMSEventType.WAV; break;
                case 2:
                    AddEvent(new BMSEvent {
                        measure = verse,
                        beat = 0,
                        data2 = BitConverter.DoubleToInt64Bits(double.Parse(strParam1)),
                        type = BMSEventType.BeatReset
                    });
                    return true;
                case 3: evType = BMSEventType.BPM; break;
                case 4:
                case 6:
                case 7: evType = BMSEventType.BMP; break;
                case 8: evType = BMSEventType.BPM; break;
                case 9: evType = BMSEventType.STOP; break;
                default:
                    if(channel > 10 && channel < 30)
                        evType = BMSEventType.Note;
                    else if(channel > 50 && channel < 70)
                        evType = BMSEventType.LongNoteStart;
                    else
                        evType = BMSEventType.Unknown;
                    break;
            }
            for(int i = 0; i < length; i++) {
                int value = Base36.Decode(strParam1.Substring(i * 2, 2));
                if(value > 0) {
                    AddEvent(new BMSEvent {
                        measure = verse,
                        beat = (float)i / length,
                        type = evType,
                        data1 = channel,
                        data2 = value
                    });
                }
            }
            return true;
        }

        private bool CheckExecution(Stack<IfBlock> stack, Random random, string command, string strParam) {
            IfBlock current, parent;
            int intParam;
            switch(command) {
                case "#if":
                    current = new IfBlock();
                    parent = stack.Pop();
                    if(parent.parsing) {
                        if(int.TryParse(strParam, out intParam) && intParam == parent.rand) {
                            current.parsing = true;
                            parent.parsed = true;
                        } else {
                            parent.parsed = false;
                        }
                    }
                    stack.Push(parent);
                    stack.Push(current);
                    break;
                case "#elseif":
                    current = stack.Pop();
                    parent = stack.Pop();
                    if(parent.parsing) {
                        if(!parent.parsed && int.TryParse(strParam, out intParam) && intParam == parent.rand) {
                            current.parsing = true;
                            parent.parsed = true;
                        } else {
                            current.parsing = false;
                        }
                    }
                    stack.Push(parent);
                    stack.Push(current);
                    break;
                case "#else":
                    current = stack.Pop();
                    parent = stack.Pop();
                    if(parent.parsing) {
                        if(!parent.parsed) {
                            current.parsing = true;
                            parent.parsed = true;
                        } else {
                            current.parsing = false;
                        }
                    }
                    stack.Push(parent);
                    stack.Push(current);
                    break;
                case "#endif":
                    stack.Pop();
                    break;
                case "#random":
                case "#setrandom":
                    current = stack.Pop();
                    if(current.parsing && int.TryParse(strParam, out intParam))
                        current.rand = random.Next(intParam) + 1;
                    stack.Push(current);
                    break;
                default:
                    return !stack.Peek().parsing;
            }
            return true;
        }

        private void PostProcessContent(List<BMSEvent> bmev) {
            // Insert beat reset after 1 measure of time signature change event according to BMS specifications.
            BMSEvent[] beatResetEvents = bmev.Where(ev => ev.type == BMSEventType.BeatReset).ToArray();
            for(int i = 0, l = beatResetEvents.Length; i < l; i++) {
                BMSEvent currentEv = beatResetEvents[i];
                int meas = currentEv.measure;
                if(i == l - 1 || (beatResetEvents[i + 1].measure - meas > 1 &&
                    BitConverter.Int64BitsToDouble(currentEv.data2) != 1))
                    AddEvent(new BMSEvent {
                        measure = meas + 1,
                        beat = 0,
                        data2 = BitConverter.DoubleToInt64Bits(1),
                        type = BMSEventType.BeatReset
                    });
            }
            
            Dictionary<int, BMSEvent> lnMarker = new Dictionary<int, BMSEvent>();
            double bpm = initialBPM, beatOffset = 0, beatPerMeas = 1;
            TimeSpan referenceTimePoint = TimeSpan.Zero;

            TimeSpan stopTimePoint = TimeSpan.Zero;
            float stopMeasBeat = float.MinValue;

            AddEvent(new BMSEvent {
                measure = 0,
                beat = 0,
                type = BMSEventType.BPM,
                data2 = BitConverter.DoubleToInt64Bits(bpm)
            });

            foreach(BMSEvent ev in bmev) {
                BMSEvent converted = new BMSEvent();
                converted.measure = ev.measure;
                converted.beat = (float)(ev.beat * beatPerMeas);
                if(ev.measure + ev.beat == stopMeasBeat)
                    converted.time = stopTimePoint;
                else
                    converted.time = referenceTimePoint + MeasureBeatToTimeSpan(ev.measure + ev.beat - beatOffset, beatPerMeas, bpm);
                switch(ev.type) {
                    case BMSEventType.BPM:
                        BMSResourceData bpmData;
                        int bpmInt;
                        double newBpm;
                        if(ev.data1 == 8 && TryGetResourceData(ResourceType.bpm, ev.data2, out bpmData)) // Extended BPM
                            newBpm = Convert.ToDouble(bpmData.additionalData);
                        else if(ev.data1 == 3 && int.TryParse(Base36.Encode((int)ev.data2), NumberStyles.HexNumber, null, out bpmInt)) // BPM
                            newBpm = bpmInt;
                        else
                            continue;
                        if(newBpm == bpm)
                            continue;
                        converted.type = BMSEventType.BPM;
                        converted.data2 = BitConverter.DoubleToInt64Bits(newBpm);
                        referenceTimePoint = converted.time;
                        beatOffset = ev.measure + ev.beat;
                        bpm = newBpm;
                        minBpm = Math.Min(minBpm, (float)newBpm);
                        break;
                    case BMSEventType.BeatReset:
                        double newBeatPerMeas = BitConverter.Int64BitsToDouble(ev.data2);
                        if(newBeatPerMeas == beatPerMeas)
                            continue;
                        converted.type = BMSEventType.BeatReset;
                        converted.data2 = ev.data2;
                        beatOffset = ev.measure;
                        beatPerMeas = newBeatPerMeas;
                        referenceTimePoint = converted.time;
                        break;
                    case BMSEventType.STOP:
                        BMSResourceData stopData;
                        if(!TryGetResourceData(ResourceType.stop, ev.data2, out stopData))
                            continue;
                        converted.type = BMSEventType.STOP;
                        stopTimePoint = converted.time;
                        stopMeasBeat = ev.measure + ev.beat;
                        double stopBeats = Convert.ToDouble(stopData.additionalData);
                        converted.data2 = BitConverter.DoubleToInt64Bits(stopBeats);
                        referenceTimePoint += MeasureBeatToTimeSpan(stopBeats, beatPerMeas, bpm);
                        break;
                    case BMSEventType.BMP:
                        converted.type = BMSEventType.BMP;
                        switch(ev.data1) {
                            case 4: converted.data1 = 0; break;
                            case 6: converted.data1 = -1; break;
                            case 7: converted.data1 = 1; break;
                            default: continue;
                        }
                        converted.data2 = ev.data2;
                        break;
                    case BMSEventType.LongNoteStart:
                        converted.data1 = ev.data1 - 40;
                        converted.data2 = ev.data2;
                        converted.sliceStart = TimeSpan.Zero;
                        converted.sliceEnd = TimeSpan.MaxValue;
                        BMSEvent lnStart;
                        if(lnMarker.TryGetValue(ev.data1, out lnStart)) {
                            converted.type = BMSEventType.LongNoteEnd;
                            int index = FindEventIndex(lnStart);
                            lnStart.time2 = converted.time;
                            ReplaceEvent(index, lnStart);
                            lnMarker.Remove(ev.data1);
                        } else {
                            converted.type = BMSEventType.LongNoteStart;
                            lnMarker[ev.data1] = converted;
                        }
                        break;
                    case BMSEventType.Unknown:
                        continue;
                    default:
                        if((ev.data1 >= 30 && ev.data1 <= 50) || ev.data1 >= 70)
                            continue;
                        converted.type = ev.type;
                        converted.data1 = ev.data1;
                        converted.data2 = ev.data2;
                        converted.sliceStart = TimeSpan.Zero;
                        converted.sliceEnd = TimeSpan.MaxValue;
                        break;
                }
                AddEvent(converted);
            }
        }

        private static void AppendString(ref string original, string append) {
            if(string.IsNullOrEmpty(original)) {
                original = append;
            } else {
                original = string.Concat(original, "\n", append);
            }
        }

        private static TimeSpan MeasureBeatToTimeSpan(double beat, double beatPerMeasure, double beatPerMinute) {
            return new TimeSpan((long)Math.Round(beat * beatPerMeasure * 4 / beatPerMinute * TimeSpan.TicksPerMinute));
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
            return result;
        }

        private struct IfBlock {
            public bool parsing, parsed;
            public int rand;
        }

        private struct LnStart {
            public BMSEvent note;
            public int index;
        }
    }
}
