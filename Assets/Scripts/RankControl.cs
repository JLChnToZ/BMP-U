using UnityEngine;

using System;
using System.Collections.Generic;


public class RankControl : ScriptableObject {
    [Serializable]
    struct RankMapping {
        public float rangeStart;
        public string rankName;
        public Color rankColor;
    }

    [SerializeField]
    RankMapping[] ranks;
    [NonSerialized]
    List<RankMapping> rankSorted;

    void InitRankList() {
        if(rankSorted != null)
            return;
        rankSorted = new List<RankMapping>(ranks);
        rankSorted.Sort((lhs, rhs) => lhs.rangeStart.CompareTo(rhs.rangeStart));
    }

    public void GetRank(float score, out string rankName, out Color rankColor) {
        InitRankList();
        RankMapping rankMap = new RankMapping {
            rangeStart = float.MinValue,
            rankName = string.Empty,
            rankColor = Color.white
        };
        foreach(var item in rankSorted) {
            if(score < item.rangeStart)
                break;
            rankMap = item;
        }
        rankName = rankMap.rankName;
        rankColor = rankMap.rankColor;
    }

}
