using System;

namespace BananaBeats {

    public class ScoreEventArgs: EventArgs {
        public int RankType { get; private set; }

        public TimeSpan TimeDiff { get; private set; }

        public int ScoreGet { get; private set; }

        public int TotalScore { get; private set; }

        public int Combos { get; private set; }

        public ScoreEventArgs(int rankType, TimeSpan timeDiff, int scoreGet, int totalScore, int combos) {
            RankType = rankType;
            TimeDiff = timeDiff;
            ScoreGet = scoreGet;
            TotalScore = totalScore;
            Combos = combos;
        }
    }

    public class ScoreCalculator {
        private readonly ScoreConfig scoreConfig;
        private int[] comboScores;

        public int MaxScore =>
            scoreConfig.maxScore;

        public float ComboBonusRatio =>
            scoreConfig.comboBonusRatio;

        public TimingConfig[] TimingConfigs =>
            scoreConfig.timingConfigs;

        public int MaxNotes {
            get { return maxNotes; }
            set {
                if(maxNotes == value) return;
                maxNotes = value;
                Init();
            }
        }
        private int maxNotes;

        public int Score { get; private set; }

        public int Combos { get; private set; }

        public event EventHandler<ScoreEventArgs> OnScore;

        public ScoreCalculator(ScoreConfig scoreConfig) {
            if(scoreConfig.timingConfigs == null)
                throw new ArgumentException("Timing configs in score config is null.", nameof(scoreConfig));
            this.scoreConfig = scoreConfig;
            Array.Sort(this.scoreConfig.timingConfigs);
            Init();
        }

        public ScoreCalculator(ScoreConfig scoreConfig, int maxNotes) {
            if(scoreConfig.timingConfigs == null)
                throw new ArgumentException("Timing configs in score config is null.", nameof(scoreConfig));
            this.scoreConfig = scoreConfig;
            this.maxNotes = maxNotes;
            Init();
        }

        public int HitNote(TimeSpan timeDiff, bool checkMiss = false) {
            if(Combos >= maxNotes)
                throw new InvalidOperationException("Combos already excess max value!");
            if(checkMiss && timeDiff > TimeSpan.Zero)
                return 0;
            var ticksDiff = Math.Abs(timeDiff.Ticks);
            foreach(var timing in scoreConfig.timingConfigs) {
                if(ticksDiff >= timing.secondsDiff * TimeSpan.TicksPerSecond)
                    continue;
                if(!checkMiss) {
                    int scoreAdd = (int)Math.Floor(comboScores[++Combos] * timing.score);
                    Score += scoreAdd;
                    OnScore?.Invoke(this, new ScoreEventArgs(timing.rankType, timeDiff, scoreAdd, Score, Combos));
                }
                return timing.rankType;
            }
            MissNote();
            return -1;
        }

        public void MissNote() {
            Combos = 0;
            OnScore?.Invoke(this, new ScoreEventArgs(-1, TimeSpan.Zero, 0, Score, 0));
        }

        public void Reset() {
            Score = 0;
            Combos = 0;
        }

        private void Init() {
            comboScores = new int[maxNotes];
            if(maxNotes > 0) {
                double totalBaseScore = scoreConfig.maxScore * (1 - scoreConfig.comboBonusRatio);
                double comboScore = (scoreConfig.maxScore - totalBaseScore) * 2 / maxNotes / maxNotes;
                int baseScore = (int)Math.Floor(totalBaseScore / maxNotes);
                int remainScore = scoreConfig.maxScore;
                for(int i = 0; i < maxNotes; i++) {
                    comboScores[i] = baseScore + (int)Math.Floor(i * comboScore);
                    remainScore -= comboScores[i];
                }
                comboScores[maxNotes - 1] += remainScore;
            }
            Reset();
        }
    }

    [Serializable]
    public struct ScoreConfig {
        public int maxScore;
        public float comboBonusRatio;
        public TimingConfig[] timingConfigs;
    }

    [Serializable]
    public struct TimingConfig: IComparable<TimingConfig>, IEquatable<TimingConfig> {
        public int rankType;
        public float score;
        public float secondsDiff;

        int IComparable<TimingConfig>.CompareTo(TimingConfig other) =>
            secondsDiff.CompareTo(other.secondsDiff);

        public bool Equals(TimingConfig other) =>
            rankType == other.rankType &&
            score == other.score &&
            secondsDiff == other.secondsDiff;

        public override bool Equals(object obj) =>
            obj is TimingConfig other && Equals(other);

        public override int GetHashCode() =>
            rankType ^
            score.GetHashCode() ^
            secondsDiff.GetHashCode();

        public override string ToString() =>
            $"@{secondsDiff}: R{rankType} (score: {score:P})";
    }
}
