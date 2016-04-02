using System;
using System.Text;

using Texture = UnityEngine.Texture;

public struct SongInfo:IEquatable<SongInfo>, IComparable<SongInfo> {
    public int index;
    public string filePath;
    public string name;
    public string artist;
    public string subArtist;
    public string genre;
    public string comments;
    public float level;
    public float bpm;
    public Texture background;
    public string backgroundPath;

    public override bool Equals(object obj) {
        if(obj == null || !(obj is SongInfo))
            return false;
        return Equals((SongInfo)obj);
    }

    public bool Equals(SongInfo other) {
        return filePath == other.filePath && background == other.background;
    }

    public override int GetHashCode() {
        return filePath.GetHashCode() * 29;
    }

    public int CompareTo(SongInfo other) {
        return index.CompareTo(other.index);
    }
}

static class SongInfoLoader {
    static Encoding encoding = Encoding.Default;
    public static Encoding CurrentEncoding {
        get { return encoding; }
        set {
            encoding = value ?? Encoding.Default;
        }
    }

    public static int CurrentCodePage {
        get { return encoding.CodePage; }
        set {
            encoding = Encoding.GetEncoding(value) ?? Encoding.Default;
        }
    }

    static int index = 0;
    public static int GetNextIndex() {
        return index++;
    }
}

public static class SongInfoComparer {
    public static int CompareByName(SongInfo lhs, SongInfo rhs) {
        return string.Compare(lhs.name, rhs.name, StringComparison.InvariantCultureIgnoreCase);
    }

    public static int CompareByNameInverse(SongInfo lhs, SongInfo rhs) {
        return string.Compare(rhs.name, lhs.name, StringComparison.InvariantCultureIgnoreCase);
    }

    public static int CompareByArtist(SongInfo lhs, SongInfo rhs) {
        return string.Compare(lhs.artist, rhs.artist, StringComparison.InvariantCultureIgnoreCase);
    }

    public static int CompareByArtistInverse(SongInfo lhs, SongInfo rhs) {
        return string.Compare(rhs.artist, lhs.artist, StringComparison.InvariantCultureIgnoreCase);
    }

    public static int CompareByGenre(SongInfo lhs, SongInfo rhs) {
        return string.Compare(lhs.genre, rhs.genre, StringComparison.InvariantCultureIgnoreCase);
    }

    public static int CompareByGenreInverse(SongInfo lhs, SongInfo rhs) {
        return string.Compare(rhs.genre, lhs.genre, StringComparison.InvariantCultureIgnoreCase);
    }

    public static int CompareByBPM(SongInfo lhs, SongInfo rhs) {
        return lhs.bpm.CompareTo(rhs.bpm);
    }

    public static int CompareByBPMInverse(SongInfo lhs, SongInfo rhs) {
        return rhs.bpm.CompareTo(lhs.bpm);
    }

    public static int CompareByLevel(SongInfo lhs, SongInfo rhs) {
        return lhs.level.CompareTo(rhs.level);
    }

    public static int CompareByLevelInverse(SongInfo lhs, SongInfo rhs) {
        return rhs.level.CompareTo(lhs.level);
    }

    public enum SortMode {
        Name, NameInverse,
        Artist, ArtistInverse,
        Genre, GenreInverse,
        BPM, BPMInverse,
        Level, LevelInverse
    }
}
