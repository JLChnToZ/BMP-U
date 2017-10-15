using UnityEngine;
using BMS;

public abstract class BeatFlowHandlerBase: MonoBehaviour {
    public BMSManager bmsManager;
    public Color color1;
    public Color color2;
    public AnimationCurve colorTransformCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public ColorToneHandler colorToneHandler;
    [SerializeField]
    bool useMeasureFlow = false;

    protected bool gameStarted;

    protected virtual void Start() {
        bmsManager.OnGameStarted += GameStarted;
        bmsManager.OnGameEnded += GameEnded;
        bmsManager.OnBeatFlow += BeatFlow;
    }

    protected virtual void OnDestroy() {
        if(bmsManager != null) {
            bmsManager.OnGameStarted -= GameStarted;
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
        if(useMeasureFlow)
            ChangeColor(Color.Lerp(color1, color2, colorTransformCurve.Evaluate(measure / bmsManager.TimeSignature)));
        else
            ChangeColor(Color.Lerp(color1, color2, colorTransformCurve.Evaluate(beat)));
    }

    protected abstract void ChangeColor(Color color);

    protected virtual void Update() {
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
