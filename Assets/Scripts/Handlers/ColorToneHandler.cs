using UnityEngine;
using System.Collections;
using System.Threading;
using ThreadPriority = System.Threading.ThreadPriority;

public class ColorToneHandler : MonoBehaviour {
    public RenderTexture singleColorInput;
    public float singleColorLerpDelta;

    Texture2D singleColorInputClone;
    float currentHue = 0, targetHue = 0;
    Color targetColor = Color.white, resultColor = Color.white;

    Thread updateThread;
    Color[] pixels;
    Vector2 inputSize;

    public Color ResultColor {
        get { return resultColor; }
    }

    void Start () {
        inputSize = new Vector2(singleColorInput.width, singleColorInput.height);
        singleColorInputClone = new Texture2D(singleColorInput.width, singleColorInput.height);
        updateThread = new Thread(UpdateInThread) {
            Priority = ThreadPriority.Lowest
        };
        updateThread.Start();
        StartCoroutine(UpdateCoroutine());
    }

    void OnDestroy() {
        updateThread.Abort();
    }

    void UpdateInThread() {
        Vector4 c;
        float h, s, v, i;
        while(true) {
            Thread.Sleep(25);
            if(pixels == null) continue;
            i = 0;
            c = Vector4.zero;
            foreach(var px in pixels) {
                Color.RGBToHSV(px, out h, out s, out v);
                if(s > 0.1F && v > 0.1F) {
                    c += (Vector4)px;
                    i++;
                }
            }
            if(i == 0) continue;
            c /= i;
            Color.RGBToHSV(c, out h, out s, out v);
            if(s > 0.1F) targetColor = Color.HSVToRGB(h, s / 2 + 0.5F, 1);
        }
    }

    IEnumerator UpdateCoroutine() {
        while(true) {
            RenderTexture.active = singleColorInput;
            singleColorInputClone.ReadPixels(new Rect(Vector2.zero, inputSize), 0, 0, true);
            RenderTexture.active = null;
            pixels = singleColorInputClone.GetPixels();
            yield return new WaitForSeconds(0.1F);
        }
    }
	
	void Update () {
        resultColor = Color.Lerp(resultColor, targetColor, Time.deltaTime * singleColorLerpDelta);
    }
}
