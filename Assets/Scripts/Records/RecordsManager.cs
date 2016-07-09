using System;
using System.Data;
using System.Collections.Generic;
using System.Security.Cryptography;
using Mono.Data.Sqlite;

using BMSManager = BMS.BMSManager;

public class RecordsManager {
    public struct Record {
        public readonly string playerName;
        public readonly int combos;
        public readonly int channelConfig;
        public readonly int score;
        public readonly DateTime timeStamp;
        public readonly string recordId;

        internal Record(string playerName, int channelConfig, int combos, int score, DateTime timeStamp, string recordId) {
            this.playerName = playerName;
            this.channelConfig = channelConfig;
            this.combos = combos;
            this.score = score;
            this.timeStamp = timeStamp;
            this.recordId = recordId;
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

    SqliteConnection connection;
    HashAlgorithm hashAlgorithm;

    public HashAlgorithm HashAlgorithm {
        get { return hashAlgorithm; }
    }

    private RecordsManager() {
        hashAlgorithm = SHA512.Create();
        connection = new SqliteConnection(string.Format("URI=file:{0}", SongInfoLoader.GetAbsolutePath("../records.dat")));
        connection.Open();
        InitTable();
    }

    void InitTable() {
        const string commandText = "CREATE TABLE IF NOT EXISTS `records`(" +
            "`id` INTEGER PRIMARY KEY AUTOINCREMENT UNIQUE NOT NULL," +
            "`hash` VARCHAR UNIQUE NOT NULL," +
            "`channel_config` INTEGER DEFAULT 0," +
            "`player_name` VARCHAR NOT NULL DEFAULT 'Player'," +
            "`combos` INTEGER DEFAULT 0," +
            "`score` INTEGER DEFAULT 0," +
            "`time` INTEGER DEFAULT (strftime('%s', 'now'))," + 
            "`record_id` VARCHAR);" +
            "CREATE INDEX IF NOT EXISTS `records_idx` ON `records` (" +
            "`hash`," +
            "`player_name`" +
            ");";
        using(var command = connection.CreateCommand()) {
            command.CommandText = commandText;
            command.ExecuteNonQuery();
        }
    }

    static int GetAdoptedChannelHash(ICollection<int> adoptedChannels) {
        int result = 0;
        unchecked {
            foreach(int channel in adoptedChannels) {
                if(channel >= 50) result |= 1 << (channel - 40);
                else result |= 1 << channel;
            }
        }
        return result;
    }

    public void CreateRecord(BMSManager bmsManager, string playerName = "Player") {
        const string commandText = "INSERT INTO `records`(`hash`, `channel_config`, `player_name`, `combos`, `score`, `record_id`) VALUES (?, ?, ?, ?, ?, ?);";
        var timeHash = DateTime.UtcNow.Ticks.ToBaseString(32);
        using(var command = connection.CreateCommand()) {
            command.CommandText = commandText;
            var parameters = command.Parameters;
            parameters.Add(new SqliteParameter(DbType.String, (object)bmsManager.GetHash(SongInfoLoader.CurrentEncoding, hashAlgorithm)));
            parameters.Add(new SqliteParameter(DbType.String, (object)GetAdoptedChannelHash(bmsManager.GetAllAdoptedChannels())));
            parameters.Add(new SqliteParameter(DbType.String, (object)playerName));
            parameters.Add(new SqliteParameter(DbType.Int32, (object)bmsManager.MaxCombos));
            parameters.Add(new SqliteParameter(DbType.Int32, (object)bmsManager.Score));
            parameters.Add(new SqliteParameter(DbType.String, (object)timeHash));
            command.ExecuteNonQuery();
        }
    }

    public Record[] GetRecords(string bmsHash) {
        const string commandText = "SELECT `player_name`, `channel_config`, `combos`, `score`, `time`, `record_id` FROM `records` WHERE `hash` = ? ORDER BY `score` DESC;";
        var result = new List<Record>();
        using(var command = connection.CreateCommand()) {
            command.CommandText = commandText;
            var parameters = command.Parameters;
            parameters.Add(new SqliteParameter(DbType.String, (object)bmsHash));
            using(var reader = command.ExecuteReader()) {
                while(reader.Read())
                    result.Add(new Record(
                        reader.GetString(0),
                        reader.GetInt32(1),
                        reader.GetInt32(2),
                        reader.GetInt32(3),
                        reader.GetDateTime(4),
                        reader.GetString(5)
                    ));
                reader.Close();
            }
        }
        return result.ToArray();
    }

    ~RecordsManager() {
        if(connection != null)
            connection.Dispose();
    }
}