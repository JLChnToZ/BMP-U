using UnityEngine;
using static UnityEngine.RectTransform;

namespace BananaBeats.UI {
    [RequireComponent(typeof(RectTransform))]
    public class Marquee: MonoBehaviour {
        private RectTransform rectTransform;
        public RectTransform target;
        public float speed;
        public bool autoCalculateStopPosition;
        public Vector2 stopPosition;
        public Axis direction;

        protected void Awake() {
            rectTransform = GetComponent<RectTransform>();
            if(autoCalculateStopPosition)
                stopPosition = target.anchoredPosition;
        }

        protected void Update() {
            float t = Time.unscaledDeltaTime;
            var localPos = target.localPosition;
            var anchoredPos = target.anchoredPosition;
            var pivot = target.pivot;
            var size = target.sizeDelta;
            var parentSize = rectTransform.sizeDelta;
            if(direction == Axis.Horizontal) {
                if(size.x > parentSize.x) {
                    if(speed < 0) {
                        if(localPos.x + pivot.x * size.x < -size.x)
                            anchoredPos.x += parentSize.x + size.x;
                    } else {
                        if(localPos.x + pivot.x * size.x > size.x)
                            anchoredPos.x -= parentSize.x + size.x;
                    }
                    anchoredPos.x += t * speed;
                } else {
                    anchoredPos = stopPosition;
                }
            } else {
                if(size.y > parentSize.y) {
                    if(speed < 0) {
                        if(localPos.y + pivot.y * size.y < -size.y)
                            anchoredPos.y += parentSize.y + size.y;
                    } else {
                        if(localPos.y + pivot.y * size.y > size.y)
                            anchoredPos.y -= parentSize.y + size.y;
                    }
                    anchoredPos.y += t * speed;
                } else {
                    anchoredPos = stopPosition;
                }
            }
            target.anchoredPosition = anchoredPos;
        }
    }
}
