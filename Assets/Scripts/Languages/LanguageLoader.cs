using UnityEngine;
using System;

public class LanguageLoader : MonoBehaviour {
    static string currentLang;
    static string[] langText;
    static Font font;
    static bool initialzed = false;
    public static event Action OnLanguageChange;

    void Start() {
        Init();
    }

    static void Init() {
        if(!initialzed)
            LoadLang(PlayerPrefs.GetString("lang", "default"));
    }

    public static void LoadLang(string lang) {
        if(string.IsNullOrEmpty(lang) || currentLang == lang) return;
        var langPack = Resources.Load<TextAsset>(string.Format("Lang/{0}", lang));
        langText = langPack.text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        int count = langText.Length;
        if(count > 1) {
            font = Resources.Load<Font>(string.Format("Fonts/{0}", langText[0]));
            for(int i = 1; i < count; i++)
                langText[i] = langText[i].Replace("\\n", "\n").Replace("\\t", "\t");
        }
        Resources.UnloadAsset(langPack);
        PlayerPrefs.SetString("lang", lang);
        currentLang = lang;
        initialzed = true;
        if(OnLanguageChange != null)
            OnLanguageChange.Invoke();
    }

    public static string GetText(int langId) {
        Init();
        if(langText != null && langId >= 0 && langId < langText.Length)
            return langText[langId];
        return string.Empty;
    }

    public static Font currentFont {
        get {
            Init();
            return font;
        }
    }

    public static string CurrentLang {
        get {
            Init();
            return currentLang;
        }
    }
}
