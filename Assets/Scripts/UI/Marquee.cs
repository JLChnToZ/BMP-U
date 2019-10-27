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
                stopPosition = target.localPosition;
        }

        protected void Update() {
            float t = Time.unscaledDeltaTime;
            var localPos = target.localPosition;
            var pivot = target.pivot;
            var size = target.sizeDelta;
            var parentSize = rectTransform.sizeDelta;
            if(direction == Axis.Horizontal) {
                if(size.x + stopPosition.x > parentSize.x) {
                    if(speed < 0) {
                        if(localPos.x + pivot.x * size.x < -size.x)
                            localPos.x += parentSize.x + size.x;
                    } else {
                        if(localPos.x + pivot.x * size.x > size.x)
                            localPos.x -= parentSize.x + size.x;
                    }
                    localPos.x += t * speed;
                } else {
                    localPos = stopPosition;
                }
            } else {
                if(size.y + stopPosition.y > parentSize.y) {
                    if(speed < 0) {
                        if(localPos.y + pivot.y * size.y < -size.y)
                            localPos.y += parentSize.y + size.y;
                    } else {
                        if(localPos.y + pivot.y * size.y > size.y)
                            localPos.y -= parentSize.y + size.y;
                    }
                    localPos.y += t * speed;
                } else {
                    localPos = stopPosition;
                }
            }
            target.localPosition = localPos;
        }
    }
}
