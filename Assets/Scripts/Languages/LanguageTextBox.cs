using UnityEngine;
using UnityEngine.UI;

public class LanguageTextBox : Text {
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

    public void SetText(int langId, params object[] args) {
        var text = LanguageLoader.GetText(langId);
        if(args != null && args.Length > 0 && !string.IsNullOrEmpty(text))
            text = string.Format(text, args);
        this.langId = langId;
        this.text = text;
        font = LanguageLoader.currentFont;
        SetAllDirty();
        RectTransform parent = rectTransform.parent as RectTransform;
        if(parent) LayoutRebuilder.MarkLayoutForRebuild(parent);
    }
}
