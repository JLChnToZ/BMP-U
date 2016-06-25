using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class LayoutElementAdapter: MonoBehaviour, ILayoutElement {

    public RectTransform child;

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
        get { return 0; }
    }

    public float minWidth {
        get { return 0; }
    }

    public float preferredHeight {
        get { return child ? child.rect.height : 0; }
    }

    public float preferredWidth {
        get { return child ? child.rect.width : 0; }
    }

    public void CalculateLayoutInputHorizontal() {
    }

    public void CalculateLayoutInputVertical() {
    }
}
