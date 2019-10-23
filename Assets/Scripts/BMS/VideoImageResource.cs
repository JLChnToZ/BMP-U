using System;
using UnityEngine;
using VideoStreamer;
using BMS;
using BananaBeats.Utils;
using UniRx.Async;
using SharpFileSystem;

namespace BananaBeats {
    public class VideoImageResource: ImageResource {
        private VideoStream videoStream;

        public override Vector2 Transform => new Vector2(1, -1);

        public VideoImageResource(BMSResourceData resourceData, IFileSystem fileSystem, FileSystemPath path) :
            base(resourceData, fileSystem, path) {
        }

        protected override async UniTask LoadImpl() {
            if(videoStream != null) return;
            await UniTask.SwitchToTaskPool();
            try {
                videoStream = new VideoStream(await fileSystem.GetRealPathAsync(filePath)) {
                    Preload = true,
                };
                videoStream.FrameChanged += FrameChanged;
            } catch(DllNotFoundException ex) {
                try {
                    videoStream.Dispose();
                } catch {
                } finally {
                    videoStream = null;
                }
                throw ex;
            }
            await UniTask.SwitchToMainThread();
            var size = videoStream.VideoSize;
            Texture = new Texture2D((int)size.X, (int)size.Y, TextureFormat.RGBA32, false) {
                wrapMode = TextureWrapMode.Repeat
            };
        }

        private void FrameChanged(VideoStream videoStream) {
            if(Texture == null) return;
            var buffer = videoStream.VideoFrameData.VideoDataBuffer;
            Texture.LoadRawTextureData(buffer.BufferPointer, (int)buffer.Length);
            Texture.Apply();
        }

        public override void Play(BMSEvent bmsEvent) {
            if(videoStream == null) return;
            if(videoStream.CurrentState != PlayState.Stopped)
                videoStream.Stop();
            videoStream.Play();
            base.Play(bmsEvent);
        }

        public override void Pause() {
            if(videoStream == null) return;
            videoStream.Pause();
            base.Pause();
        }

        public override void Resume() {
            if(videoStream == null) return;
            videoStream.Play();
            base.Resume();
        }

        public override void Reset() {
            if(videoStream == null) return;
            videoStream.Stop();
            base.Reset();
        }

        public override void Update(TimeSpan diff) {
            if(videoStream != null && videoStream.CurrentState == PlayState.Playing) {
                videoStream.Update(diff);
            } else if(wasPlaying) {
                wasPlaying = false;
                InvokeEnd();
            }
        }

        public override void Dispose() {
            try {
                if(videoStream != null) {
                    videoStream.FrameChanged -= FrameChanged;
                    videoStream.Dispose();
                    videoStream = null;
                }
            } catch { }
            base.Dispose();
        }
    }
}
