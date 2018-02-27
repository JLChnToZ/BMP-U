using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using BMS;

using System.Collections;

public class SelectSongManager: MonoBehaviour {
    static int savedSortMode;
    
    public RectTransform loadingDisplay;
    public RectTransform loadingPercentageDisplay;
    public RectTransform optionsPanel;
    public RectTransform detailsPanel;
    public Button optionsButton;
    public Button optionsBackButton;
    public Dropdown gameMode;
    public Dropdown colorMode;
    public Toggle autoModeToggle;
    public Toggle detuneToggle;
    public Toggle bgaToggle;
    public Toggle dynamicSpeedToggle;
    public Dropdown judgeModeDropDown;
    public Slider speedSlider;
    public Slider notesLimitSlider;
    public Dropdown sortMode;
    public RawImage background;
    public ColorRampLevel colorSet;
    public Button startGameButton;

    [SerializeField]
    NoteLayoutOptionsHandler layoutOptionsHandler;
    [SerializeField]
    SelectSongScrollView selectSongScrollView;
    [SerializeField]
    SongInfoDetails detailsDisplay;

    public BMSManager bmsManager;

    SongInfo? currentInfo;

    void Awake() {
        if(!bmsManager) bmsManager = GetComponent<BMSManager>();
        if(!bmsManager) bmsManager = gameObject.AddComponent<BMSManager>();
        SongInfoLoader.SetBMSManager(bmsManager);

        gameMode.value = Loader.gameMode;
        gameMode.onValueChanged.AddListener(GameModeChange);
        colorMode.value = (int)Loader.colorMode;
        colorMode.onValueChanged.AddListener(ColorModeChange);
        autoModeToggle.isOn = Loader.autoMode;
        autoModeToggle.onValueChanged.AddListener(ToggleAuto);
        detuneToggle.isOn = Loader.enableDetune;
        detuneToggle.onValueChanged.AddListener(ToggleDetune);
        bgaToggle.isOn = Loader.enableBGA;
        bgaToggle.onValueChanged.AddListener(ToggleBGA);
        dynamicSpeedToggle.isOn = Loader.dynamicSpeed;
        dynamicSpeedToggle.onValueChanged.AddListener(ToggleDynamicSpeed);
        judgeModeDropDown.value = Loader.judgeMode;
        judgeModeDropDown.onValueChanged.AddListener(JudgeModeChange);
        speedSlider.value = Loader.speed;
        speedSlider.onValueChanged.AddListener(ChangeSpeed);
        notesLimitSlider.value = Loader.noteLimit;
        notesLimitSlider.onValueChanged.AddListener(ChangeNoteLimit);
        sortMode.value = savedSortMode;
        sortMode.onValueChanged.AddListener(ChangeSortMode);
        startGameButton.onClick.AddListener(StartGame);
        optionsButton.onClick.AddListener(ShowOptions);
        optionsBackButton.onClick.AddListener(HideOptions);

        currentInfo = SongInfoLoader.SelectedSong;
        SongInfoLoader.OnStartLoading += OnLoadingChanged;
        SongInfoLoader.OnListUpdated += OnLoadingChanged;
        SongInfoLoader.OnSelectionChanged += SelectionChanged;
        LanguageLoader.OnLanguageChange += LangChange;
        OnLoadingChanged();
    }

    void Start() {
        SongInfoLoader.CurrentCodePage = 932; // Hardcoded to Shift-JIS as most of BMS are encoded by this.
        SongInfoLoader.ReloadDirectory();
        InternalHideOptions();
    }

    void OnDestroy() {
        SongInfoLoader.OnStartLoading -= OnLoadingChanged;
        SongInfoLoader.OnListUpdated -= OnLoadingChanged;
        SongInfoLoader.OnSelectionChanged -= SelectionChanged;
        LanguageLoader.OnLanguageChange -= LangChange;
    }

    void OnLoadingChanged() {
        loadingDisplay.gameObject.SetActive(!SongInfoLoader.IsReady);
    }

    void SelectionChanged(SongInfo? newInfo) {
        bool changed = false;
        if(currentInfo.HasValue != newInfo.HasValue)
            changed = true;
        else if(newInfo.HasValue && currentInfo.Value.filePath != newInfo.Value.filePath)
            changed = true;
        if(changed) {
            HideOptions();
        }
        currentInfo = newInfo;
    }

    public void GameModeChange(int index) {
        Loader.gameMode = index;
    }

    public void ColorModeChange(int index) {
        Loader.colorMode = (BMS.Visualization.ColoringMode)index;
    }

    public void ToggleAuto(bool state) {
        Loader.autoMode = autoModeToggle.isOn;
    }

    public void ToggleBGA(bool state) {
        Loader.enableBGA = bgaToggle.isOn;
    }

    public void ToggleDetune(bool state) {
        Loader.enableDetune = detuneToggle.isOn;
    }

    public void ToggleDynamicSpeed(bool state) {
        Loader.dynamicSpeed = dynamicSpeedToggle.isOn;
    }

    public void JudgeModeChange(int index) {
        Loader.judgeMode = index;
    }

    public void ChangeSpeed(float value) {
        Loader.speed = speedSlider.value;
    }

    public void ChangeNoteLimit(float value) {
        Loader.noteLimit = notesLimitSlider.value;
    }

    public void ChangeSortMode(int mode) {
        savedSortMode = mode;
        switch(mode) {
            case 0: SongInfoLoader.CurrentSortMode = SongInfoComparer.SortMode.Name; break;
            case 1: SongInfoLoader.CurrentSortMode = SongInfoComparer.SortMode.Artist; break;
            case 2: SongInfoLoader.CurrentSortMode = SongInfoComparer.SortMode.Genre; break;
            case 3: SongInfoLoader.CurrentSortMode = SongInfoComparer.SortMode.Level; break;
            case 4: SongInfoLoader.CurrentSortMode = SongInfoComparer.SortMode.BPM; break;
        }
    }

    public void StartGame() {
        HideOptions();
        if(string.IsNullOrEmpty(Loader.songPath)) {
            SongInfoLoader.OnRecursiveLoaded += StartListen;
            SongInfoLoader.RecursiveLoadDirectory();
            return;
        }
        switch(Loader.gameMode) {
            case 0: SceneManager.LoadScene("GameScene"); break;
            case 1: SceneManager.LoadScene("ClassicGameScene"); break;
        }
    }

    public void StartListen() {
        SongInfoLoader.OnRecursiveLoaded -= StartListen;
        SceneManager.LoadScene("ListenScene");
    }

    public void ShowOptions() {
        detailsPanel.gameObject.SetActive(false);
        optionsPanel.gameObject.SetActive(true);
        LangChange();
    }

    void LangChange() {
        StartCoroutine(DelayUpdateOptionsLayout());
    }

    IEnumerator DelayUpdateOptionsLayout() {
        RectTransform content = optionsPanel.GetComponent<ScrollRect>().content;
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    public void HideOptions() {
        layoutOptionsHandler.Apply();
        InternalHideOptions();
        selectSongScrollView.RefreshDisplay();
        detailsDisplay.ReloadRecord();
    }

    void InternalHideOptions() {
        detailsPanel.gameObject.SetActive(true);
        optionsPanel.gameObject.SetActive(false);
    }
}
