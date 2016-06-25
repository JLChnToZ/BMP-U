
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace JLChnToZ.Toolset.UI {
    [AddComponentMenu("UGUI/Effects/OutlinePlus", 15)]
    public class OutlinePlus: Outline {
        [Range(1, 10)]
        public int iteration = 2;

        List<UIVertex> verts;

        protected OutlinePlus() {
            verts = new List<UIVertex>();
        }

        public override void ModifyMesh(VertexHelper vh) {
            if(!IsActive()) return;
            vh.GetUIVertexStream(verts);
            int _iteration = iteration * 2 + 1;
            int neededCpacity = verts.Count * _iteration * _iteration;
            if(verts.Capacity < neededCpacity) verts.Capacity = neededCpacity;
            int start = 0, end = verts.Count, i, j;
            for(i = 0; i < _iteration; i++)
                for(j = 0; j < _iteration; j++) {
                    ApplyShadowZeroAlloc(
                        verts, effectColor, start, end,
                        effectDistance.x * ((float)(j - iteration) / iteration),
                        effectDistance.y * ((float)(i - iteration) / iteration)
                    );
                    start = end;
                    end = verts.Count;
                }
            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);
            verts.Clear();
        }
    }
}