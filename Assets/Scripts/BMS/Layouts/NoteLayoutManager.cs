using System;
using System.Collections.Generic;
using UnityEngine;
using BananaBeats.Visualization;
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

        public static void SetLayout(BMSKeyLayout layout) {
            if(startPos == null)
                startPos = new Vector3[20];
            if(endPos == null)
                endPos = new Vector3[20];

            var preset = GetPreset(layout);

            NoteLaneManager.Clear();

            for(int i = 0; i < preset.Length; i++) {
                var line = GetLine(i, preset[i], preset.Length);
                startPos[preset[i]] = line.start;
                endPos[preset[i]] = line.end;
                NoteLaneManager.CreateGauge(line.end, Vector3.LerpUnclamped(line.end, line.start, 10));
            }

            NoteDisplayManager.RegisterPosition(startPos, endPos);

            // Draw end line
            for(int i = 0; i < preset.Length - 1; i++)
                NoteLaneManager.CreateLane(endPos[preset[i]], endPos[preset[i + 1]]);
        }

        private static int[] GetPreset(BMSKeyLayout layout) {
            if(layoutPresets.TryGetValue(layout, out var preset))
                return preset;
            if((layout & ~BMSKeyLayout.Single5Key) == BMSKeyLayout.None)
                return layoutPresets.TryGetValue(BMSKeyLayout.Single5Key, out preset) ? preset :
                    new[] { 6, 1, 2, 3, 4, 5, };
            else if((layout & ~BMSKeyLayout.Single7Key) == BMSKeyLayout.None)
                return layoutPresets.TryGetValue(BMSKeyLayout.Single7Key, out preset) ? preset :
                    new[] { 6, 1, 2, 3, 4, 5, 8, 9, };
            else if((layout & ~BMSKeyLayout.Single9Key) == BMSKeyLayout.None)
                return layoutPresets.TryGetValue(BMSKeyLayout.Single9Key, out preset) ? preset :
                    new[] { 1, 2, 3, 4, 5, 12, 13, 14, 15, };
            else if((layout & ~BMSKeyLayout.Single9KeyAlt) == BMSKeyLayout.None)
                return layoutPresets.TryGetValue(BMSKeyLayout.Single9KeyAlt, out preset) ? preset :
                    new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, };
            else if((layout & ~BMSKeyLayout.Duel10Key) == BMSKeyLayout.None)
                return layoutPresets.TryGetValue(BMSKeyLayout.Duel10Key, out preset) ? preset :
                    new[] { 6, 1, 2, 3, 4, 5, 11, 12, 13, 14, 15, 16, };
            else if((layout & ~BMSKeyLayout.Duel14Key) == BMSKeyLayout.None)
                return layoutPresets.TryGetValue(BMSKeyLayout.Duel14Key, out preset) ? preset :
                    new[] { 6, 1, 2, 3, 4, 5, 8, 9, 11, 12, 13, 14, 15, 18, 19, 16, };
            return new[] {
                0,  1,  2,  3,  4,  5,  6,  7,  8,  9,
                10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
            };
        }

        public static void SetLayoutPresets(IEnumerable<KeyValuePair<BMSKeyLayout, int[]>> layouts) {
            foreach(var kv in layouts)
                if(kv.Value != null)
                    layoutPresets[kv.Key] = kv.Value;
                else
                    layoutPresets.Remove(kv.Key);
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
