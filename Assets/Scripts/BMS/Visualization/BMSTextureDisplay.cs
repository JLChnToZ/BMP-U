using UnityEngine;
using System.Collections;

namespace BMS.Visualization {
    [RequireComponent(typeof(Renderer))]
    public class BMSTextureDisplay: MonoBehaviour {

        static readonly float magitudeOfVector2One = Vector2.one.sqrMagnitude;

        public int channel;

        public BMSManager bmsManager;

        bool hasBga = false;

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
            hasBga = false;
            transform.localPosition = Vector3.forward * transform.localPosition.z;
            transform.localScale = new Vector3(
                texture ? (float)texture.width / texture.height : 1,
                1, 1
            );
        }

        public void OnTextureUpdated(Texture texture, int channel, BGAObject? temp, int id) {
            if(this.channel == channel) {
                bool inverted = texture != bmsManager.placeHolderTexture;
                var mat = meshRenderer.material;
                mat.mainTexture = texture;
                BGAObject bga;
                var textureSize = Vector2.one;
                if(texture)
                    textureSize = new Vector2(
                        texture.width,
                        texture.height
                    );
                if(temp.HasValue)
                    bga = temp.Value;
                else
                    bga = new BGAObject {
                        clipArea = new Rect(Vector2.zero, textureSize),
                        offset = new Vector2(128 - textureSize.x / 2, 0)
                    };
                if(texture) {
                    var filterMode = channel == 0 ? FilterMode.Bilinear : FilterMode.Point;
                    var wrapMode =
                        filterMode == FilterMode.Point ||
                        Mathf.Repeat(bga.clipArea.xMin, textureSize.x) > Mathf.Repeat(bga.clipArea.xMax, textureSize.x) ||
                        Mathf.Repeat(bga.clipArea.yMin, textureSize.y) > Mathf.Repeat(bga.clipArea.yMax, textureSize.y) ?
                            TextureWrapMode.Repeat :
                            TextureWrapMode.Clamp;
                    if(texture.filterMode != filterMode)
                        texture.filterMode = filterMode;
                    if(texture.wrapMode != wrapMode)
                        texture.wrapMode = wrapMode;
                }
                var textureOffset = new Vector2(
                    Mathf.Repeat(bga.clipArea.xMin / textureSize.x, 1),
                    Mathf.Repeat(bga.clipArea.yMax / textureSize.y, 1)
                );
                if(inverted && textureOffset.y == 0)
                    textureOffset.y = 1;
                mat.mainTextureOffset = textureOffset;
                mat.mainTextureScale = new Vector2(
                    bga.clipArea.width / textureSize.x,
                    bga.clipArea.height / textureSize.y * (inverted ? -1 : 1)
                );
                if(!temp.HasValue && !hasBga) {
                    transform.localScale = new Vector3(textureSize.x / textureSize.y, 1, 1);
                    transform.localPosition = Vector3.forward * transform.localPosition.z;
                } else {
                    transform.localScale = (Vector3)(bga.clipArea.size / 256) + Vector3.forward;
                    transform.localPosition = new Vector3(
                        (bga.clipArea.width / 2 + bga.offset.x) / 256 - 0.5F,
                        -(bga.clipArea.height / 2 + bga.offset.y) / 256 + 0.5F,
                        transform.localPosition.z
                    );
                }
                meshRenderer.enabled = texture != null;
            } else if(meshRenderer.material.mainTexture == bmsManager.placeHolderTexture && texture != null) {
                meshRenderer.material.mainTexture = null;
                meshRenderer.enabled = false;
            }
            if(channel >= 0) hasBga = true;
        }
    }

}
