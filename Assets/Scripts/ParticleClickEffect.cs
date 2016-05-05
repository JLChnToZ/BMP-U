using UnityEngine;
using System.Collections;

public class ParticleClickEffect : MonoBehaviour {

    public Camera renderCamera;
    public ParticleSystem particles;

    void Update() {
        if(Input.touchSupported) {
            Touch touch;
            for(int i = 0, l = Input.touchCount; i < l; i++) {
                touch = Input.GetTouch(i);
                if(touch.phase == TouchPhase.Began)
                    particles.Emit(new ParticleSystem.EmitParams {
                        position = GetPosition(touch.position, 1)
                    }, 1);
            }
        } else if(Input.GetMouseButton(0)) {
            particles.Emit(new ParticleSystem.EmitParams {
                position = GetPosition(Input.mousePosition, 1)
            }, 1);
        }
        particles.subEmitters.birth0.startColor = Random.ColorHSV(0, 1, 1, 1, 1, 1);
    }

    Vector3 GetPosition(Vector3 original, float depth) {
        original.z = depth;
        return renderCamera.ScreenToWorldPoint(original);
    }
}
