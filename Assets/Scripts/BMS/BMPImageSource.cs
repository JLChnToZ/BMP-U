using B83.Image.BMP;
using BananaBeats.Utils;
using BMS;
using UniRx.Async;

namespace BananaBeats {
    public class BMPImageSource: ImageResource {
        private static readonly BMPLoader bmpLoader = new BMPLoader();

        public BMPImageSource(BMSResourceData resourceData, IVirtualFSEntry fileEntry) :
            base(resourceData, fileEntry) {
        }

        public override async UniTask Load() {
            await UniTask.SwitchToTaskPool();
            byte[] fileData = await fileEntry.ReadAllBytesAsync();
            var bmp = bmpLoader.LoadBMP(fileData);
            await UniTask.SwitchToMainThread();
            Texture = bmp.ToTexture2D();
        }
    }
}
