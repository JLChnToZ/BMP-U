using UnityEngine;
using System.Collections;
using BMS.Visualization;
using System;
using UnityRandom = UnityEngine.Random;

[RequireComponent(typeof(LineRenderer))]
public class CurvedNoteHandler: NoteHandler {
    float timeBetween, endTimeBetween, endDelta;
    Color baseColor = Color.white;

    [NonSerialized]
    public Vector3 centroid;
    [NonSerialized]
    public float clampRangeStart, clampRangeEnd;
    [NonSerialized]
    public float startDistance, targetDistance, offset;

    float delta1 = 0, delta2 = 0;

    public SpriteRenderer startNoteHandler;
    public SpriteRenderer endNoteHander;
    public SpriteRenderer targetPointInd;
    // public SpriteRenderer scoreInd;

    [NonSerialized]
    public ScoreDisplayPack scoreDisplayPack;

    public ParticleSystem particles;

    LineRenderer _lineRenderer;
    LineRenderer lineRenderer {
        get {
            if(_lineRenderer == null)
                _lineRenderer = GetComponent<LineRenderer>();
            return _lineRenderer;
        }
    }

    protected override void Initialize() {
        transform.rotation = Quaternion.AngleAxis(Mathf.Lerp(clampRangeStart, clampRangeEnd, float.IsNaN(delta) ? 0.5F : delta) - 90, Vector3.forward);
        transform.position = centroid;
        transform.localScale = Vector3.one;
        targetPointInd.transform.localPosition = new Vector3(0, offset, targetDistance);
        targetPointInd.enabled = true;
        startNoteHandler.transform.localScale = Vector3.one;
        endNoteHander.transform.localScale = Vector3.one;
        UpdateTimeBetween();
        endTimeBetween = noteDetector.EndTimeOffset.ToAccurateSecondF();
        baseColor = Color.white;
        endNoteHander.gameObject.SetActive(isLongNote);
        lineRenderer.enabled = isLongNote;
        // scoreInd.sprite = null;
        endDelta = 0;
        endTargetTime = targetTime;
        SetColor(Color.white);
    }

    public override void SetColor(Color color) {
        baseColor = color;
    }

    public override void RegisterLongNoteEnd(TimeSpan time, int noteId) {
        base.RegisterLongNoteEnd(time, noteId);
        endNoteHander.gameObject.SetActive(true);
    }

    protected override void NoteClicked(TimeSpan timePosition, int channel, int data, int flag) {
        base.NoteClicked(timePosition, channel, data, flag);
        if(firstNoteClicked || secondNoteClicked || isMissed) {
            // scoreInd.sprite = scoreDisplayPack.images[resultFlag < 0 ? scoreDisplayPack.images.Length - 1 : resultFlag];
            if(channel == channelId)
                particles.Emit(new ParticleSystem.EmitParams { position = startNoteHandler.transform.position }, UnityRandom.Range(3, 5));
        }
        // scoreInd.transform.rotation = Quaternion.identity;
        if(secondNoteClicked)
            lineRenderer.enabled = false;
        if(isLongNote ? secondNoteClicked : firstNoteClicked) {
            ScoreIndicatorHandler.Instance.Spawn(resultFlag, startNoteHandler.transform.position);
            cycleDone = true;
        }
    }

    protected override void UpdatePosition() {
        float lerpDelay = (float)noteDetector.EndTimeOffset.TotalMilliseconds;
        TimeSpan delta = targetTime < bmsManager.TimePosition ? targetTime - bmsManager.RealTimePosition : targetTime - bmsManager.TimePosition;
        UpdateNotePos(startNoteHandler, delta.ToAccurateSecondF(), firstNoteClicked, true);
        if(isLongNote) {
            if(!longNoteRegistered) endTargetTime = bmsManager.TimePosition + bmsManager.PreEventOffset;
            UpdateNotePos(endNoteHander, (float)(endTargetTime - bmsManager.TimePosition).TotalSeconds, secondNoteClicked, false);
            lineRenderer.SetPosition(0, startNoteHandler.transform.position);
            lineRenderer.SetPosition(1, endNoteHander.transform.position);
            lineRenderer.startColor = startNoteHandler.color;
            lineRenderer.endColor = endNoteHander.color;
        }
    }

    void UpdateNotePos(SpriteRenderer handler, float timeDelta, bool clicked, bool isFirst) {
        float delta;
        bool overTime = timeDelta < 0;
        if(overTime) {
            delta = timeDelta / endTimeBetween;
            if(!clicked && delta >= 2) cycleDone = true;
        } else {
            UpdateTimeBetween(Time.deltaTime * 10);
            delta = timeDelta / timeBetween;
        }

        if(clicked || isMissed) {
            if(isLongNote ? secondNoteClicked : true) {
                endDelta = Mathf.Lerp(endDelta, 1, Time.deltaTime * 5);
                handler.color = new Color(1, 1, 1, 0);// new Color(baseColor.r, baseColor.g, baseColor.b, 1 - endDelta);
                // scoreInd.color = new Color(1, 1, 1, 1 - endDelta);
                handler.transform.localScale = Vector3.one * (1 + endDelta);
                if(endDelta >= 0.999F) cycleDone = true;
                targetPointInd.enabled = false;
            }
            if(isLongNote && !secondNoteClicked && UnityRandom.value > 0.8F)
                particles.Emit(new ParticleSystem.EmitParams { position = startNoteHandler.transform.position }, 1);
        } else if(overTime) {
            if(delta >= 1) {
                // scoreInd.color = new Color(1, 1, 1, 2 - delta);
                handler.color = new Color(1, 1, 1, 0);
            } else {
                // scoreInd.color = new Color(1, 1, 1, 0);
                handler.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1 - delta);
            }
        } else {
            handler.color = delta >= 0.5F ? new Color(baseColor.r, baseColor.g, baseColor.b, (1 - delta) * 2) : baseColor;
            if(isFirst) targetPointInd.color = new Color(1, 1, 1, (1 - delta) / 2);
        }

        if(cycleDone) return;

        if(clicked || isMissed) {
            if(isFirst && isLongNote) handler.transform.localPosition = Vector3.up * offset + Vector3.forward * targetDistance;
        } else if(overTime) {
            handler.transform.localPosition = Vector3.up * offset + Vector3.back * (targetDistance + Mathf.Abs(targetDistance - startDistance) * Mathf.Pow(delta, 0.5F) / 64);
        } else {
            handler.transform.localPosition = Vector3.up * offset + Vector3.forward * Mathf.Lerp(startDistance, targetDistance, 1 - delta);
        }
    }

    void UpdateTimeBetween(float lerp = 1) {
        timeBetween = Mathf.Lerp(timeBetween, bmsManager.PreEventOffset.ToAccurateSecondF(), lerp);
    }
}
