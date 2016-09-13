using System;
using System.Collections.Generic;

using UnityEngine;
using JLChnToZ.Toolset.Singleton;

public class MovieTexturePlayer: SingletonBehaviour<MovieTexturePlayer> {
    readonly HashSet<MovieTextureHolder> movieTextureHolders = new HashSet<MovieTextureHolder>();
    DateTime prevDateTime;

    void Start() {
        prevDateTime = DateTime.UtcNow;
    }

    void Update() {
        DateTime currentDateTime = DateTime.UtcNow;
        long timeDiff = (currentDateTime - prevDateTime).Ticks;
        foreach(var mt in movieTextureHolders)
            if(mt) mt.ReadFrame(timeDiff);
        prevDateTime = currentDateTime;
    }

    public void Register(MovieTextureHolder mt) {
        if(mt) movieTextureHolders.Add(mt);
    }

    public void Unregister(MovieTextureHolder mt) {
        movieTextureHolders.Remove(mt);
    }
}
