using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class Marquee: MonoBehaviour {
    private RectTransform rectTransform;
    private RectTransform parentTransform;
    private bool enableX, enableY;
    public Vector2 speed = new Vector2(-100, 0);

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

    private void CheckSize() {
        if(parentTransform == null) return;
        Rect rect = rectTransform.rect;
        Rect parentRect = parentTransform.rect;
        Vector2 currentPos = rectTransform.anchoredPosition;
        enableX = rect.width > parentRect.width;
        enableY = rect.height > parentRect.height;
        if(!enableX && !Mathf.Approximately(speed.x, 0))
            currentPos.x = 0;
        if(!enableY && !Mathf.Approximately(speed.y, 0))
            currentPos.y = 0;
        rectTransform.anchoredPosition = currentPos;
    }
    
    protected void Update() {
        if(parentTransform == null) return;
        float delta = Time.unscaledDeltaTime;
        Rect rect = rectTransform.rect;
        Rect parentRect = parentTransform.rect;
        Vector2 currentPos = rectTransform.anchoredPosition;
        if(enableX && !Mathf.Approximately(speed.x, 0)) {
            currentPos.x += speed.x * delta;
            if(speed.x < 0 && currentPos.x < -rect.width)
                currentPos.x = parentRect.width;
            else if(speed.x > 0 && currentPos.x > parentRect.width)
                currentPos.x = -rect.width;
        }
        if(enableY && !Mathf.Approximately(speed.y, 0)) {
            currentPos.y += speed.y * delta;
            if(speed.y < 0 && currentPos.y < -rect.height)
                currentPos.y = parentRect.height;
            else if(speed.y > 0 && currentPos.y > parentRect.height)
                currentPos.y = -rect.height;
        }
        rectTransform.anchoredPosition = currentPos;
    }
}
