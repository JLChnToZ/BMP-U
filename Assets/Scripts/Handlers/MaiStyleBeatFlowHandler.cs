using UnityEngine;
using System.Collections;
using BMS;
using System;

[RequireComponent(typeof(SpriteRenderer))]
public class MaiStyleBeatFlowHandler : MonoBehaviour {

    public BMSManager bmsManager;
    public Color color1;
    public Color color2;
    public AnimationCurve colorTransformCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public ColorToneHandler colorToneHandler;
    [SerializeField]
    float notStartedScale = 1.6F;
    
    bool gameStarted;
    Vector3 currentScale;

    SpriteRenderer _renderer;
    new SpriteRenderer renderer {
        get {
            if(_renderer == null)
                _renderer = GetComponent<SpriteRenderer>();
            return _renderer;
        }
    }

    void Start () {
        transform.localScale = currentScale = Vector3.one * notStartedScale;
        bmsManager.OnGameStarted += GameStarted;
        bmsManager.OnGameEnded += GameEnded;
        bmsManager.OnBeatFlow += BeatFlow;
	}

    void OnDestroy() {
        if(bmsManager != null) {
            bmsManager.OnBeatFlow -= BeatFlow;
            bmsManager.OnGameEnded -= GameEnded;
            bmsManager.OnBeatFlow -= BeatFlow;
        }
    }

    void GameStarted() {
        gameStarted = true;
    }

    void GameEnded() {
        gameStarted = false;
    }

    void BeatFlow(float beat, float measure) {
        renderer.color = Color.Lerp(color1, color2, colorTransformCurve.Evaluate(beat));
    }

    void Update() {
        currentScale = Vector3.Lerp(currentScale, Vector3.one * (gameStarted ? 1 : notStartedScale), Time.deltaTime * 10);
        transform.localScale = currentScale;
        if(!colorToneHandler) return;
        float a;
        var resultColor = colorToneHandler.ResultColor;
        a = color1.a;
        color1 = resultColor;
        color1.a = a;
        a = color2.a;
        color2 = resultColor;
        color2.a = a;
    }


}
