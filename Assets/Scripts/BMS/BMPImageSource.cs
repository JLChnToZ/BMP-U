using StbImageSharp;
using BMS;
using SharpFileSystem;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace BananaBeats {
    public class BMPImageSource: ImageResource {
        private static readonly ImageStreamLoader bmpLoader = new ImageStreamLoader();

        public BMPImageSource(BMSResourceData resourceData, IFileSystem fileSystem, FileSystemPath path) :
            base(resourceData, fileSystem, path) {
        }

        public override Vector2 Transform => new Vector2(1, -1);

        protected override async UniTask LoadImpl() {
            ImageResult bmp;
            await UniTask.SwitchToTaskPool();
            using(var stream = fileSystem.OpenFile(filePath, System.IO.FileAccess.Read))
                bmp = bmpLoader.Load(stream, ColorComponents.RedGreenBlueAlpha);
            await UniTask.SwitchToMainThread();
            Texture = new Texture2D(bmp.Width, bmp.Height, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None);
            Texture.LoadRawTextureData(bmp.Data);
            Texture.Apply();
        }
    }
}
