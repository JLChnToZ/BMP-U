using System;
using System.IO;
using System.Collections.Generic;
using SQLite4Unity3d;
using BMS;
using BananaBeats.Utils;
using BananaBeats.Layouts;
using BananaBeats.Inputs;
using UnityEngine;
using UniRx.Async;

namespace BananaBeats.PlayerData {
    public class PlayerDataManager: IDisposable {
        private readonly SQLiteConnection db;

        private static PlayerDataManager instance;
        public static PlayerDataManager Instance {
            get {
                if(instance == null)
                    instance = new PlayerDataManager();
                return instance;
            }
        }

        public PlayerDataManager() {
            db = new SQLiteConnection(
                Path.Combine(FilsSystemHelper.AppPath, "player.dat"),
                SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite);
            db.CreateTable<KeyBinding>();
            db.CreateTable<KeyLayouts>();
            db.CreateTable<SongInfo>();
        }

        public IEnumerable<IKeyLayouts> GetLayouts() =>
            db.Table<KeyLayouts>();

        public void SaveLayout(BMSKeyLayout layoutType, int[] layout) =>
            db.InsertOrReplace(new KeyLayouts(layoutType, layout));

        public void ClearAllBindings() {
            var mapping = db.GetMapping<KeyBinding>();
            db.Execute($"DELETE FROM {mapping.TableName}");
        }

        public void SetKeyBinding(Guid guid, BMSKeyLayout layout, string path) {
            db.Insert(new KeyBinding {
                Guid = guid,
                LayoutType = layout,
                Path = path,
            });
        }

        public IEnumerable<KeyBinding> GetKeyBinding() =>
            db.Table<KeyBinding>();

        public string GetKeyBinding(Guid guid, BMSKeyLayout layout) {
            var mapping = db.GetMapping<KeyBinding>();
            foreach(var result in db.DeferredQuery<KeyBinding>(
                $"SELECT * FROM {mapping.TableName} WHERE {mapping.FindColumnWithPropertyName("Guid").Name} = ? AND {mapping.FindColumnWithPropertyName("LayoutType").Name} = ? LIMIT 1",
                guid, layout))
                return result.Path;
            return string.Empty;
        }

        public void UpdateSongInfo(string path, Chart chart) =>
            db.InsertOrReplace(new SongInfo(path, chart));

        public void ClearSongInfo(string path) {
            var mapping = db.GetMapping<SongInfo>();
            path = path.Replace("/", "//").Replace("%", "/%").Replace("_", "/_");
            db.Execute($"DELECT FROM {mapping.TableName} WHERE {mapping.FindColumnWithPropertyName("Path").Name} LIKE ? ESCAPE ?", path + "%", "/");
        }

        public void Dispose() {
            if(instance == this)
                instance = null;
            db.Dispose();
        }


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Load() => LoadAsync().Forget();

        private static async UniTaskVoid LoadAsync() {
            using(var playerData = new PlayerDataManager()) {
                await UniTask.SwitchToThreadPool();
                InputManager.Load(playerData);
                NoteLayoutManager.Load(playerData);
                await UniTask.SwitchToMainThread();
            }
        }

        public static void Save() => SaveAsync().Forget();

        private static async UniTaskVoid SaveAsync() {
            using(var playerData = new PlayerDataManager()) {
                await UniTask.SwitchToThreadPool();
                InputManager.Save(playerData);
                NoteLayoutManager.Save(playerData);
                await UniTask.SwitchToMainThread();
            }
        }
    }

    public interface IKeyLayouts {
        BMSKeyLayout LayoutType { get; set; }
        int[] LayoutData { get; set; }
    }

    [Serializable]
    public class KeyLayouts: IKeyLayouts {

        public KeyLayouts() { }

        public KeyLayouts(BMSKeyLayout layoutType, int[] layout) {
            LayoutType = layoutType;
            LayoutData = layout;
        }

        [Unique]
        public BMSKeyLayout LayoutType { get; set; }

        public string Layout {
            get {
                return string.Join(",", Array.ConvertAll(LayoutData, Convert.ToString));
            }
            set {
                LayoutData = Array.ConvertAll(value.Split(','), Convert.ToInt32);
            }
        }

        [Ignore]
        public int[] LayoutData { get; set; }
    }

    [Serializable]
    public class KeyBinding {
        [Indexed]
        public Guid Guid { get; set; }

        [Indexed]
        public BMSKeyLayout LayoutType { get; set; }

        public string Path { get; set; }
    }

    [Serializable]
    public class SongInfo {

        public SongInfo() { }

        public SongInfo(string path, Chart chart) {
            Path = path;
            Title = chart.Title;
            Artist = chart.Artist;
            Genre = chart.Genre;
            Rank = chart.Rank;
            PlayLevel = chart.PlayLevel;
        }

        [Unique]
        public string Path { get; set; }

        [Indexed]
        public string Title { get; set; }

        [Indexed]
        public string Artist { get; set; }

        [Indexed]
        public string Genre { get; set; }

        [Indexed]
        public int Rank { get; set; }

        [Indexed]
        public float PlayLevel { get; set; }
    }
}
