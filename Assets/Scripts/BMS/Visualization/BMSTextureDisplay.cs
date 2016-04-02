using UnityEngine;
using System.Collections;

namespace BMS.Visualization {
    [RequireComponent(typeof(Renderer))]
    public class BMSTextureDisplay: MonoBehaviour {

        public int channel;

        public BMSManager bmsManager;

        Renderer _meshRenderer;
        Renderer meshRenderer {
            get {
                if(_meshRenderer == null) _meshRenderer = GetComponent<Renderer>();
                return _meshRenderer;
            }
        }

        void Start() {
            bmsManager.OnGameStarted += OnStarted;
            bmsManager.OnChangeBackground += OnTextureUpdated;
            OnStarted();
        }

        void OnDestroy() {
            if(bmsManager != null) {
                bmsManager.OnGameStarted -= OnStarted;
                bmsManager.OnChangeBackground -= OnTextureUpdated;
            }
        }
        
        void OnStarted() {
            var texture = channel == 0 ? bmsManager.placeHolderTexture : null;
            meshRenderer.material.mainTexture = texture;
            meshRenderer.material.mainTextureOffset = Vector2.zero;
            meshRenderer.material.mainTextureScale = Vector2.one;
            meshRenderer.enabled = texture;
            transform.localPosition = Vector3.forward * transform.localPosition.z;
            transform.localScale = new Vector3(
                texture ? (float)texture.width / texture.height : 1,
                1, 1
            );
        }

        public void OnTextureUpdated(Texture texture, int channel, BGAObject? temp, int id) {
            if(this.channel == channel) {
                var scaleFineTune = Vector3.one;
                bool isMovieBmp = bmsManager.IsMovieBmp(id);
                if(bmsManager.IsMovieBmp(id) && texture != null) scaleFineTune.y *= -1;
                var mat = meshRenderer.material;
                mat.mainTexture = texture;
                BGAObject bga;
                var textureSize = Vector2.one;
                if(texture != null)
                    textureSize = new Vector2(
                        texture.width,
                        texture.height
                    );
                if(temp.HasValue)
                    bga = temp.Value;
                else
                    bga = new BGAObject {
                        clipArea = new Rect(Vector2.zero, textureSize),
                        offset = Vector2.zero
                    };
                mat.mainTextureOffset = new Vector2(
                    bga.clipArea.xMin / textureSize.x,
                    bga.clipArea.yMin / textureSize.y
                );
                mat.mainTextureScale = new Vector2(
                    bga.clipArea.width / textureSize.x,
                    bga.clipArea.height / textureSize.y
                );
                if(!temp.HasValue) {
                    transform.localScale = new Vector3(
                        textureSize.x / textureSize.y * scaleFineTune.x,
                        scaleFineTune.y,
                        scaleFineTune.z
                    );
                    transform.localPosition = Vector3.forward * transform.localPosition.z;
                } else {
                    transform.localScale = new Vector3(
                        bga.clipArea.width / 256 * scaleFineTune.x,
                        bga.clipArea.height / 256 * scaleFineTune.y,
                        scaleFineTune.z
                    );
                    transform.localPosition = new Vector3(
                        (bga.clipArea.width / 2 + bga.offset.x) / 256 - 0.5F,
                        (bga.clipArea.height / 2 - bga.offset.y) / 256 - 0.5F,
                        transform.localPosition.z
                    );
                }
                meshRenderer.enabled = texture != null;
            }
        }
    }

}
