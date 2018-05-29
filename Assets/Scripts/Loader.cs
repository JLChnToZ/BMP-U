using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Collections;
using JLChnToZ.Toolset.Singleton;
using BMS;
using BMS.Visualization;

using UnityRandom = UnityEngine.Random;

public class Loader : SingletonBehaviour<Loader> {
    public static string songPath;
    public static string[] songPaths;
    public static bool listenMode = false;
    public static bool autoMode = false;
    public static bool enableBGA = true;
    public static bool enableDetune = true;
    public static bool dynamicSpeed = true;
    public static int gameMode = 0;
    public static ColoringMode colorMode = ColoringMode.Timing;
    public static int judgeMode = 0;
    public static float noteLimit = 3;
    public static float speed = 1;

    public int sceneIndex;

    public BMSManager bmsManager;
    public NoteDetector noteDetector;
    public NoteSpawner[] noteSpaawners;

    private bool isFirstRun = true;

    public void LoadScene(string sceneName) {
        SceneManager.LoadScene(sceneName);
    }
    
	void Start() {
        switch(sceneIndex) {
            case 0: // Menu Scene
                listenMode = false;
                break;
            case 1: // Game Scene
                listenMode = false;
                StartCoroutine(LoadBMSCoroutine(songPath));
                break;
            case 2: // Listen Scene
                listenMode = true;
                LoadRandomSong();
                break;
        }
	}

    protected override void OnDestroy() {
        base.OnDestroy();
        if(bmsManager != null) {
            bmsManager.OnGameEnded -= LoadRandomSong;
        }
    }

    IEnumerator LoadBMSCoroutine(string songPath) {
        bmsManager.IsStarted = false;
        FileInfo fileInfo = new FileInfo(songPath);
        string bmsContent = SongInfoLoader.LoadFile(fileInfo);
        bmsManager.NoteLimit = noteLimit > 0 ? Mathf.FloorToInt(Mathf.Pow(2, 3 + noteLimit)) : 0;
        bmsManager.DetuneEnabled = enableDetune;
        bmsManager.BGAEnabled = enableBGA;
        bmsManager.DynamicPreEventOffset = dynamicSpeed;
        bmsManager.PreEventOffset = TimeSpan.FromSeconds((2 - speed) * 2);
        bmsManager.LoadBMS(bmsContent, fileInfo.Directory.FullName, fileInfo.Extension);
        while(!bmsManager.BMSLoaded) yield return null;

        bmsManager.ReloadBMS(BMSReloadOperation.Body | BMSReloadOperation.ResourceHeader);
        while(!bmsManager.BMSLoaded) yield return null;
        bmsManager.ReloadBMS(BMSReloadOperation.Resources);
        while(bmsManager.IsLoadingResources) yield return null;
        bmsManager.InitializeNoteScore();

        if(noteDetector != null)
            noteDetector.autoMode = autoMode;
        if(noteSpaawners != null)
            foreach(var spawner in noteSpaawners)
                spawner.coloringMode = colorMode;
        bmsManager.TightMode = judgeMode == 1;
        if(!enableBGA)
            bmsManager.placeHolderTexture = Texture2D.whiteTexture;
        bmsManager.IsStarted = true;
    }

    private void LoadRandomSong() {
        if(songPaths == null || songPaths.Length < 1) {
            SceneManager.LoadScene("MenuScene");
            return;
        }
        StartCoroutine(LoadRandomSongDeferred());
    }

    private IEnumerator LoadRandomSongDeferred() {
        yield return null;
        StartCoroutine(LoadBMSCoroutine(songPaths[UnityRandom.Range(0, songPaths.Length)]));
        yield return null;
        if(isFirstRun && bmsManager != null)
            bmsManager.OnGameEnded += LoadRandomSong;
        isFirstRun = false;
    }
}
