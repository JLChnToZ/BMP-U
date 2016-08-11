using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class CameraScaler : MonoBehaviour {

    Camera _camera;
    new Camera camera {
        get {
            if(_camera == null)
                _camera = GetComponent<Camera>();
            return _camera;
        }
    }

    public float referenceScale = 1;
    public BMS.Visualization.BMSTextureDisplay[] displays;
	
	void LateUpdate () {
        CalculateMaxScale();
        float scale = (float)Screen.height / Screen.width;
        camera.orthographicSize = scale * referenceScale;
	}

    void CalculateMaxScale() {
        if(displays == null || displays.Length < 1) return;
        float maxScale = float.PositiveInfinity;
        foreach(var display in displays) {
            if(!display.isActiveAndEnabled) continue;
            Vector2 scale = display.transform.localScale;
            maxScale = Mathf.Min(maxScale, 1 / scale.x);
        }
        if(maxScale == float.PositiveInfinity) return;
        referenceScale = maxScale / 2F;
    }
}
