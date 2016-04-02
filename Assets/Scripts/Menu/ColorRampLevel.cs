using UnityEngine;
using System;

public class ColorRampLevel : ScriptableObject {

    [Serializable]
    public struct LevelMapping:IComparable {
        public float level;
        public Color color;

        public int CompareTo(object obj) {
            if(!(obj is LevelMapping)) return 0;
            return level.CompareTo(((LevelMapping)obj).level);
        }
    }

    [SerializeField]
    LevelMapping[] levelMapping;
    bool isTidy = false;

    public Color GetColor(float level) {
        if(!isTidy) {
            Array.Sort(levelMapping);
            isTidy = true;
        }
        bool hasLowerBound = false;
        LevelMapping lvLow = new LevelMapping { level = 0, color = Color.black }, lv;
        for(int i = 0, l = levelMapping.Length; i < l; i++) {
            lv = levelMapping[i];
            if(lv.level < level) {
                if(i == l - 1)
                    return lv.color;
                lvLow = lv;
                hasLowerBound = true;
            } else if(lv.level > level) {
                if(!hasLowerBound)
                    return lv.color;
                return Color.Lerp(lvLow.color, lv.color, Mathf.InverseLerp(lvLow.level, lv.level, level));
            } else
                return lv.color;
        }
        return Color.black;
    }
}
