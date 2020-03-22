using System;
using System.Collections.Generic;
using UniRx.Async;
using SQLite4Unity3d;
using BMS;
using UniRx;
using UnityEngine;

namespace BananaBeats.GameData {
    internal class GameDataHandler: ScriptableObject, IDisposable {
        public SQLiteConnection Connection { get; private set; }
        private bool descroyed;

        private void Awake() {
            Connection = new SQLiteConnection($"{Application.dataPath}/bananabeats.dat");
            Connection.CreateTable<ChartGroup>();
            Connection.CreateTable<ChartTag>();
            Connection.CreateTable<ChartResourceMap>();
            Connection.CreateTable<ChartMeta>();
            Connection.CreateTable<PlayerResult>();
        }

        private void OnDestroy() {
            if(!descroyed) {
                descroyed = true;
                Connection.Close();
            }
        }

        public void Dispose() => Destroy(this);
    }

    public static class GameDataHub {
        private static GameDataHandler connectionHandler;

        private static SQLiteConnection Connection {
            get {
                if(connectionHandler == null)
                    connectionHandler = ScriptableObject.CreateInstance<GameDataHandler>();
                return connectionHandler.Connection;
            }
        }

        public static ChartGroup CreateChartId() {
            var chartGroup = new ChartGroup { };
            Connection.Insert(chartGroup);
            return chartGroup;
        }


        public static void SaveScore(string resourceId, string playerId, int score, int maxCombos, BMSKeyLayout layout) =>
            Connection.Insert(new PlayerResult {
                ChartResourceId = resourceId,
                PlayerId = playerId,
                Score = score,
                MaxCombos = maxCombos,
                Layout = (int)layout,
                CreateTime = DateTime.Now,
            });

        public static List<PlayerResult> GetScores(string resourceId) =>
            Connection.Query<PlayerResult>(
                "SELECT * FROM PLAYER_RESULT WHERE RES_ID = ? ORDER BY SCORE DESC, MAX_COMBOS DESC",
                resourceId);

        public static List<ChartMeta> GetChartByGroup(int chartId) =>
            Connection.Query<ChartMeta>(
                "SELECT * FROM CHART_META WHERE CHART_ID = ? ORDER BY DISPLAY_ORDER ASC",
                chartId);

        public static IEnumerable<int> SearchTags(params string[] tags) {
            if(tags == null || tags.Length == 0)
                yield break;
            var factory = string.Join(",", Array.ConvertAll(tags, _ => "?"));
            foreach(var tag in Connection.DeferredQuery<ChartTag>(
                $"SELECT DISTINCT CHART_ID FROM CHART_TAG WHERE TAG IN ({factory})",
                tags))
                yield return tag.ChartId;
        }
    }

    [Table("CHART_GROUP")]
    public class ChartGroup {
        [Column("CHART_ID"), PrimaryKey, AutoIncrement, Unique, NotNull]
        public int ChartId { get; set; }
    }

    [Table("CHART_TAG")]
    public class ChartTag {
        [Column("CHART_ID"), PrimaryKey, NotNull, Indexed(Name = "CHART_TAG_INV_INDEX")]
        public int ChartId { get; set; }

        [Column("TAG"), PrimaryKey, NotNull, Indexed(Name = "CHART_TAG_INDEX")]
        public string Tag { get; set; }
    }

    [Table("CHART_RESMAP")]
    public class ChartResourceMap {
        [Column("CHART_ID"), PrimaryKey, NotNull, Indexed(Name = "CHART_RESMAP_INDEX")]
        public int ChartId { get; set; }

        [Column("RES_NAME"), PrimaryKey, NotNull]
        public string ResourceName { get; set; }

        [Column("RES_ID"), PrimaryKey, NotNull]
        public string ResourceId { get; set; }
    }

	[Table("CHART_META")]
    public class ChartMeta {
        [Column("CHART_ID"), NotNull, Indexed(Name = "CHART_ID_INDEX")]
        public int ChartId { get; set; }

        [Column("RES_ID"), PrimaryKey, NotNull]
        public string ChartResourceId { get; set; }

        [Column("TITLE"), NotNull]
        public string Title { get; set; }

        [Column("SUB_TITLE"), NotNull]
        public string SubTitle { get; set; }

        [Column("ARTIST"), NotNull]
        public string Artist { get; set; }

        [Column("SUB_ARTIST"), NotNull]
        public string SubArtist { get; set; }

        [Column("COMMENTS"), NotNull]
        public string Comments { get; set; }

        [Column("PLAYER_COUNT"), NotNull]
        public int PlayerCount { get; set; }

        [Column("BPM"), NotNull]
        public float BPM { get; set; }

        [Column("MAX_COMBOS"), NotNull]
        public int MaxCombos { get; set; }

        [Column("DISPLAY_ORDER"), NotNull, Indexed(Name = "CHART_ID_INDEX", Order = -1)]
        public int Order { get; set; }

        [Column("LEVEL"), NotNull]
        public float Level { get; set; }
    }

    [Table("PLAYER_RESULT")]
    public class PlayerResult {

        [Column("RES_ID"), PrimaryKey, NotNull, Indexed(Name = "PLAYER_RESULT_INDEX")]
        public string ChartResourceId { get; set; }

        [Column("PLAYER_ID"), NotNull]
        public string PlayerId { get; set; }

        [Column("CREATE_TIME"), NotNull]
        public DateTime CreateTime { get; set; }

        [Column("SCORE"), NotNull]
        public int Score { get; set; }

        [Column("MAX_COMBOS"), NotNull]
        public int MaxCombos { get; set; }

        [Column("LAYOUT"), NotNull]
        public int Layout { get; set; }
    }
}
