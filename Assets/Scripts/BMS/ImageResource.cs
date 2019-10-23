using BMS;
using System.IO;
using BananaBeats.Utils;
using UnityEngine;
using UniRx.Async;
using SharpFileSystem;

using UnityObject = UnityEngine.Object;

namespace BananaBeats {
    public class ImageResource: BMSResource {
        public Texture2D Texture { get; protected set; }

        public virtual Vector2 Transform => Vector2.one;

        public ImageResource(BMSResourceData resourceData, IFileSystem fileSystem, FileSystemPath path) :
            base(resourceData, fileSystem, path) {
        }

        protected override async UniTask LoadImpl() {
            await UniTask.SwitchToTaskPool();
            byte[] fileData = await fileSystem.ReadAllBytesAsync(filePath);
            await UniTask.SwitchToMainThread();
            Texture = new Texture2D(2, 2);
            Texture.LoadImage(fileData);
        }

        public override void Dispose() {
            if(Texture != null) {
                UnityObject.Destroy(Texture);
                Texture = null;
            }
        }
    }
}
