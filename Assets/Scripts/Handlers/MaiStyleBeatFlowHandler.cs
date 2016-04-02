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

    SpriteRenderer _renderer;
    new SpriteRenderer renderer {
        get {
            if(_renderer == null)
                _renderer = GetComponent<SpriteRenderer>();
            return _renderer;
        }
    }

    void Start () {
        bmsManager.OnBeatFlow += BeatFlow;
	}

    void OnDestroy() {
        if(bmsManager != null)
            bmsManager.OnBeatFlow -= BeatFlow;
    }

    void BeatFlow(float beat, float measure) {
        renderer.color = Color.Lerp(color1, color2, colorTransformCurve.Evaluate(beat));
    }


}
