using B83.Image.BMP;
using BananaBeats.Utils;
using BMS;
using SharpFileSystem;
using UniRx.Async;

namespace BananaBeats {
    public class BMPImageSource: ImageResource {
        private static readonly BMPLoader bmpLoader = new BMPLoader();

        public BMPImageSource(BMSResourceData resourceData, IFileSystem fileSystem, FileSystemPath path) :
            base(resourceData, fileSystem, path) {
        }

        public override async UniTask Load() {
            await UniTask.SwitchToTaskPool();
            byte[] fileData = await fileSystem.ReadAllBytesAsync(filePath);
            var bmp = bmpLoader.LoadBMP(fileData);
            await UniTask.SwitchToMainThread();
            Texture = bmp.ToTexture2D();
        }
    }
}
