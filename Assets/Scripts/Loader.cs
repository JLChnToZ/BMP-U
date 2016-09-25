using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Collections;
using JLChnToZ.Toolset.Singleton;
using BMS;
using BMS.Visualization;

public class Loader : SingletonBehaviour<Loader> {
    public static string songPath;
    public static bool autoMode = false;
    public static bool enableBGA = true;
    public static bool enableDetune = true;
    public static int gameMode = 0;
    public static ColoringMode colorMode = ColoringMode.Timing;
    public static int judgeMode = 0;
    public static float speed = 1;

    public int sceneIndex;

    public BMSManager bmsManager;
    public NoteDetector noteDetector;
    public NoteSpawner[] noteSpaawners;

    public void LoadScene(string sceneName) {
        SceneManager.LoadScene(sceneName);
    }
    
	void Start () {
        switch(sceneIndex) {
            case 0: // Menu Scene
                break;
            case 1: // Game Scene
                StartCoroutine(LoadBMSCoroutine());
                break;
        }
	}

    IEnumerator LoadBMSCoroutine() {
        string bmsContent;
        var fileInfo = new FileInfo(songPath);
        using(var fs = fileInfo.OpenRead())
        using(var fsRead = new StreamReader(fs, SongInfoLoader.CurrentEncoding))
            bmsContent = fsRead.ReadToEnd();
        bmsManager.DetuneEnabled = enableDetune;
        bmsManager.BGAEnabled = enableBGA;
        bmsManager.PreEventOffset = TimeSpan.FromSeconds(2 - speed);
        bmsManager.LoadBMS(bmsContent, fileInfo.Directory.FullName);
        while(!bmsManager.BMSLoaded) yield return null;

        bmsManager.ReloadBMS(BMSReloadOperation.Body | BMSReloadOperation.ResourceHeader);
        while(!bmsManager.BMSLoaded) yield return null;
        bmsManager.ReloadBMS(BMSReloadOperation.Resources);
        while(bmsManager.IsLoadingResources) yield return null;
        bmsManager.InitializeNoteScore();

        noteDetector.autoMode = autoMode;
        foreach(var spawner in noteSpaawners)
            spawner.coloringMode = colorMode;
        bmsManager.TightMode = judgeMode == 1;
        if(!enableBGA)
            bmsManager.placeHolderTexture = Texture2D.whiteTexture;
        bmsManager.IsStarted = true;
    }
}
