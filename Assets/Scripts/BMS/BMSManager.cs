using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace BMS {
    [Flags]
    public enum BMSReloadOperation {
        None = 0,
        Header = 1,
        Body = 2,
        Resources = 4,
        ResourceHeader = 8
    }

    public delegate void StateChangedEvent();
    public delegate void ChangeBPMEvent(float bpm);
    public delegate void NoteEmitEvent(BMSEvent bmsEvent);
    public delegate void NoteClickEvent(TimeSpan expectedTimePosition, TimeSpan currentTimePosition, int channel, int eventId, int resultFlag);
    public delegate void ChangeBGAEvent(Texture texture, int channel, BGAObject? bga, int eventId);
    public delegate void BeatFlowEvent(float measureFlow, float beatFlow);

    public class LongNoteTimeHolder {
        int score, remainScore;
        long startTime, duration, previousUpdatetime;
        int channel;

        public event Action<int> OnAddScore;

        public TimeSpan StartTime {
            get { return new TimeSpan(startTime); }
            set { startTime = value.Ticks; }
        }

        public TimeSpan Duration {
            get { return new TimeSpan(duration); }
            set { duration = value.Ticks; }
        }

        public int Score {
            get { return score; }
            set { remainScore = score = value; }
        }

        public int Channel {
            get { return channel; }
        }

        public LongNoteTimeHolder(int channel) {
            this.channel = channel;
            ResetRemainTime();
        }

        public void AddRemainScore() {
            if(remainScore > 0) {
                if(OnAddScore != null)
                    OnAddScore.Invoke(remainScore);
                remainScore = 0;
            }
            ResetRemainTime();
        }

        public void ResetRemainTime() {
            previousUpdatetime = 0;
            score = 0;
        }

        public void Update(TimeSpan currentTime) {
            if(score == 0) return;
            if(remainScore < 0) {
                score = 0;
                return;
            }
            long newUpdateTime = currentTime.Ticks - startTime;
            bool exceed = newUpdateTime > duration;
            if(exceed) newUpdateTime = duration;
            int addScore = Mathf.FloorToInt((float)score * (newUpdateTime - previousUpdatetime) / duration);
            if(addScore > 0) {
                remainScore -= addScore;
                if(remainScore < 0)
                    addScore += remainScore;
                if(OnAddScore != null)
                   OnAddScore.Invoke(addScore);
                previousUpdatetime = newUpdateTime;
            }
            if(exceed) score = 0;
        }
    }

    public partial class BMSManager: MonoBehaviour {
        public Texture placeHolderTexture;
        Chart chart;

        static BMSManager() {
        }

        internal Chart.EventDispatcher EventDispatcher {
            get { return mainTimingHelper; }
        }

        public ICollection<int> GetAllChannelIds() {
            return chart.AllChannels;
        }

        public ICollection<int> GetAllAdoptedChannels() {
            return handledChannels;
        }

        SoundPlayer _soundPlayer;
#if !UNITY_ANDROID
        readonly HashSet<MovieTexture> playingMovieTextures = new HashSet<MovieTexture>();
#endif
        readonly HashSet<MovieTextureHolder> playingMovieTextureHolders = new HashSet<MovieTextureHolder>();
        readonly HashSet<int> handledChannels = new HashSet<int>();
        readonly Dictionary<int, int> autoPlayLNState = new Dictionary<int, int>();
        Chart.EventDispatcher mainTimingHelper;
        Chart.EventDispatcher preTimingHelper;
        DateTime startTime;
        TimeSpan timePosition;
        TimeSpan stopPosition;
        TimeSpan preEventOffset, calculatedPreOffset;
        TimeSpan preOffset;
        TimeSpan bpmBasePoint;
        float bpmBasePointBeatFlow = 0;
        float currentTimeSignature = 4;
        float accuracy = 0;
        int noteLimit;
        bool dynamicPreEvents;
        bool isStarted, isPaused;
        bool tightMode;

        // Score and combos
        [SerializeField]
        int maxScore = 1000000;
        int discountMaxScore;
        [SerializeField]
        float[] noteOffsetThesholds = new [] { 0F };
        [Range(0, 1), SerializeField]
        float[] scoreWeight = new[] { 0F };
        [Range(0, 1), SerializeField]
        float comboBonusWeight = 0.5F;
        int combos, maxCombos;
        int score, scorePerNote, extraScore;
        int[] comboBonus;
        readonly List<int> comboPools = new List<int>();
        int[] noteScoreCount;
        readonly Dictionary<int, LongNoteTimeHolder> lnHolders = new Dictionary<int, LongNoteTimeHolder>();

        [SerializeField]
        RankControl rankControl;
        string rankString;
        Color rankColor;
        bool rankSynced;

        bool bgaEnabled = true;
        bool detuneEnabled = true;

        public int Combos { get { return combos; } }
        public int MaxCombos { get { return maxCombos; } }
        public int MaxScore { get { return maxScore; } }
        public int Score { get { return score; } }
        public float Accuracy { get { return accuracy; } }
        public float TimeSignature { get { return currentTimeSignature; } }

        public Chart LoadedChart {
            get { return chart; }
        }

        public Color RankColor {
            get {
                SyncRank();
                return rankColor;
            }
        }

        public string RankString {
            get {
                SyncRank();
                return rankString;
            }
        }

        public bool BGAEnabled {
            get { return bgaEnabled; }
            set { bgaEnabled = value; }
        }

        public bool DetuneEnabled {
            get { return detuneEnabled; }
            set { detuneEnabled = value; }
        }

        void SyncRank() {
            if(rankSynced) return;
            rankControl.GetRank(score, out rankString, out rankColor);
            rankSynced = true;
        }

        [SerializeField]
        float preEventOffsetSeconds = 2;

        SoundPlayer soundPlayer {
            get {
                if(_soundPlayer == null)
                    _soundPlayer = GetComponent<SoundPlayer>() ?? gameObject.AddComponent<SoundPlayer>();
                return _soundPlayer;
            }
        }

        public bool DynamicPreEventOffset {
            get { return dynamicPreEvents; }
            set {
                dynamicPreEvents = value;
                CalculatePreOffset();
            }
        }

        public TimeSpan PreEventOffset {
            get {
                if(dynamicPreEvents)
                    return calculatedPreOffset;
                return preEventOffset;
            }
            set {
                if(value >= TimeSpan.Zero)
                    preEventOffset = value;
                CalculatePreOffset();
            }
        }

        public TimeSpan TimePosition {
            get {
                if(stopPosition > timePosition)
                    return stopPosition;
                return timePosition;
            }
        }

        public TimeSpan RealTimePosition {
            get { return timePosition; }
        }

        public float PercentageTimePassed {
            get { return duration > TimeSpan.Zero ? (float)(timePosition.TotalMilliseconds / duration.TotalMilliseconds) : 0; }
        }

        public int Polyphony {
            get { return soundPlayer.Polyphony; }
        }

        public int GetNoteScoreCount(int index) {
            return noteScoreCount[index];
        }

        public bool TightMode {
            get { return tightMode; }
            set { tightMode = value; }
        }

        public int NoteLimit {
            get { return noteLimit; }
            set { noteLimit = value; }
        }

        public event StateChangedEvent OnGameStarted;
        public event StateChangedEvent OnGameEnded;
        public event StateChangedEvent OnPauseChanged;
        public event ChangeBPMEvent OnChangeBPM;
        public event NoteEmitEvent OnPreNoteEvent;
        public event NoteEmitEvent OnNoteEvent;
        public event NoteClickEvent OnNoteClicked;
        public event ChangeBGAEvent OnChangeBackground;
        public event BeatFlowEvent OnBeatFlow;

        public bool IsStarted {
            get { return isStarted; }
            set {
                bool _isStarted = value && bmsLoaded;
                if(isStarted == _isStarted) return;
                if(_isStarted) {
                    var preWaitTime = startPos - TimeSpan.FromSeconds(3);
                    stopPosition = timePosition = preOffset = preWaitTime < TimeSpan.Zero ? preWaitTime : TimeSpan.Zero;
                    startTime = DateTime.Now;
                    combos = maxCombos = 0;
                    score = 0;
                    accuracy = 0;
                    bpm = chart.BPM;
                    comboPools.Clear();
                    lnHolders.Clear();
                    soundPlayer.StopAll();
                    soundPlayer.Volume = chart.Volume;
                    mainTimingHelper.Seek(TimeSpan.MinValue, false);
                    preTimingHelper.Seek(TimeSpan.MinValue, false);
                    autoPlayLNState.Clear();
                    if(noteScoreCount != null && noteScoreCount.Length > 0)
                        Array.Clear(noteScoreCount, 0, noteScoreCount.Length);
                    bpmBasePointBeatFlow = 0;
                    currentTimeSignature = 4;
                    bpmBasePoint = TimeSpan.Zero;
                    CalculatePreOffset();
                    if(OnGameStarted != null)
                        OnGameStarted.Invoke();
                } else {
                    soundPlayer.StopAll();
#if !UNITY_ANDROID
                    foreach(var movTexture in playingMovieTextures)
                        movTexture.Stop();
                    playingMovieTextures.Clear();
#endif
                    foreach(var movTexture in playingMovieTextureHolders)
                        movTexture.Stop();
                    playingMovieTextureHolders.Clear();
                    if(OnGameEnded != null)
                        OnGameEnded.Invoke();
                }
                isStarted = _isStarted;
                IsPaused = false;
            }
        }

        public bool IsPaused {
            get { return isPaused; }
            set {
                bool _isPaused = value && isStarted && bmsLoaded;
                if(isPaused == _isPaused) return;
                isPaused = _isPaused;
                if(OnPauseChanged != null)
                    OnPauseChanged.Invoke();
                soundPlayer.PauseChanged(_isPaused);
                if(_isPaused) {
                    preOffset = timePosition;
#if !UNITY_ANDROID
                    var temp = new HashSet<MovieTexture>();
                    foreach(var movTexture in playingMovieTextures)
                        if(movTexture.isPlaying) movTexture.Pause();
                        else temp.Add(movTexture);
                    playingMovieTextures.ExceptWith(temp);
#endif
                    var temp2 = new HashSet<MovieTextureHolder>();
                    foreach(var movTexture in playingMovieTextureHolders)
                        if(movTexture.IsPlaying) movTexture.Pause();
                        else temp2.Add(movTexture);
                    playingMovieTextureHolders.ExceptWith(temp2);
                } else {
                    startTime = DateTime.Now;
#if !UNITY_ANDROID
                    foreach(var movTexture in playingMovieTextures)
                        movTexture.Play();
#endif
                    foreach(var movTexture in playingMovieTextureHolders)
                        movTexture.Play();
                }
            }
        }

        void Awake() {
            Application.targetFrameRate = -1;
            if(preEventOffset == TimeSpan.Zero)
                preEventOffset = TimeSpan.FromSeconds(preEventOffsetSeconds);
        }

        public void InitializeNoteScore() {
            comboPools.Clear();
            score = maxCombos = combos = 0;
            int noteCount = 0, noteCount2 = 0;
            foreach(BMSEvent ev in chart.Events) {
                if(ev.IsNote) {
                    if(handledChannels.Contains(ev.data1)) noteCount++;
                    noteCount2++;
                }
            }
            if(noteCount < 1) return;
            int maxScore = discountMaxScore = Mathf.FloorToInt(this.maxScore * ((float)noteCount / noteCount2));
            int totalNormalScore = Mathf.FloorToInt(maxScore * (1 - comboBonusWeight));
            scorePerNote = totalNormalScore / noteCount;
            extraScore = totalNormalScore - scorePerNote * noteCount;
            comboPools.Capacity = noteCount / 2;
            comboBonus = new int[noteCount];
            int maxComboBonus = Mathf.FloorToInt((float)(maxScore - totalNormalScore) / noteCount * 2);
            int remainComboScore = maxScore - totalNormalScore;
            for(int i = 0; i < noteCount; i++) {
                comboBonus[i] = Mathf.FloorToInt((float)i / noteCount * maxComboBonus);
                remainComboScore -= comboBonus[i];
            }
            comboBonus[noteCount - 1] += remainComboScore;
            noteScoreCount = new int[scoreWeight.Length + 1];
        }

        public void AdoptChannel(int channel, bool adopted = true) {
            if(adopted) handledChannels.Add(channel);
            else handledChannels.Remove(channel);
        }

        void Update() {
            if(isStarted && !isPaused) {
                var now = DateTime.Now;
                timePosition = now - startTime + preOffset;
                TimeSpan calculatedTimePosition = TimePosition;
                mainTimingHelper.Seek(calculatedTimePosition);
                preTimingHelper.Seek(calculatedTimePosition + Scale(preEventOffset, Math.Max(1, 130 / chart.MinBPM)));
                foreach(var ln in lnHolders.Values)
                    ln.Update(calculatedTimePosition);
                if(OnBeatFlow != null) {
                    float beatFlow = (calculatedTimePosition - bpmBasePoint).ToAccurateMinuteF() * bpm + bpmBasePointBeatFlow;
                    OnBeatFlow.Invoke(Mathf.Repeat(beatFlow, 1), Mathf.Repeat(beatFlow, currentTimeSignature));
                }
                if(timePosition > endTimeTheshold && soundPlayer.Polyphony <= 0)
                    IsStarted = false;
            }
        }

        void OnDestroy() {
            ClearDataObjects(true, false, true);
        }

        void OnPreEvent(BMSEvent bmsEvent) {
            if(OnPreNoteEvent != null)
                OnPreNoteEvent.Invoke(bmsEvent);
        }

        void OnEventUpdate(BMSEvent bmsEvent) {
            System.Console.WriteLine("BMS Event {0} {1} {2}", bmsEvent.type, bmsEvent.data1, bmsEvent.data2);
            switch(bmsEvent.type) {
                case BMSEventType.WAV: PlayWAV((int)bmsEvent.data2, bmsEvent.sliceStart, bmsEvent.sliceEnd); break;
                case BMSEventType.BMP: ChangeBGA(bmsEvent.data1, (int)bmsEvent.data2); break;
                case BMSEventType.BeatReset:
                    bpmBasePointBeatFlow = 0;
                    bpmBasePoint = timePosition;
                    currentTimeSignature = (float)bmsEvent.Data2F;
                    break;
                case BMSEventType.BPM:
                    float newBpm = (float)bmsEvent.Data2F;
                    bpmBasePointBeatFlow += (timePosition - bpmBasePoint).ToAccurateMinuteF() * bpm;
                    bpmBasePoint = timePosition;
                    bpm = newBpm;
                    CalculatePreOffset();
                    if(OnChangeBPM != null)
                        OnChangeBPM.Invoke(newBpm);
                    break;
                case BMSEventType.STOP:
                    TimeSpan offset = new TimeSpan(bmsEvent.data2);
                    stopPosition = bmsEvent.time + offset;
                    bpmBasePoint -= offset;
                    break;
                case BMSEventType.LongNoteStart:
                case BMSEventType.LongNoteEnd:
                case BMSEventType.Note:
                    if(!handledChannels.Contains(bmsEvent.data1)) {
                        bool shouldPlayWav = true;
                        if(bmsEvent.type == BMSEventType.LongNoteEnd || bmsEvent.type == BMSEventType.LongNoteStart) {
                            int lnState;
                            if(!autoPlayLNState.TryGetValue(bmsEvent.data1, out lnState))
                                lnState = 0;
                            shouldPlayWav = lnState != bmsEvent.data2;
                            if(bmsEvent.type == BMSEventType.LongNoteStart)
                                autoPlayLNState[bmsEvent.data1] = (int)bmsEvent.data2;
                            else
                                autoPlayLNState.Remove(bmsEvent.data1);
                        }
                        if(shouldPlayWav) PlayWAV((int)bmsEvent.data2, bmsEvent.sliceStart, bmsEvent.sliceEnd);
                    }
                    if(OnNoteEvent != null)
                        OnNoteEvent.Invoke(bmsEvent);
                    break;
                default:
                    if(OnNoteEvent != null)
                        OnNoteEvent.Invoke(bmsEvent);
                    break;
            }
        }

        void ChangeBGA(int channel, int eventId) {
            if(bgaEnabled && OnChangeBackground != null) {
                Texture bmp;
                var bga = GetBGA(eventId);
                BGAObject? _bga = null;
                if(bga.index != -99) {
                    _bga = bga;
                    eventId = bga.index;
                }
                bmp = GetBMP(eventId);
                if(bmp == null && channel == 0 && !hasBGA)
                    bmp = placeHolderTexture;
                OnChangeBackground.Invoke(bmp, channel, _bga, eventId);
            }
        }

        void PlayWAV(int eventId, TimeSpan sliceStart, TimeSpan sliceEnd, bool isPlayer = false, float pitch = 1) {
            if(eventId == 0) return;
            var wav = GetWAV(eventId);
            if(wav != 0)
                soundPlayer.PlaySound(wav, sliceStart, sliceEnd, eventId, isPlayer, pitch, "");
        }

        public bool IsValidFlag(int flag) {
            return flag >= 0 && flag < scoreWeight.Length;
        }

        void CalculatePreOffset() {
            if(!dynamicPreEvents) {
                calculatedPreOffset = preEventOffset;
                return;
            }
            calculatedPreOffset = Scale(preEventOffset, 130 / bpm);
        }

        static TimeSpan Scale(TimeSpan source, double ratio) {
            return new TimeSpan((long)Math.Floor(source.Ticks * ratio));
        }

        void AddScore(int addScore) {
            score += addScore;
        }

        LongNoteTimeHolder GetHolder(int channel) {
            LongNoteTimeHolder lnHolder;
            if(!lnHolders.TryGetValue(channel, out lnHolder)) {
                lnHolders[channel] = lnHolder = new LongNoteTimeHolder(channel);
                lnHolder.OnAddScore += AddScore;
            }
            return lnHolder;
        }

        void ResetHolder(int channel) {
            LongNoteTimeHolder lnHolder;
            if(lnHolders.TryGetValue(channel, out lnHolder))
                lnHolder.ResetRemainTime();
        }

        void AddRemainTime(int channel) {
            LongNoteTimeHolder lnHolder;
            if(lnHolders.TryGetValue(channel, out lnHolder))
                lnHolder.AddRemainScore();
        }

        public int NoteClicked(
            TimeSpan expectedTimePosition,
            int channel, int eventId, bool countAsMiss,
            TimeSpan sliceStart, TimeSpan sliceEnd,
            bool hasSound = true, TimeSpan? endNotePos = null) {
            if(!isStarted || isPaused) return -2;
            var timeDiff = TimePosition - expectedTimePosition;

            int resultFlag = -1;
            if(!countAsMiss) {
                for(int i = 0, count = noteOffsetThesholds.Length; i < count - 1; i++) {
                    if(timeDiff.TotalSeconds < noteOffsetThesholds[i]) break;
                    resultFlag = Mathf.Abs(i + 1 - count / 2);
                }
                accuracy = (float)timeDiff.TotalMilliseconds;
            }
            if(IsValidFlag(resultFlag)) {
                int addScore = 0;
                if(tightMode) {
                    float seconds = (float)timeDiff.TotalSeconds;
                    float temp = 1 - seconds / (seconds < 0 ? noteOffsetThesholds[0] : noteOffsetThesholds.Last());
                    addScore = Mathf.FloorToInt(scorePerNote * temp) + comboBonus[combos];
                } else {
                    addScore = Mathf.FloorToInt(scorePerNote * scoreWeight[resultFlag]) + comboBonus[combos];
                }
                if(endNotePos.HasValue) {
                    var lnHolder = GetHolder(channel);
                    lnHolder.ResetRemainTime();
                    lnHolder.StartTime = timePosition;
                    lnHolder.Duration = endNotePos.Value - timePosition;
                    lnHolder.Score = addScore;
                } else {
                    score += addScore;
                    if(resultFlag == 0)
                        AddRemainTime(channel);
                    else
                        ResetHolder(channel);
                }
                noteScoreCount[resultFlag]++;
            } else {
                if(combos > 1) comboPools.Add(combos);
                combos = -1;
                noteScoreCount[noteScoreCount.Length - 1]++;
                ResetHolder(channel);
            }
            if(score + extraScore >= maxScore)
                score = maxScore;
            combos++;
            maxCombos = Mathf.Max(combos, maxCombos);
            rankSynced = false;

            if(hasSound && IsValidFlag(resultFlag))
                PlayWAV(eventId, sliceStart, sliceEnd, true, detuneEnabled && resultFlag > 1 ? Mathf.Clamp(1 + accuracy / 1000, 0.5F, 1.5F) : 1);

            if(OnNoteClicked != null)
                OnNoteClicked.Invoke(expectedTimePosition, timePosition, channel, eventId, resultFlag);

            return resultFlag;
        }
    }
}