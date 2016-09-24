using UnityEngine;
using UnityEngine.UI;

using FitMode = UnityEngine.UI.ContentSizeFitter.FitMode;

public class LayoutElementAdapter: MonoBehaviour, ILayoutElement {
    public RectTransform child;
    public FitMode restrictWidth = FitMode.PreferredSize;
    public FitMode restrictHeight = FitMode.PreferredSize;
    
    public float flexibleHeight {
        get { return 0; }
    }

    public float flexibleWidth {
        get { return 0; }
    }

    public int layoutPriority {
        get { return 0; }
    }

    public float minHeight {
        get { return restrictHeight == FitMode.MinSize && child ? child.rect.height : -1; }
    }

    public float minWidth {
        get { return restrictWidth == FitMode.MinSize && child ? child.rect.width : -1; }
    }

    public float preferredHeight {
        get { return restrictHeight == FitMode.PreferredSize && child ? child.rect.height : -1; }
    }

    public float preferredWidth {
        get { return restrictWidth == FitMode.PreferredSize && child ? child.rect.width : -1; }
    }

    public void CalculateLayoutInputHorizontal() {
    }

    public void CalculateLayoutInputVertical() {
    }
}
