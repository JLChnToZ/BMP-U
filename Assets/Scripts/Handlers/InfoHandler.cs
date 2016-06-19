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

    public RectTransform panel;
    public Text resultText;

    public RectTransform pausePanel;

    int displayingScore, displayCombos, targetCombos;
    float combosValue;

    bool bmsLoaded, stageFileLoaded, gameStarted, gameEnded, pauseChanged, startOnLoad;

    [SerializeField, Multiline]
    string resultFormat = "";
    
    void Start () {
        if(bmsManager) {
            bmsManager.OnBMSLoaded += OnBMSLoaded;
            bmsManager.OnStageFileLoaded += OnStageFileLoaded;
            bmsManager.OnGameStarted += OnGameStarted;
            bmsManager.OnGameEnded += OnGameEnded;
            bmsManager.OnPauseChanged += OnPauseChanged;
        }
        if(graphDisplay) {
            if(graphHandler)
                graphHandler.size = graphDisplay.rectTransform.rect.size;
            graphDisplay.enabled = false;
        }
    }

    void OnDestroy() {
        if(bmsManager) {
            bmsManager.OnBMSLoaded -= OnBMSLoaded;
            bmsManager.OnStageFileLoaded -= OnStageFileLoaded;
            bmsManager.OnGameStarted -= OnGameStarted;
            bmsManager.OnGameEnded -= OnGameEnded;
            bmsManager.OnPauseChanged -= OnPauseChanged;
        }
    }

    void Update() {
        if(bmsLoaded) {
            bmsLoaded = false;
            infoDisplay.text = string.Format("{0} - {1}\nLevel: {2}", bmsManager.Title, bmsManager.Artist, bmsManager.PlayLevel);
            infoDisplay2.text = string.Format("{0}\n{1}", bmsManager.SubArtist, bmsManager.Comments);
            if(startOnLoad) {
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
            resultText.text = string.Format(resultFormat,
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
        }
        if(gameStarted) {
            gameStarted = false;
            panel.gameObject.SetActive(false);
            pausePanel.gameObject.SetActive(false);
            bgTexture.enabled = false;
        }
        if(pauseChanged) {
            pauseChanged = false;
            pausePanel.gameObject.SetActive(bmsManager.IsPaused);
        }
        float t = Time.deltaTime * 10;
        if(bmsManager.Score < displayingScore)
            displayingScore = bmsManager.Score;
        displayingScore = Mathf.CeilToInt(Mathf.Lerp(displayingScore, bmsManager.Score, t));
        scoreDisplay.text = displayingScore.ToString("0000000");

        if(targetCombos != bmsManager.Combos) {
            combosValue = Mathf.Clamp(combosValue + 0.5F * (bmsManager.Combos - targetCombos), 0, 2.25F);
            targetCombos = bmsManager.Combos;
        } else {
            combosValue = Mathf.Lerp(combosValue, 0, t / 4);
        }
        if(displayCombos < targetCombos)
            displayCombos = targetCombos;
        displayCombos = Mathf.FloorToInt(Mathf.Lerp(displayCombos, bmsManager.Combos, t));
        comboDisplay.text = (bmsManager.IsStarted && displayCombos >= 3) ? displayCombos.ToString() : "";
        comboDisplay.transform.localScale = Vector3.one * (1 + combosValue / 1.5F);
        if(bmsManager.IsLoadingResources) {
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
        if(debugInfoDisplay)
            debugInfoDisplay.text = string.Format("{0:0.0}BPM \t{1}POLY \tACCU.{2:0.0}MS \t{3:0.0}FPS", bmsManager.BPM, bmsManager.Polyphony, bmsManager.Accuracy, 1 / Time.unscaledDeltaTime);
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
