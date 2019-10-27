using System;
using UnityEngine;
using UnityEngine.UI;
using BMS;
using UniRx.Async;
using BananaBeats.Utils;

namespace BananaBeats.HUD {
    public class GameHUDManager: MonoBehaviour {
        private static Action<BMSLoader> updateHUD;

        public static void UpdateHUD(BMSLoader loader) =>
            updateHUD?.Invoke(loader);

        public CanvasGroup infoDisplayGroup;
        public Text titleText;
        public Text subTitleText;
        public Text artistText;
        public Text subArtistText;
        public Text genreText;
        public Text commentsText;
        public Text levelText;
        public Text bpmText;
        public RawImage loadingBackground;
        public Text scoreText;
        public Text comboText;
        public Image scoreRankDisplay;
        public Sprite[] rankSprites;
        public Image songProgressDisplay;
        private IDisposable updateSongProgress;

        private Animation scoreRankDisplayAnim;

        protected void Awake() {
            BMSPlayableManager.OnScore += OnScoreUpdate;
            BMSPlayableManager.GlobalPlayStateChanged += PlayStateChanged;
            BMSPlayableManager.GlobalBMSEvent += OnBMSEvent;
            updateHUD += UpdateHUDHandler;
            if(scoreRankDisplay != null)
                scoreRankDisplayAnim = scoreRankDisplay.GetComponent<Animation>();
            if(songProgressDisplay != null)
                updateSongProgress = GameLoop.RunAsUpdate(UpdateProgress);
        }

        protected void OnDestroy() {
            BMSPlayableManager.OnScore -= OnScoreUpdate;
            BMSPlayableManager.GlobalPlayStateChanged -= PlayStateChanged;
            BMSPlayableManager.GlobalBMSEvent -= OnBMSEvent;
            updateHUD -= UpdateHUDHandler;
            updateSongProgress?.Dispose();
        }

        protected void OnScoreUpdate(object sender, ScoreEventArgs e) {
            if(scoreText != null)
                scoreText.text = e.TotalScore.ToString();
            if(comboText != null)
                comboText.text = e.Combos > 1 ? e.Combos.ToString() : string.Empty;
            if(scoreRankDisplay != null) {
                int rank = e.RankType;
                if(rank < 0) rank += rankSprites.Length;
                scoreRankDisplay.sprite = rankSprites[rank];
            }
            if(scoreRankDisplayAnim != null) {
                scoreRankDisplayAnim.Rewind();
                scoreRankDisplayAnim.Play();
            }
        }

        private void UpdateText(Text text, string content) {
            if(text == null) return;
            text.gameObject.SetActive(!string.IsNullOrEmpty(content));
            text.text = content ?? string.Empty;
        }

        protected void UpdateHUDHandler(BMSLoader loader) {
            var chart = loader.Chart;
            if(infoDisplayGroup != null) infoDisplayGroup.alpha = 1;
            UpdateText(titleText, chart.Title);
            UpdateText(subTitleText, chart.SubTitle);
            UpdateText(artistText, chart.Artist);
            UpdateText(subArtistText, chart.SubArtist);
            UpdateText(genreText, chart.Genre);
            UpdateText(commentsText, chart.Comments);
            UpdateText(levelText, chart.PlayLevel.ToString());
            UpdateText(bpmText, chart.BPM.ToString());
            LoadStageImage(loader).Forget();
        }

        private async UniTaskVoid LoadStageImage(BMSLoader loader) {
            if(loadingBackground == null) return;
            var stageImage = await loader.GetStageImage();
            loadingBackground.enabled = stageImage != null;
            if(stageImage != null) {
                loadingBackground.texture = stageImage.Texture;
                var imgTransform = stageImage.Transform;
                loadingBackground.uvRect = new Rect(
                    imgTransform.x < 0 ? 1 : 0,
                    imgTransform.y < 0 ? 1 : 0,
                    imgTransform.x,
                    imgTransform.y
                );
                loadingBackground.SizeToParent();
            }
        }

        private void PlayStateChanged(object sender, EventArgs e) {
            if(!(sender is BMSPlayableManager player)) return;
            if(infoDisplayGroup != null)
                infoDisplayGroup.alpha = player.PlaybackState == PlaybackState.Playing ? 0 : 1;
            if(player.PlaybackState == PlaybackState.Stopped)
                UpdateText(bpmText, player.BMSLoader.Chart.BPM.ToString());
        }

        protected void OnBMSEvent(BMSEvent bmsEvent, object resource) {
            switch(bmsEvent.type) {
                case BMSEventType.BPM:
                    UpdateText(bpmText, bmsEvent.Data2F.ToString());
                    break;
            }
        }

        private void UpdateProgress() {
            if(songProgressDisplay == null) {
                updateSongProgress.Dispose();
                updateSongProgress = null;
                return;
            }
            var instance = BMSPlayableManager.Instance;
            if(instance != null)
                songProgressDisplay.fillAmount = 
                    (float)instance.CurrentPosition.Ticks / instance.Duration.Ticks;
        }
    }
}
