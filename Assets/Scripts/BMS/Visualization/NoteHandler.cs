using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace BMS.Visualization {
    public abstract class NoteHandler: MonoBehaviour {
        protected BMSManager bmsManager;
        protected NoteSpawner noteSpawner;
        protected NoteDetector noteDetector;
        protected int channelId;
        protected int noteId, endNoteId;
        protected TimeSpan targetTime, endTargetTime;
        protected float delta;
        protected bool isIdle = true;
        protected bool isLongNote = false;
        protected bool longNoteRegistered = false;
        protected bool firstNoteClicked = false;
        protected bool secondNoteClicked = false;
        protected bool isMissed = false;
        protected bool cycleDone = false;
        protected int resultFlag = -1;
        public bool IsIdle {
            get { return isIdle; }
        }

        public void Register(NoteSpawner noteSpawner, BMSManager bmsManager, TimeSpan time, int channelId, int noteId, float delta, bool isLongNote) {
            if(!isIdle) return;
            this.noteSpawner = noteSpawner;
            noteDetector = noteSpawner.noteDetector;
            noteDetector.OnNoteClicked += NoteClicked;
            noteDetector.OnLongNoteMissed += LongNoteMissed;
            this.bmsManager = bmsManager;
            targetTime = time;
            this.channelId = channelId;
            this.noteId = noteId;
            this.delta = delta;
            isIdle = false;
            isMissed = false;
            cycleDone = false;
            longNoteRegistered = false;
            firstNoteClicked = false;
            secondNoteClicked = false;
            resultFlag = -1;
            this.isLongNote = isLongNote;
            gameObject.SetActive(true);
            Initialize();
            StartCoroutine(UpdateCoroutine());
        }

        protected virtual void NoteClicked(TimeSpan timePosition, int channel, int data, int flag) {
            bool handleN1 = targetTime == timePosition && channel == channelId && data == noteId;
            bool handleN2 = endTargetTime == timePosition && channel == channelId && data == endNoteId;
            bool handle = isLongNote ? handleN2 : handleN1;
            if(handleN1 || handleN2) {
                resultFlag = flag;

                if(bmsManager.IsValidFlag(flag)) {
                    if(handleN1) firstNoteClicked = true;
                    if(handle) secondNoteClicked = true;
                } else {
                    if(handle) isMissed = true;
                }
                if(handle && noteDetector != null) {
                    noteDetector.OnNoteClicked -= NoteClicked;
                    noteDetector.OnLongNoteMissed -= LongNoteMissed;
                }
            }
        }

        protected virtual void LongNoteMissed(int channel) {
            if(channel == channelId && isLongNote) {
                cycleDone = true;
            }
        }

        void OnDestroy() {
            if(!isIdle && noteDetector != null) {
                noteDetector.OnNoteClicked -= NoteClicked;
                noteDetector.OnLongNoteMissed -= LongNoteMissed;
            }
        }

        public virtual void RegisterLongNoteEnd(TimeSpan time, int noteId) {
            endTargetTime = time;
            endNoteId = noteId;
            longNoteRegistered = true;
        }

        public void SetMatchColor() {
            SetColor(noteSpawner.CurrentMatchColor);
        }

        public virtual void SetColor(Color color) { }
        
        IEnumerator UpdateCoroutine() {
            while(!cycleDone) {
                UpdatePosition();
                if((isLongNote ? secondNoteClicked : firstNoteClicked) && noteSpawner.RequireRecycleImmediately) {
                    cycleDone = true;
                    break;
                }
                yield return null;
            }
            if(noteDetector != null) {
                noteDetector.OnNoteClicked -= NoteClicked;
                noteDetector.OnLongNoteMissed -= LongNoteMissed;
            }
            gameObject.SetActive(false);
            isIdle = true;
            noteSpawner.RecycleNote(this);
            yield break;
        }

        protected virtual void Initialize() { }

        protected virtual void UpdatePosition() { }
    }
}