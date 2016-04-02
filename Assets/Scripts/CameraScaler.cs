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
	
	void LateUpdate () {
        float scale = (float)Screen.height / Screen.width;
        camera.orthographicSize = scale * referenceScale;
	}
}
