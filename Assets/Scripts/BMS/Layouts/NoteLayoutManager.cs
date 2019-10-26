using System;
using System.Collections.Generic;
using UnityEngine;
using BananaBeats.Visualization;
using BananaBeats.PlayerData;
using BMS;

namespace BananaBeats.Layouts {
    public static class NoteLayoutManager {
        public struct Line {
            public Vector3 start;
            public Vector3 end;
        }

        public delegate Line LayoutFunc(int index, int channel, int count);

        public static readonly Dictionary<BMSKeyLayout, int[]> layoutPresets = new Dictionary<BMSKeyLayout, int[]>();

        public static LayoutFunc layoutFunc;

        private static Vector3[] startPos, endPos;

        public static void SwitchLayout(BMSKeyLayout layout) {
            if(startPos == null)
                startPos = new Vector3[20];
            if(endPos == null)
                endPos = new Vector3[20];

            var preset = GetPreset(layout);

            NoteLaneManager.Clear();

            for(int i = 0; i < preset.Length; i++) {
                int val = preset[i] - 10;
                var line = GetLine(i, val, preset.Length);
                startPos[val] = line.start;
                endPos[val] = line.end;
                NoteLaneManager.CreateGauge(line.end, Vector3.LerpUnclamped(line.end, line.start, 10));
            }

            NoteDisplayManager.RegisterPosition(startPos, endPos);

            // Draw end line
            for(int i = 0; i < preset.Length - 1; i++)
                NoteLaneManager.CreateLane(endPos[preset[i]], endPos[preset[i + 1]]);
        }

        public static BMSKeyLayout GetFallbackLayout(this BMSKeyLayout layout) {
            if((layout & ~BMSKeyLayout.Single5Key) == BMSKeyLayout.None)
                return BMSKeyLayout.Single5Key;
            if((layout & ~BMSKeyLayout.Single7Key) == BMSKeyLayout.None)
                return BMSKeyLayout.Single7Key;
            if((layout & ~BMSKeyLayout.Single9Key) == BMSKeyLayout.None)
                return BMSKeyLayout.Single9Key;
            if((layout & ~BMSKeyLayout.Single9KeyAlt) == BMSKeyLayout.None)
                return BMSKeyLayout.Single9KeyAlt;
            if((layout & ~BMSKeyLayout.Duel10Key) == BMSKeyLayout.None)
                return BMSKeyLayout.Duel10Key;
            if((layout & ~BMSKeyLayout.Duel14Key) == BMSKeyLayout.None)
                return BMSKeyLayout.Duel14Key;
            return layout;
        }

        public static BMSKeyLayout ChannelToLayout(int channel) {
            if(channel < 10)
                return BMSKeyLayout.None;
            switch((channel - 10) % 20) {
                case 1: return BMSKeyLayout.P11;
                case 2: return BMSKeyLayout.P12;
                case 3: return BMSKeyLayout.P13;
                case 4: return BMSKeyLayout.P14;
                case 5: return BMSKeyLayout.P15;
                case 6: return BMSKeyLayout.P16;
                case 7: return BMSKeyLayout.P17;
                case 8: return BMSKeyLayout.P18;
                case 9: return BMSKeyLayout.P19;
                case 11: return BMSKeyLayout.P21;
                case 12: return BMSKeyLayout.P22;
                case 13: return BMSKeyLayout.P23;
                case 14: return BMSKeyLayout.P24;
                case 15: return BMSKeyLayout.P25;
                case 16: return BMSKeyLayout.P26;
                case 17: return BMSKeyLayout.P27;
                case 18: return BMSKeyLayout.P28;
                case 19: return BMSKeyLayout.P29;
                default: return BMSKeyLayout.None;
            }
        }

        public static int GetChannel(this BMSKeyLayout layout) {
            switch(layout) {
                case BMSKeyLayout.P11: return 11;
                case BMSKeyLayout.P12: return 12;
                case BMSKeyLayout.P13: return 13;
                case BMSKeyLayout.P14: return 14;
                case BMSKeyLayout.P15: return 15;
                case BMSKeyLayout.P16: return 16;
                case BMSKeyLayout.P17: return 17;
                case BMSKeyLayout.P18: return 18;
                case BMSKeyLayout.P19: return 19;
                case BMSKeyLayout.P21: return 21;
                case BMSKeyLayout.P22: return 22;
                case BMSKeyLayout.P23: return 23;
                case BMSKeyLayout.P24: return 24;
                case BMSKeyLayout.P25: return 25;
                case BMSKeyLayout.P26: return 26;
                case BMSKeyLayout.P27: return 27;
                case BMSKeyLayout.P28: return 28;
                case BMSKeyLayout.P29: return 29;
                default: return 0;
            }
        }

        public static bool HasChannel(this BMSKeyLayout layout, int channelId) {
            if(channelId > 50) channelId -= 40;
            switch(channelId) {
                case 11: return (layout & BMSKeyLayout.P11) == BMSKeyLayout.P11;
                case 12: return (layout & BMSKeyLayout.P12) == BMSKeyLayout.P12;
                case 13: return (layout & BMSKeyLayout.P13) == BMSKeyLayout.P13;
                case 14: return (layout & BMSKeyLayout.P14) == BMSKeyLayout.P14;
                case 15: return (layout & BMSKeyLayout.P15) == BMSKeyLayout.P15;
                case 16: return (layout & BMSKeyLayout.P16) == BMSKeyLayout.P16;
                case 17: return (layout & BMSKeyLayout.P17) == BMSKeyLayout.P17;
                case 18: return (layout & BMSKeyLayout.P18) == BMSKeyLayout.P18;
                case 19: return (layout & BMSKeyLayout.P19) == BMSKeyLayout.P19;
                case 21: return (layout & BMSKeyLayout.P21) == BMSKeyLayout.P21;
                case 22: return (layout & BMSKeyLayout.P22) == BMSKeyLayout.P22;
                case 23: return (layout & BMSKeyLayout.P23) == BMSKeyLayout.P23;
                case 24: return (layout & BMSKeyLayout.P24) == BMSKeyLayout.P24;
                case 25: return (layout & BMSKeyLayout.P25) == BMSKeyLayout.P25;
                case 26: return (layout & BMSKeyLayout.P26) == BMSKeyLayout.P26;
                case 27: return (layout & BMSKeyLayout.P27) == BMSKeyLayout.P27;
                case 28: return (layout & BMSKeyLayout.P28) == BMSKeyLayout.P28;
                case 29: return (layout & BMSKeyLayout.P29) == BMSKeyLayout.P29;
                default: return false;
            }
        }

        private static int[] GetPreset(BMSKeyLayout layout) {
            if(layoutPresets.TryGetValue(layout, out var preset))
                return preset;
            layout = layout.GetFallbackLayout();
            if(layoutPresets.TryGetValue(layout, out preset))
                return preset;
            return GetDefaultPreset(layout);
        }

        public static int[] GetDefaultPreset(BMSKeyLayout layout) {
            switch(layout) {
                case BMSKeyLayout.Single5Key:
                    return new[] { 16, 11, 12, 13, 14, 15, };
                case BMSKeyLayout.Single7Key:
                    return new[] { 16, 11, 12, 13, 14, 15, 18, 19, };
                case BMSKeyLayout.Single9Key:
                    return new[] { 11, 12, 13, 14, 15, 22, 23, 24, 25, };
                case BMSKeyLayout.Single9KeyAlt:
                    return new[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, };
                case BMSKeyLayout.Duel10Key:
                    return new[] { 16, 11, 12, 13, 14, 15, 21, 22, 23, 24, 25, 26, };
                case BMSKeyLayout.Duel14Key:
                    return new[] { 16, 11, 12, 13, 14, 15, 18, 19, 21, 22, 23, 24, 25, 28, 29, 26, };
                default:
                    return new[] {
                        11, 12, 13, 14, 15, 16, 17, 18, 19,
                        21, 22, 23, 24, 25, 26, 27, 28, 29,
                    };
            }
        }

        public static void SetLayoutPresets(IEnumerable<KeyValuePair<BMSKeyLayout, int[]>> layouts) {
            foreach(var kv in layouts)
                if(kv.Value != null)
                    layoutPresets[kv.Key] = kv.Value;
                else
                    layoutPresets.Remove(kv.Key);
        }

        public static void Load(PlayerDataManager playerDataManager) {
            layoutPresets.Clear();
            foreach(var layout in playerDataManager.GetLayouts())
                layoutPresets[layout.LayoutType] = layout.LayoutData;
        }

        public static void Save(PlayerDataManager playerDataManager) {
            foreach(var layout in layoutPresets)
                playerDataManager.SaveLayout(layout.Key, layout.Value);
        }

        private static Line GetLine(int index, int channel, int count) {
            if(layoutFunc != null)
                return layoutFunc(index, channel, count);
            float x = (index - (count - 1) / 2F) * 25F / count;
            return new Line {
                start = new Vector3(x, 0, 100),
                end = new Vector3(x, 0, 0),
            };
        }
    }
}
