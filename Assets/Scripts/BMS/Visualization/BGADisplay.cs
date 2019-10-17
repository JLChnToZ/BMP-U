using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BMS;
using BananaBeats;

namespace BananaBeats.Visualization {
    public class BGADisplay: IDisposable {
        private bool enabled = true;
        private Rect clipArea;
        private Vector2 offset;
        private Texture texture;
        private Vector2 textureTransform;
        private bool isCropped;
        private readonly Transform rendererTransform;
        private const float MAX_RESOLUTION = 256;

        public int Channel { get; }

        public BMSPlayer Player { get; }

        public Renderer Renderer { get; }

        public bool Enabled {
            get { return enabled; }
            set {
                enabled = value;
                UpdateDisplay();
            }
        }

        public BGADisplay(BMSPlayer player, Renderer renderer, int channel) {
            Channel = channel;
            Player = player;
            Renderer = renderer;
            rendererTransform = renderer.transform;
            player.BMSEvent += BMSEvent;
            renderer.enabled = false;
        }

        private void BMSEvent(BMSEvent bmsEvent, object resource) {
            if(bmsEvent.type != BMSEventType.BMP || bmsEvent.data1 != Channel)
                return;
            if(resource is CroppedImageResource cropped) {
                texture = cropped.resource.Texture;
                textureTransform = cropped.resource.Transform;
                clipArea = new Rect(cropped.pos1, cropped.pos2 - cropped.pos1);
                offset = cropped.delta;
                isCropped = true;
            } else if(resource is ImageResource imgres) {
                texture = imgres.Texture;
                textureTransform = imgres.Transform;
                if(texture != null) {
                    clipArea = new Rect(Vector2.zero, GetTextureSize());
                    offset = new Vector2(128 - clipArea.width / 2, 0);
                } else {
                    clipArea = Rect.zero;
                    offset = Vector2.zero;
                }
                isCropped = false;
            } else {
                texture = null;
                textureTransform = Vector3.one;
                clipArea = Rect.zero;
                offset = Vector2.zero;
                isCropped = false;
            }
            UpdateDisplay();
        }

        private void UpdateDisplay() {
            if(!enabled) {
                Renderer.enabled = false;
                return;
            }
            Vector2 textureSize = GetTextureSize();
            if(texture != null) {
                var filterMode = Channel == 0 ? FilterMode.Bilinear : FilterMode.Point;
                var wrapMode =
                    filterMode == FilterMode.Point ||
                    Mathf.Repeat(clipArea.xMin, textureSize.x) > Mathf.Repeat(clipArea.xMax, textureSize.x) ||
                    Mathf.Repeat(clipArea.yMin, textureSize.y) > Mathf.Repeat(clipArea.yMax, textureSize.y) ?
                        TextureWrapMode.Repeat :
                        TextureWrapMode.Clamp;
                if(texture.filterMode != filterMode)
                    texture.filterMode = filterMode;
                if(texture.wrapMode != wrapMode)
                    texture.wrapMode = wrapMode;
            }
            var textureOffset = new Vector2(
                Mathf.Repeat(clipArea.xMin / textureSize.x, 1),
                Mathf.Repeat(clipArea.yMax / textureSize.y, 1)
            );
            if(textureOffset.y == 0 && textureTransform.y < 0)
                textureOffset.y = 1;
            var mat = Renderer.sharedMaterial;
            mat.mainTexture = texture;
            mat.mainTextureOffset = textureOffset;
            mat.mainTextureScale = new Vector2(
                clipArea.width / textureSize.x * textureTransform.x,
                clipArea.height / textureSize.y * textureTransform.y
            );
            if(isCropped) {
                rendererTransform.localScale = new Vector3(textureSize.x / textureSize.y, 1, 1);
                rendererTransform.localPosition = Vector3.forward * rendererTransform.localPosition.z;
            } else {
                rendererTransform.localScale = (Vector3)(clipArea.size / MAX_RESOLUTION) + Vector3.forward;
                rendererTransform.localPosition = new Vector3(
                    (clipArea.width / 2 + offset.x) / MAX_RESOLUTION - 0.5F,
                    -(clipArea.height / 2 + offset.y) / MAX_RESOLUTION + 0.5F,
                    rendererTransform.localPosition.z
                );
            }
            Renderer.enabled = texture != null;
        }

        private Vector2 GetTextureSize() {
            if(texture == null)
                return Vector2.one;
            if(texture.width > MAX_RESOLUTION)
                return new Vector2(MAX_RESOLUTION, MAX_RESOLUTION * texture.height / texture.width);
            if(texture.height > MAX_RESOLUTION)
                return new Vector2(MAX_RESOLUTION * texture.width / texture.height, MAX_RESOLUTION);
            return new Vector2(texture.width, texture.height);
        }

        public void Dispose() {
            Player.BMSEvent -= BMSEvent;
        }
    }
}
