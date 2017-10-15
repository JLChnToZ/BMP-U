using UnityEngine;
using BMS;
using System.Collections;
using System;

[RequireComponent(typeof(Camera))]
public class NotesCameraController: MonoBehaviour {
    private new Camera camera;
    public BMSManager bmsManager;

    bool started;

    private void Awake() {
        camera = GetComponent<Camera>();
    }

    private void Start() {
        bmsManager.OnGameStarted += GameStarted;
        bmsManager.OnGameEnded += GameEnded;
        bmsManager.OnPauseChanged += GameStateChange;
    }

    private void OnDestroy() {
        if(bmsManager != null) {
            bmsManager.OnGameStarted -= GameStarted;
            bmsManager.OnGameEnded -= GameEnded;
            bmsManager.OnPauseChanged -= GameStateChange;
        }
    }

    private void GameStarted() {
        camera.enabled = true;
    }

    private void GameEnded() {
        camera.enabled = false;
    }

    private void GameStateChange() {
        camera.enabled = bmsManager.IsStarted && !bmsManager.IsPaused;
    }
}
