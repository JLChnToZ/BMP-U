using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using BMSManager = BMS.BMSManager;

public class RecordsManager {
    public const string DefaultPlayerName = "Player";

    public struct Record {
        public readonly string playerName;
        public readonly int combos;
        public readonly int channelConfig;
        public readonly int score;
        public readonly DateTime timeStamp;
        // public readonly string recordId;
        public readonly int playCount;

        internal Record(string playerName, int channelConfig, int combos, int score, DateTime timeStamp, int playCount) {
            this.playerName = playerName;
            this.channelConfig = channelConfig;
            this.combos = combos;
            this.score = score;
            this.timeStamp = timeStamp;
            // this.recordId = recordId;
            this.playCount = playCount;
        }

        public bool HasChannel(int channel) {
            if(channel >= 50) channel -= 40;
            return unchecked(channelConfig & (1 << channel)) != 0;
        }
    }

    static readonly string sqlitePath = "records.dat";
    static RecordsManager instance;
    public static RecordsManager Instance {
        get {
            if(instance == null)
                instance = new RecordsManager();
            return instance;
        }
    }

    Database database;
    HashAlgorithm hashAlgorithm;

    public HashAlgorithm HashAlgorithm {
        get { return hashAlgorithm; }
    }

    private RecordsManager() {
        hashAlgorithm = SHA512.Create();
        InitTable();
#if UNITY_EDITOR
        Action<PlayModeStateChange> playModeChangeHandle = null;
        playModeChangeHandle = (state) => {
            if(state == PlayModeStateChange.ExitingPlayMode) {
                CloseDatabase();
                EditorApplication.playModeStateChanged -= playModeChangeHandle;
            }
        };
        EditorApplication.playModeStateChanged += playModeChangeHandle;
#endif
    }

    void InitTable() {
        const string commandText = "CREATE TABLE IF NOT EXISTS `records`(" +
            "`id` INTEGER PRIMARY KEY AUTOINCREMENT UNIQUE NOT NULL," +
            "`hash` TEXT UNIQUE NOT NULL," +
            "`channel_config` INTEGER DEFAULT 0," +
            "`player_name` TEXT NOT NULL DEFAULT 'Player'," +
            "`combos` INTEGER DEFAULT 0," +
            "`score` INTEGER DEFAULT 0," +
            "`time` INTEGER DEFAULT (strftime('%s', 'now'))," +
            // "`record_id` TEXT," +
            "`play_count` INTEGER DEFAULT 0);" +
            "CREATE INDEX IF NOT EXISTS `records_idx` ON `records` (" +
            "`hash`," +
            "`player_name`" +
            ");";
        OpenDatabase();
        database.RunSql(commandText);
    }

    public static int GetAdoptedChannelHash(ICollection<int> adoptedChannels) {
        int result = 0;
        unchecked {
            foreach(int channel in adoptedChannels) {
                if(channel >= 50) result |= 1 << (channel - 40);
                else result |= 1 << channel;
            }
        }
        return result;
    }

    public void CreateRecord(BMSManager bmsManager, string playerName = DefaultPlayerName) {
        const string queryCommandText = "SELECT `play_count`, `combos`, `score` FROM `records` WHERE `hash` = ? AND `player_name` = ? AND `channel_config` = ?;";
        const string updateCommandText = "UPDATE `records` SET `combos` = ?, `score` = ?, `play_count` = ?, `time` = (strftime('%s', 'now')) WHERE " +
            "`hash` = ? AND `player_name` = ? AND `channel_config` = ?;";
        const string insertCommandText = "INSERT INTO `records`(`hash`, `player_name`, `channel_config`, `combos`, `score`, `play_count`)" +
            "VALUES (?, ?, ?, ?, ?, ?);";
        string bmsHash = bmsManager.GetHash(SongInfoLoader.CurrentEncoding, hashAlgorithm);
        // string timeHash = DateTime.UtcNow.Ticks.ToBaseString(32);
        int channelConfig = GetAdoptedChannelHash(bmsManager.GetAllAdoptedChannels());
        int playCount = 0;
        int maxCombos = bmsManager.MaxCombos;
        int maxScore = bmsManager.Score;

        OpenDatabase();
        foreach(var record in database.QuerySql(queryCommandText, bmsHash, playerName, channelConfig)) {
            playCount += record.GetValueAsInt32(0);
            maxCombos = Math.Max(maxCombos, record.GetValueAsInt32(1));
            maxScore = Math.Max(maxScore, record.GetValueAsInt32(2));
        }

        if(playCount > 0) {
            database.RunSql(updateCommandText,
                maxCombos, maxScore, playCount + 1,
                bmsHash, playerName, channelConfig
            );
        } else {
            database.RunSql(insertCommandText,
                bmsHash, playerName, channelConfig,
                maxCombos, maxScore, playCount + 1
            );
        }

    }

    /* public Record[] GetRecords(string bmsHash) {
        const string commandText = "SELECT `player_name`, `channel_config`, `combos`, `score`, `time`, `record_id` FROM `records` WHERE `hash` = ? ORDER BY `score` DESC;";
        OpenDatabase();
        return database.QuerySql(commandText, bmsHash).Select(record => new Record(
            record.GetString(0),
            record.GetInt32(1),
            record.GetInt32(2),
            record.GetInt32(3),
            record.GetDateTime(4),
            record.GetString(5)
        )).ToArray();
    } */

    public Record? GetRecord(string bmsHash, int channelConfig, string playerName = DefaultPlayerName) {
        const string commandText = "SELECT `player_name`, `channel_config`, `combos`, `score`, `time`, `play_count` FROM `records` " +
            "WHERE `hash` = ? AND `player_name` = ?;";// AND `channel_config` = ?;";
        OpenDatabase();
        foreach(var result in database.QuerySql(commandText, bmsHash, playerName/*, channelConfig*/)) {
            return new Record(
                result.GetString(0),
                result.GetValueAsInt32(1),
                result.GetValueAsInt32(2),
                result.GetValueAsInt32(3),
                DateTimeHelper.FromUnixTime(result.GetValueAsInt64(4)),
                result.GetValueAsInt32(5)
            );
        }
        return null;
    }

    public void OpenDatabase() {
        if(database == null)
            database = new Database(SongInfoLoader.GetAbsolutePath("../" + sqlitePath));
    }

    public void CloseDatabase() {
        database.Dispose();
        database = null;
    }

    ~RecordsManager() {
        CloseDatabase();
    }
}