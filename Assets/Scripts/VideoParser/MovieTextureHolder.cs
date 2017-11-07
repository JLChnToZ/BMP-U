using UnityEngine;
using System;
using System.Collections;
using System.IO;
#if UNITY_STANDALONE_WIN
using VideoStreamer;

public class MovieTextureHolder: ScriptableObject {
    [NonSerialized]
    VideoStream videoStream;
    [NonSerialized]
    Texture2D outputTexture;
    [NonSerialized]
    bool loaded = false, hasNewFrame = false;
    [NonSerialized]
    byte[] frame;

    public bool Loaded {
        get { return loaded; }
    }

    public Texture Output {
        get { return outputTexture; }
    }

    public TimeSpan Duration {
        get { return videoStream != null ? videoStream.VideoLength : TimeSpan.Zero; }
    }

    public TimeSpan Position {
        get { return videoStream != null ? videoStream.VideoPlayingOffset : TimeSpan.Zero; }
    }

    public bool IsPlaying {
        get { return loaded && videoStream != null && videoStream.CurrentState == PlayState.Playing; }
    }

    public static MovieTextureHolder Create(FileInfo file) {
        var instance = CreateInstance<MovieTextureHolder>();
        instance.Init(file);
        return instance;
    }

    private MovieTextureHolder() { }

    public void Init(FileInfo file) {
        try {
            if(videoStream != null)
                videoStream.FrameChanged -= FrameChanged;
            videoStream = new VideoStream(file.FullName);
            videoStream.Preload = true;
            videoStream.FrameChanged += FrameChanged;
            outputTexture = new Texture2D((int)videoStream.VideoSize.X, (int)videoStream.VideoSize.Y, TextureFormat.RGBA32, false);
            outputTexture.wrapMode = TextureWrapMode.Repeat;
            Stop();
            loaded = true;
        } catch(Exception ex) {
            Debug.LogException(ex);
            OnDestroy();
        }
    }

    private void FrameChanged(VideoStream stream) {
        var buffer = stream.VideoFrameData.VideoDataBuffer;
        outputTexture.LoadRawTextureData(buffer.BufferPointer, (int)buffer.Length);
        outputTexture.Apply();
    }

    public void Play() {
        if(!loaded || videoStream == null) return;
        videoStream.Play();
        MovieTexturePlayer.GetOrCreate(false).Register(this);
    }

    public void Pause() {
        if(!loaded || videoStream == null) return;
        videoStream.Pause();
        Unregister();
    }

    public void Stop() {
        if(!loaded || videoStream == null) return;
        videoStream.Stop();
        Unregister();
    }

    public void ReadFrame(long ticksDiff) {
        if(loaded && videoStream != null && videoStream.CurrentState == PlayState.Playing)
            videoStream.Update(ticksDiff);
    }

    void OnDestroy() {
        loaded = false;
        if(videoStream != null) {
            videoStream.FrameChanged -= FrameChanged;
            videoStream.Dispose();
            videoStream = null;
        }
        if(outputTexture)
            Destroy(outputTexture);
        Unregister();
    }

    void Unregister() {
        var player = MovieTexturePlayer.Instance;
        if(player) player.Unregister(this);
    }
}
#else
public class MovieTextureHolder:ScriptableObject {
    public bool Loaded {
        get { return false; }
    }

    public Texture Output {
        get { return null; }
    }

    public TimeSpan Duration {
        get { return TimeSpan.Zero; }
    }

    public TimeSpan Position {
        get { return TimeSpan.Zero; }
    }

    public bool IsPlaying {
        get { return false; }
    }

    public static MovieTextureHolder Create(FileInfo file) {
        throw new NotSupportedException();
    }

    private MovieTextureHolder() { }

    public void Play() {
        throw new NotSupportedException();
    }

    public void Pause() {
        throw new NotSupportedException();
    }

    public void Stop() {
        throw new NotSupportedException();
    }
    
    public void ReadFrame(long ticksDiff) {

    }
}
#endif