using UnityEngine;
using UnityEngine.UI;

public class LanguageDropDown:Dropdown {
    [SerializeField]
    int langId = -1;

    public int LangId {
        get { return langId; }
        set { SetText(value); }
    }

    protected override void Awake() {
        base.Awake();
        if(!Application.isPlaying) return;
        if(langId >= 0) SetText(langId);
        LanguageLoader.OnLanguageChange += ChangeLang;
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        LanguageLoader.OnLanguageChange -= ChangeLang;
    }

    void ChangeLang() {
        if(langId >= 0)
            SetText(langId);
    }

    public void SetText(int langId) {
        var text = LanguageLoader.GetText(langId).Split('\n');
        for(int i = 0, c = Mathf.Min(text.Length, options.Count); i < c; i++)
            options[i].text = text[i];
        if(value >= 0 && value < options.Count)
            captionText.text = options[value].text;
    }
}
