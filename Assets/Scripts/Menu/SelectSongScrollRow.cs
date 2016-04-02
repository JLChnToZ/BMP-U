using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SelectSongScrollRow : MonoBehaviour {
    public SongFileSelect[] items;

    RectTransform _rectTransform;
    public RectTransform rectTransform {
        get {
            if(_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();
            return _rectTransform;
        }
    }

}
