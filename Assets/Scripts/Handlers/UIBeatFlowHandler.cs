using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class UIBeatFlowHandler: BeatFlowHandlerBase {
    Graphic graphic;
    Graphic Graphic {
        get {
            if(graphic == null)
                graphic = GetComponent<Graphic>();
            return graphic;
        }
    }

    protected override void ChangeColor(Color color) {
        Graphic.color = color;
    }
}