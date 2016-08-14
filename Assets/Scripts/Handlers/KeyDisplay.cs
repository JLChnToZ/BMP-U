using UnityEngine;
using UnityEngine.UI;

using System.Collections;
using System.Collections.Generic;

using BMS;
using BMS.Visualization;

[RequireComponent(typeof(RectTransform))]
public class KeyDisplay : MonoBehaviour {

    public BMSManager bmsManager;
    public NoteSpawner noteSpawner;

    public GameObject prefab;

    public float fadeDelay;
    public float fadeSpeed;

    readonly HashSet<Text> instances = new HashSet<Text>();
    
    public float distance;

    Coroutine fadeCoroutine;
    
    void Start () {
        bmsManager.OnGameStarted += GameStarted;
        bmsManager.OnGameEnded += GameEnded;
	}

    void OnDestroy() {
        if(bmsManager) {
            bmsManager.OnGameStarted -= GameStarted;
            bmsManager.OnGameEnded -= GameEnded;
        }
        GameEnded();
    }

    void GameStarted() {
        if(!noteSpawner) return;
        var mappedChannels = noteSpawner.MappedChannels;
        float delta = 0.5F, angle = 0;
        var position = Vector3.zero;
        var keyMapping = NoteLayoutOptionsHandler.KeyMapping;
        KeyCode keyCode;
        float clampStart = 0, clampEnd = 0;
        NoteSpawnerSP noteSpawnerSP = noteSpawner as NoteSpawnerSP;
        if(noteSpawnerSP) {
            clampStart = noteSpawnerSP.clampRangeStart;
            clampEnd = noteSpawnerSP.clampRangeEnd;
            for(int i = 0, l = mappedChannels.Count; i < l; i++) {
                if(l > 1) delta = (float)i / (l - 1);
                angle = Mathf.Lerp(clampStart + 3, clampEnd - 3, delta) * Mathf.Deg2Rad;
                var go = Instantiate(prefab);
                position.x = Mathf.Cos(angle) * distance;
                position.y = Mathf.Sin(angle) * distance;
                go.transform.SetParent(transform, false);
                go.transform.localPosition = position;
                var text = go.GetComponent<Text>();
                text.text = keyMapping.TryGetValue(mappedChannels[i], out keyCode) ? keyCode.ToString() : string.Empty;
                instances.Add(text);
                go.SetActive(true);
            }
        }
        NoteSpawnerClassic noteSpawnerClassic = noteSpawner as NoteSpawnerClassic;
        if(noteSpawnerClassic) {
            clampStart = noteSpawnerClassic.clampRangeStart;
            clampEnd = noteSpawnerClassic.clampRangeEnd;
            for(int i = 0, l = mappedChannels.Count; i < l; i++) {
                if(l > 1) delta = (float)i / (l - 1);
                angle = Mathf.Lerp(clampStart, clampEnd, delta);
                var go = Instantiate(prefab);
                position.x = Mathf.Lerp(clampStart, clampEnd, delta) * 215;
                position.y = distance;
                go.transform.SetParent(transform, false);
                go.transform.localPosition = position;
                var text = go.GetComponent<Text>();
                text.text = keyMapping.TryGetValue(mappedChannels[i], out keyCode) ? keyCode.ToString() : string.Empty;
                instances.Add(text);
                go.SetActive(true);
            }
        }

        fadeCoroutine = StartCoroutine(FadeOutCoroutine());
    }

    IEnumerator FadeOutCoroutine() {
        Color color;
        float alpha = 1;
        yield return new WaitForSeconds(fadeDelay);
        while(alpha > 0.001F) {
            alpha = Mathf.Lerp(alpha, 0, Time.deltaTime * fadeSpeed);
            foreach(var text in instances) {
                color = text.color;
                color.a = alpha;
                text.color = color;
            }
            yield return null;
        }
        foreach(var text in instances)
            text.gameObject.SetActive(false);
        fadeCoroutine = null;
        yield break;
    }

    void GameEnded() {
        if(fadeCoroutine != null) {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        foreach(var instance in instances)
            Destroy(instance.gameObject);
        instances.Clear();
    }

}
