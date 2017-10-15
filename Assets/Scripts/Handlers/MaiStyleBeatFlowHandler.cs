using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class MaiStyleBeatFlowHandler : BeatFlowHandlerBase {
    [SerializeField]
    float notStartedScale = 1.6F;
    Vector3 currentScale;

    SpriteRenderer _renderer;
    new SpriteRenderer renderer {
        get {
            if(_renderer == null)
                _renderer = GetComponent<SpriteRenderer>();
            return _renderer;
        }
    }

    protected override void Start () {
        base.Start();
        transform.localScale = currentScale = Vector3.one * notStartedScale;
	}
    

    protected override void ChangeColor(Color color) {
        renderer.color = color;
    }

    protected override void Update() {
        base.Update();
        currentScale = Vector3.Lerp(currentScale, Vector3.one * (gameStarted ? 1 : notStartedScale), Time.deltaTime * 10);
        transform.localScale = currentScale;
    }
}
