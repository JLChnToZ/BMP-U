using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

using BMS;

public class InfoHandler : MonoBehaviour {

    public BMSManager bmsManager;
    public GraphHelper graphHandler;

    public Text infoDisplay;
    public Text infoDisplay2;
    public RawImage bgTexture;
    public Text scoreDisplay;
    public Text comboDisplay;
    [UnityEngine.Serialization.FormerlySerializedAs("polyphonyDisplay")]
    public Text debugInfoDisplay;
    public RawImage graphDisplay;

    public RectTransform loadingBar;
    public RectTransform durationBar;
    public Image fpsBar, polyphonyBar, accuracyBar;
    public Text fpsText, polyphonyText, accuracyText, bpmText;

    public RectTransform panel;
    public Text resultText;

    public RectTransform pausePanel;

    public GameObject dummyBGA;

    int displayingScore, displayCombos, targetCombos;
    float combosValue;

    bool bmsLoaded, stageFileLoaded, gameStarted, gameEnded, pauseChanged, startOnLoad, backgroundChanged;
    
    void Start () {
        if(bmsManager) {
            bmsManager.OnBMSLoaded += OnBMSLoaded;
            bmsManager.OnStageFileLoaded += OnStageFileLoaded;
            bmsManager.OnGameStarted += OnGameStarted;
            bmsManager.OnGameEnded += OnGameEnded;
            bmsManager.OnPauseChanged += OnPauseChanged;
            bmsManager.OnChangeBackground += OnChangeBackground;
        }
        if(graphDisplay) {
            if(graphHandler)
                graphHandler.size = graphDisplay.rectTransform.rect.size;
            graphDisplay.enabled = false;
        }
        scoreDisplay.enabled = Loader.judgeMode != 2;
        comboDisplay.enabled = Loader.judgeMode != 2;
    }

    void OnDestroy() {
        if(bmsManager) {
            bmsManager.OnBMSLoaded -= OnBMSLoaded;
            bmsManager.OnStageFileLoaded -= OnStageFileLoaded;
            bmsManager.OnGameStarted -= OnGameStarted;
            bmsManager.OnGameEnded -= OnGameEnded;
            bmsManager.OnPauseChanged -= OnPauseChanged;
            bmsManager.OnChangeBackground -= OnChangeBackground;
        }
    }

    void Update() {
        bool triggerLoadingbar = bmsLoaded;
        if(bmsLoaded) {
            bmsLoaded = false;
            infoDisplay.text = string.Format(LanguageLoader.GetText(17), bmsManager.Title, bmsManager.Artist, bmsManager.PlayLevel);
            infoDisplay2.text = string.Format(LanguageLoader.GetText(18), bmsManager.SubArtist, bmsManager.Comments);
            if(startOnLoad) {
                if(!bmsManager.BGAEnabled)
                    bmsManager.placeHolderTexture = Texture2D.whiteTexture;
                bmsManager.IsStarted = true;
                startOnLoad = false;
                gameStarted = true;
            }
        }
        if(stageFileLoaded) {
            stageFileLoaded = false;
            bgTexture.texture = bmsManager.StageFile;
            bgTexture.enabled = bmsManager.StageFile != null && !bmsManager.IsStarted;
        }
        if(gameEnded) {
            gameEnded = false;
            panel.gameObject.SetActive(true);
            if(Loader.judgeMode != 2)
                resultText.text = string.Format(LanguageLoader.GetText(21),
                    bmsManager.Score,
                    bmsManager.MaxCombos,
                    bmsManager.GetNoteScoreCount(0),
                    bmsManager.GetNoteScoreCount(1),
                    bmsManager.GetNoteScoreCount(2),
                    bmsManager.GetNoteScoreCount(3),
                    string.Format(
                        "<color=#{0}>{1}</color>",
                        ColorUtility.ToHtmlStringRGBA(bmsManager.RankColor),
                        bmsManager.RankString
                    )
                );
            bgTexture.enabled = bgTexture.texture != null;
            if(graphDisplay) {
                if(graphHandler)
                    graphDisplay.texture = graphHandler.Texture;
                graphDisplay.enabled = graphDisplay.texture;
            }
            var recordsManager = RecordsManager.Instance;
            if(Loader.judgeMode == 2) {
                var hash = bmsManager.GetHash(SongInfoLoader.CurrentEncoding, recordsManager.HashAlgorithm);
                int channelHash = RecordsManager.GetAdoptedChannelHash(bmsManager.GetAllAdoptedChannels());
                var records = recordsManager.GetRecord(hash, channelHash);
                bool pass = bmsManager.Score >= bmsManager.MaxScore / 2;
                if(pass && records != null)
                    pass = bmsManager.Score >= records.Value.score;
                resultText.text = LanguageLoader.GetText(pass ? 33 : 34);
            }
            if(!Loader.autoMode) {
                recordsManager.CreateRecord(bmsManager);
            }
        }
        if(gameStarted) {
            gameStarted = false;
            panel.gameObject.SetActive(false);
            pausePanel.gameObject.SetActive(false);
            dummyBGA.SetActive(bmsManager.BGAEnabled);
            bgTexture.enabled = false;
        }
        if(pauseChanged) {
            pauseChanged = false;
            pausePanel.gameObject.SetActive(bmsManager.IsPaused);
        }
        if(backgroundChanged) {
            backgroundChanged = false;
            dummyBGA.SetActive(false);
        }
        float t = Time.deltaTime * 10;
        if(bmsManager.Score < displayingScore)
            displayingScore = bmsManager.Score;
        displayingScore = Mathf.CeilToInt(Mathf.Lerp(displayingScore, bmsManager.Score, t));
        scoreDisplay.text = displayingScore.ToString("0000000");

        if(targetCombos != bmsManager.Combos) {
            combosValue = combosValue + bmsManager.Combos - targetCombos;
            targetCombos = bmsManager.Combos;
        } else {
            combosValue = Mathf.Lerp(combosValue, 0, t / 4);
        }
        if(displayCombos < targetCombos)
            displayCombos = targetCombos;
        displayCombos = Mathf.FloorToInt(Mathf.Lerp(displayCombos, bmsManager.Combos, t));
        comboDisplay.text = (bmsManager.IsStarted && displayCombos >= 3) ? displayCombos.ToString() : "";
        comboDisplay.transform.localScale = Vector3.one * (1 + Mathf.Log(Mathf.Max(0, combosValue) + 1, 8));
        if(bmsManager.IsLoadingResources || triggerLoadingbar) {
            var anchorMax = loadingBar.anchorMax;
            anchorMax.x = bmsManager.LoadResourceProgress;
            loadingBar.anchorMax = anchorMax;
        }

        if(durationBar.gameObject.activeSelf != bmsManager.IsStarted)
            durationBar.gameObject.SetActive(bmsManager.IsStarted);
        if(bmsManager.IsStarted) {
            var anchorPos = new Vector2(bmsManager.PercentageTimePassed, durationBar.anchorMin.y);
            durationBar.anchorMin = anchorPos;
            durationBar.anchorMax = anchorPos;
        }
        float deltaTime = Time.unscaledDeltaTime;
        if(fpsBar) UpdateVerticalBar(fpsBar, deltaTime * 10F);
        if(fpsText) fpsText.text = string.Format("{0:0} FPS", 1 / deltaTime);
        if(polyphonyBar) UpdateVerticalBar(polyphonyBar, bmsManager.Polyphony / 255F);
        if(polyphonyText) polyphonyText.text = string.Format("{0} POLY", bmsManager.Polyphony);
        if(accuracyBar) UpdateHorzBar(accuracyBar, bmsManager.Accuracy / 50F);
        if(accuracyText) accuracyText.text = string.Format("{0:+0000;-0000}MS", bmsManager.Accuracy);
        if(bpmText) bpmText.text = string.Format("{0:0.0}BPM", bmsManager.BPM);
    }

    void OnBMSLoaded() {
        if(startOnLoad)
            bmsManager.InitializeNoteScore();
        bmsLoaded = true;
    }

    void OnStageFileLoaded() {
        stageFileLoaded = true;
    }

    void OnGameStarted() {
        gameStarted = true;
    }

    void OnGameEnded() {
        gameEnded = true;
    }

    void OnPauseChanged() {
        pauseChanged = true;
    }

    void OnChangeBackground(Texture texture, int channel, BGAObject? bga, int eventId) {
        if(texture != null && !(texture is RenderTexture))
            backgroundChanged = true;
    }

    void UpdateVerticalBar(Image image, float percentage) {
        var transform = image.rectTransform;
        if(percentage < 0) {
            transform.anchorMin = new Vector2(transform.anchorMin.x, -Mathf.Clamp01(-percentage));
            transform.anchorMax = new Vector2(transform.anchorMax.x, 0);
        } else {
            transform.anchorMin = new Vector2(transform.anchorMin.x, 0);
            transform.anchorMax = new Vector2(transform.anchorMax.x, Mathf.Clamp01(percentage));
        }
        SetImageColor(image, percentage);
    }

    void UpdateHorzBar(Image image, float percentage) {
        var transform = image.rectTransform;
        if(percentage < 0) {
            transform.anchorMin = new Vector2(-Mathf.Clamp01(-percentage), transform.anchorMin.y);
            transform.anchorMax = new Vector2(0, transform.anchorMax.y);
        } else {
            transform.anchorMin = new Vector2(0, transform.anchorMin.y);
            transform.anchorMax = new Vector2(Mathf.Clamp01(percentage), transform.anchorMax.y);
        }
        SetImageColor(image, percentage);
    }

    void SetImageColor(Image image, float percentage) {
        percentage = Mathf.Clamp01(Mathf.Abs(percentage));
        if(percentage < 0.5) image.color = Color.Lerp(Color.green, Color.yellow, percentage * 2);
        else image.color = Color.Lerp(Color.yellow, Color.red, percentage * 2 - 1);
    }

    public void AgainClick() {
        bmsManager.IsStarted = false;
        if(bmsManager.HasRandom) {
            startOnLoad = true;
            bmsManager.ReloadBMS(BMSReloadOperation.Body);
        } else {
            bmsManager.IsStarted = true;
        }
    }

    public void BackClick() {
        SceneManager.LoadScene("MenuScene");
    }

    public void PauseClick() {
        bmsManager.IsPaused = true;
    }

    public void ResumeClick() {
        bmsManager.IsPaused = false;
    }
}
