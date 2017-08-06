using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class Marquee: MonoBehaviour {
    private RectTransform rectTransform;
    private RectTransform parentTransform;
    private bool enableX, enableY;
    public Vector2 speed = new Vector2(-100, 0);
    public Vector2 scrollInSpeedMultiply = new Vector2(2, 2);
    public Vector2 defaultPosition = new Vector2(0, 0);

    protected void Awake() {
        rectTransform = GetComponent<RectTransform>();
        OnTransformParentChanged();
        CheckSize();
    }

    protected void OnRectTransformDimensionsChange() {
        CheckSize();
    }

    protected void OnTransformParentChanged() {
        parentTransform = rectTransform.parent as RectTransform;
    }

    public void CheckSize() {
        if(parentTransform == null) return;
        Rect rect = rectTransform.rect;
        Rect parentRect = parentTransform.rect;
        Vector2 currentPos = rectTransform.anchoredPosition;
        enableX = rect.width > parentRect.width;
        enableY = rect.height > parentRect.height;
        if(!enableX && !Mathf.Approximately(speed.x, 0))
            currentPos.x = defaultPosition.x;
        if(!enableY && !Mathf.Approximately(speed.y, 0))
            currentPos.y = defaultPosition.y;
        rectTransform.anchoredPosition = currentPos;
    }
    
    protected void Update() {
        if(parentTransform == null) return;
        float delta = Time.unscaledDeltaTime;
        Rect rect = rectTransform.rect;
        Vector2 parentSize = parentTransform.rect.size;
        Vector2 parentPivot = parentTransform.pivot;
        Vector3 position = rectTransform.localPosition;
        if(enableX && !Mathf.Approximately(speed.x, 0)) {
            float pivotPos = parentSize.x * parentPivot.x;
            if(speed.x < 0) {
                if(position.x + rect.xMax < -pivotPos)
                    position.x += rect.width + parentSize.x;
                position.x += speed.x * delta * Mathf.Lerp(1, scrollInSpeedMultiply.x, (position.x + rect.xMin) / pivotPos + 1);
            } else {
                if(position.x + rect.xMin > pivotPos)
                    position.x -= rect.width + parentSize.x;
                position.x += speed.x * delta * Mathf.Lerp(1, scrollInSpeedMultiply.x, (position.x + rect.xMax) / -pivotPos + 1);
            }
        }
        if(enableY && !Mathf.Approximately(speed.y, 0)) {
            float pivotPos = parentSize.y * parentPivot.y;
            if(speed.y < 0) {
                if(position.y + rect.yMax < -pivotPos)
                    position.y += rect.width + parentSize.y;
                position.y += speed.y * delta * Mathf.Lerp(1, scrollInSpeedMultiply.y, (position.y + rect.yMin) / pivotPos + 1);
            } else {
                if(position.y + rect.yMin > pivotPos)
                    position.y -= rect.width + parentSize.y;
                position.y += speed.y * delta * Mathf.Lerp(1, scrollInSpeedMultiply.y, (position.y + rect.yMax) / -pivotPos + 1);
            }
        }
        rectTransform.localPosition = position;
    }
}
