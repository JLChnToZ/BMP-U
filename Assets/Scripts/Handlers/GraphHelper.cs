using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using BMS;

public class GraphHelper: MonoBehaviour {
    public BMSManager bmsManager;
    public Vector2 size;
    public Color defaultColor;
    public Color[] colorSet;
    Texture2D graphTexture;

    public Texture2D Texture {
        get { return graphTexture; }
    }

    void Awake() {
        bmsManager.OnGameStarted += GameStarted;
        bmsManager.OnGameEnded += GameEnded;
        bmsManager.OnNoteClicked += NoteClicked;
    }

    void OnDestroy() {
        if(bmsManager) {
            bmsManager.OnGameStarted -= GameStarted;
            bmsManager.OnGameEnded -= GameEnded;
            bmsManager.OnNoteClicked -= NoteClicked;
        }
        if(graphTexture) {
            Destroy(graphTexture);
            graphTexture = null;
        }
    }

    void GameStarted() {
        var oldGraph = graphTexture;
        graphTexture = new Texture2D(Mathf.FloorToInt(size.x), Mathf.FloorToInt(size.y), TextureFormat.ARGB32, false) {
            wrapMode = TextureWrapMode.Clamp
        };
        var transparentColor = new Color(1, 1, 1, 0);
        var pixels = graphTexture.GetPixels();
        for(int i = 0; i < pixels.Length; i++)
            pixels[i] = transparentColor;
        graphTexture.SetPixels(pixels);
        graphTexture.Apply();
        Destroy(oldGraph);
    }

    void GameEnded() {
        graphTexture.Apply();
    }

    void NoteClicked(TimeSpan expectedTimePosition, TimeSpan currentTimePosition, int channel, int eventId, int resultFlag) {
        if(!graphTexture || resultFlag < 0 || resultFlag >= colorSet.Length)
            return;
        int cx = Mathf.Clamp(Mathf.RoundToInt((float)expectedTimePosition.Ticks / bmsManager.Duration.Ticks * size.x), 0, graphTexture.width);
        int cy = Mathf.Clamp(Mathf.RoundToInt(((float)(currentTimePosition - expectedTimePosition).TotalSeconds * 2.5F + 0.5F) * size.y), 0, graphTexture.height);
        graphTexture.SetPixel(cx, cy, Color.Lerp(graphTexture.GetPixel(cx, cy), colorSet[resultFlag], 0.5F));
    }

}
