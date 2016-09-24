using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage)), RequireComponent(typeof(AspectRatioFitter))]
public class RawImageFitter: MonoBehaviour {
    RawImage _rawImage;
    AspectRatioFitter _aspectRatioFitter;

    public RawImage rawImage {
        get { return _rawImage; }
    }

    public AspectRatioFitter aspectRatioFitter {
        get { return _aspectRatioFitter; }
    }

    void Awake() {
        _rawImage = GetComponent<RawImage>();
        _aspectRatioFitter = GetComponent<AspectRatioFitter>();
    }

    public void SetTexture(Texture texture) {
        _rawImage.texture = texture;
        UpdateAspectRatio();
    }

    public void UpdateAspectRatio() {
        Texture texture = _rawImage.texture;
        if(texture)
            _aspectRatioFitter.aspectRatio = (float)texture.width / texture.height;
    }
}
