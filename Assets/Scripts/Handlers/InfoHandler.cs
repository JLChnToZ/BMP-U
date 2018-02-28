using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Serialization;

using BMS;

public class InfoHandler : MonoBehaviour {

    public BMSManager bmsManager;
    public GraphHelper graphHandler;

    [FormerlySerializedAs("infoDisplay")]
    public Text titleDisplay;
    public Text artistDisplay;
    [FormerlySerializedAs("infoDisplay2")]
    public Text levelDisplay;
    public RawImageFitter bgTexture;
    public Text scoreDisplay;
    public Text comboDisplay;
    public Text debugInfoDisplay;
    public RawImage graphDisplay;
    
    public RectTransform durationBar;
    public Image fpsBar, polyphonyBar, accuracyBar;
    public Text fpsText, polyphonyText, accuracyText, bpmText;

    public RectTransform panel, detailsPanel;
    public Text resultText;

    public Text[] resultCountText;
    public Text resultComboText;
    public Text resultScoreText;
    public Text resultRankText;

    public RectTransform pauseButton;

    public RectTransform pausePanel;

    public GameObject dummyBGA;
    public Color bpmLightColor = Color.yellow;

    int displayingScore, displayCombos, targetCombos;
    float combosValue;

    bool bmsLoaded, stageFileLoaded, gameStarted, gameEnded, pauseChanged, startOnLoad, backgroundChanged;
    string bpmFormatText = "{0:0.0}BPM";
    float bpmLightLerp = 0;
    
    void Start () {
        if(bmsManager) {
            bmsManager.OnBMSLoaded += OnBMSLoaded;
            bmsManager.OnStageFileLoaded += OnStageFileLoaded;
            bmsManager.OnGameStarted += OnGameStarted;
            bmsManager.OnGameEnded += OnGameEnded;
            bmsManager.OnPauseChanged += OnPauseChanged;
            bmsManager.OnChangeBackground += OnChangeBackground;
            bmsManager.OnBeatFlow += BeatFlow;
            SetPauseButton();
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
            bmsManager.OnBeatFlow -= BeatFlow;
        }
    }

    void Update() {
        bool triggerLoadingbar = bmsLoaded;
        if(bmsLoaded) {
            bmsLoaded = false;
            titleDisplay.text = bmsManager.Title;
            artistDisplay.text = string.Format(LanguageLoader.GetText(17), bmsManager.Artist, bmsManager.SubArtist.Replace("\n", " / "));
            levelDisplay.text = bmsManager.PlayLevel.ToString();
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
            bgTexture.SetTexture(bmsManager.StageFile);
            bgTexture.rawImage.enabled = bmsManager.StageFile != null && !bmsManager.IsStarted;
        }
        if(gameEnded) {
            gameEnded = false;
            if(!Loader.listenMode) {
                panel.gameObject.SetActive(true);
                if(Loader.judgeMode != 2) {
                    resultText.text = "";
                    detailsPanel.gameObject.SetActive(true);
                    for(int i = 0; i < resultCountText.Length; i++)
                        resultCountText[i].text = bmsManager.GetNoteScoreCount(i).ToString("\\x0");
                    resultComboText.text = bmsManager.MaxCombos.ToString("\\x0");
                    resultScoreText.text = bmsManager.Score.ToString("0000000");
                    resultRankText.text = string.Format(
                        "<color=#{0}>{1}</color>",
                        ColorUtility.ToHtmlStringRGBA(bmsManager.RankColor),
                        bmsManager.RankString
                    );
                }
                bgTexture.rawImage.enabled = bgTexture.rawImage.texture != null;
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
                    detailsPanel.gameObject.SetActive(false);
                }
                if(!Loader.autoMode) {
                    recordsManager.CreateRecord(bmsManager);
                }
            }
            SetPauseButton();
        }
        if(gameStarted) {
            gameStarted = false;
            panel.gameObject.SetActive(false);
            pausePanel.gameObject.SetActive(false);
            dummyBGA.SetActive(bmsManager.BGAEnabled);
            bgTexture.rawImage.enabled = false;
            SetPauseButton();
        }
        if(pauseChanged) {
            pauseChanged = false;
            pausePanel.gameObject.SetActive(bmsManager.IsPaused);
            SetPauseButton();
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
            var anchorMax = durationBar.anchorMax;
            anchorMax.x = bmsManager.LoadResourceProgress;
            durationBar.anchorMax = anchorMax;
        } else if(bmsManager.IsStarted) {
            var anchorMax = durationBar.anchorMax;
            anchorMax.x = bmsManager.PercentageTimePassed;
            durationBar.anchorMax = anchorMax;
        }
        float deltaTime = Time.unscaledDeltaTime;
        if(fpsBar) UpdateVerticalBar(fpsBar, deltaTime * 10F);
        if(fpsText) fpsText.text = string.Format("{0:0} FPS", 1 / deltaTime);
        if(polyphonyBar) UpdateVerticalBar(polyphonyBar, bmsManager.Polyphony / 255F);
        if(polyphonyText) polyphonyText.text = string.Format("{0} POLY", bmsManager.Polyphony);
        if(accuracyBar) UpdateHorzBar(accuracyBar, bmsManager.Accuracy / 50F);
        if(accuracyText) accuracyText.text = string.Format("{0:+0000;-0000}MS", bmsManager.Accuracy);
        if(bpmText) bpmText.text = string.Format(
            bpmFormatText, bmsManager.BPM,
            ColorUtility.ToHtmlStringRGBA(
                Color.Lerp(bpmText.color, bpmLightColor, bpmLightLerp)
            ));
    }

    void SetPauseButton() {
        if(bmsManager && pauseButton)
            pauseButton.gameObject.SetActive(bmsManager.IsStarted && !bmsManager.IsPaused);
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

    void BeatFlow(float beat, float measure) {
        int beatIndex = Mathf.FloorToInt(measure);
        bpmLightLerp = 1 - beat;
        switch(Mathf.FloorToInt(measure) % 3) {
            case 0:
                if(beatIndex == 0)
                    bpmFormatText = "<color=#{1}>{0:0.0}</color>BPM";
                else
                    bpmFormatText = "{0:0.0}BP<color=#{1}>M</color>";
                break;
            case 1:
                bpmFormatText = "{0:0.0}<color=#{1}>B</color>PM";
                break;
            case 2:
                bpmFormatText = "{0:0.0}B<color=#{1}>P</color>M";
                break;
        }
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
