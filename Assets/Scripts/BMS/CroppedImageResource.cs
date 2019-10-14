using BMS;
using UnityEngine;

namespace BananaBeats {
    public struct CroppedImageResource {
        public readonly ImageResource resource;
        public readonly Vector2 pos1, pos2;
        public readonly Vector2 delta;

        public CroppedImageResource(BMSResourceData resourceData, BMSLoader bmsLoader) {
            var param = resourceData.additionalData as object[];
            bmsLoader.TryGetBMP((int)(long)param[0], out resource);
            pos1 = new Vector2((float)param[1], (float)param[2]);
            pos2 = new Vector2((float)param[3], (float)param[4]);
            delta = new Vector2((float)param[5], (float)param[6]);
        }
    }
}
