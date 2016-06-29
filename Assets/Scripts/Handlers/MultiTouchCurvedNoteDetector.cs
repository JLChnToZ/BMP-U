using UnityEngine;
using System.Collections.Generic;
using BMS;
using BMS.Visualization;

[RequireComponent(typeof(NoteSpawnerSP))]
public class MultiTouchCurvedNoteDetector : MonoBehaviour {
    public Camera hitTestCamera;
    public NoteDetector noteDetector;
    public NoteSpawnerSP noteSpawner;

    public float startLength, endLength;

    readonly Dictionary<int, int> touchMapping = new Dictionary<int, int>();
    int mouseMapping;

    bool previousMouseState;
    Vector3 previousMousePosition;

    void Start() {
        Input.multiTouchEnabled = true;
        if(Input.touchSupported) Input.simulateMouseWithTouches = false;
    }

    void Update() {
        IList<int> mapping = noteSpawner.MappedChannels;
        int idx;
        bool touchHandled = false;
        foreach(var touch in Touches.Instance) {
            switch(touch.phase) {
                case TouchPhase.Began:
                    idx = DetectIndex(touch.position, mapping);
                    touchMapping[touch.fingerId] = idx;
                    HandleTouch(idx, mapping, true);
                    break;
                case TouchPhase.Moved:
                    idx = DetectIndex(touch.position, mapping);
                    if(idx != touchMapping[touch.fingerId]) {
                        HandleTouch(touchMapping[touch.fingerId], mapping, false);
                        HandleTouch(idx, mapping, true);
                        touchMapping[touch.fingerId] = idx;
                    }
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    idx = DetectIndex(touch.position, mapping);
                    HandleTouch(idx, mapping, false);
                    touchMapping[touch.fingerId] = -1;
                    break;
            }
            touchHandled = true;
        }
        if(!touchHandled) {
            bool currentMouseState = Input.GetMouseButton(0);
            Vector3 currentMousePosition = Input.mousePosition;
            if(currentMouseState != previousMouseState) {
                idx = DetectIndex(currentMousePosition, mapping);
                HandleTouch(idx, mapping, currentMouseState);
                mouseMapping = currentMouseState ? idx : -1;
            } else if(currentMousePosition != previousMousePosition && currentMouseState) {
                idx = DetectIndex(currentMousePosition, mapping);
                if(idx != mouseMapping) {
                    HandleTouch(mouseMapping, mapping, false);
                    HandleTouch(idx, mapping, true);
                    mouseMapping = idx;
                }
            }
            previousMouseState = currentMouseState;
            previousMousePosition = currentMousePosition;
        }
    }

    int DetectIndex(Vector3 position, IList<int> mapping) {
        int mappingCount = mapping.Count;
        if(mappingCount <= 0) return -1;
        Vector3 localPosition = hitTestCamera.ScreenToWorldPoint(position, Vector3.Distance(hitTestCamera.transform.position, noteSpawner.centroid));
        float distance = Vector2.Distance(localPosition, noteSpawner.centroid);
        if(distance < startLength || distance > endLength) return -1;
        float anglePerSlot = (noteSpawner.clampRangeEnd - noteSpawner.clampRangeStart) / mappingCount;
        float angle = Mathf.Repeat(Mathf.Atan2(localPosition.y - noteSpawner.centroid.y, localPosition.x - noteSpawner.centroid.x) * Mathf.Rad2Deg, 360F) + anglePerSlot / 2;
        if(angle < noteSpawner.clampRangeStart || angle > noteSpawner.clampRangeEnd + anglePerSlot) return -1;
        return Mathf.FloorToInt(Mathf.Clamp(Mathf.InverseLerp(noteSpawner.clampRangeStart, noteSpawner.clampRangeEnd + anglePerSlot, angle) * mappingCount, 0, mappingCount - 1));
    }

    void HandleTouch(int i, IList<int> mapping, bool isDown) {
        if(i >= 0) noteDetector.OnClick(mapping[i], isDown);
    }
}
