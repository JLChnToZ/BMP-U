using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class TrackIndicator: MonoBehaviour {
    [NonSerialized]
    public int mode;
    [NonSerialized]
    public float delta;
    [NonSerialized]
    public Vector3 centroid;
    [NonSerialized]
    public float clampRangeStart, clampRangeEnd;
    [NonSerialized]
    public float startDistance, targetDistance, offset, startOffset;
    
    public void Init() {
        delta = float.IsNaN(delta) ? 0.5F : delta;
        float clamp = Mathf.LerpUnclamped(clampRangeStart, clampRangeEnd, delta);
        switch(mode) {
            case 0:
                transform.position = centroid;
                transform.rotation = Quaternion.AngleAxis(clamp - 90, Vector3.forward);
                break;
            case 1:
                transform.position = centroid + new Vector3(clamp, 0, 0);
                transform.rotation = Quaternion.identity;
                break;
        }
        transform.localScale = Vector3.one;
        LineRenderer lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.SetVertexCount(2);
        lineRenderer.SetPosition(0, new Vector3(0, startOffset, startDistance));
        lineRenderer.SetPosition(1, new Vector3(0, offset, targetDistance - 10F));
        gameObject.SetActive(true);
    }
}
