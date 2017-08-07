using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class Footer : MonoBehaviour {

    private Text text;

    void Awake() {
        text = GetComponent<Text>();
        text.text = string.Format(text.text, Application.productName, Application.version);
    }
}
