using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Dropdown))]
public class LanguageSelector: MonoBehaviour {
    [Serializable]
    struct LanguageOption {
        public string name;
        public string path;
    }

    [SerializeField]
    List<LanguageOption> availableLanguages;
    Dropdown dropDown;

    void Awake() {
        dropDown = GetComponent<Dropdown>();
        foreach(var option in availableLanguages)
            dropDown.options.Add(new Dropdown.OptionData(option.name));
        dropDown.value = availableLanguages.FindIndex(option => option.path == LanguageLoader.CurrentLang);
        dropDown.onValueChanged.AddListener(ValueChanged);
    }

    void ValueChanged(int index) {
        if(index < 0) return;
        LanguageLoader.LoadLang(availableLanguages[index].path);
    }
}
