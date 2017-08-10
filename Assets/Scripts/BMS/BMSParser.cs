using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

using ThreadPriority = System.Threading.ThreadPriority;

namespace BMS {
    public enum BMSFileType {
        Unknown,
        Standard,
        Extended,
        Long,
        Popn,
        Bmson
    }

    public partial class BMSManager: MonoBehaviour {
        static Dictionary<int, int> channelAdvancedMapping;
        string resourcePath;
        float bpm;

        bool stageFileLoaded;
        bool bannerFileLoaded;
        bool hasBGA;
        BMSFileType fileType;
        Texture stageFile;
        Texture bannerFile;
        TimeSpan duration;
        TimeSpan endTimeTheshold;
        TimeSpan startPos;

        public string Title { get { return chart.Title; } }
        public string Artist { get { return chart.Artist; } }
        public string SubArtist { get { return chart.SubArtist; } }
        public string Comments { get { return chart.Comments; } }
        public string Genre { get { return chart.Genre; } }
        public int PlayerCount { get { return chart.PlayerCount; } }
        public float BPM { get { return isStarted ? bpm : chart.BPM; } }
        public float PlayLevel { get { return chart.PlayLevel; } }
        public int Rank { get { return chart.Rank; } }
        public float Volume { get { return chart.Volume; } }
        public BMSKeyLayout OriginalLayout { get { return chart.Layout; } }
        public int OriginalNotesCount { get { return chart.MaxCombos; } }
        public Texture StageFile { get { return stageFile; } }
        public Texture BannerFile { get { return bannerFile; } }
        public int LongNoteType { get { return 1; } }
        public TimeSpan Duration { get { return duration; } }
        public TimeSpan StartPosition { get { return startPos; } }
        public string StageFilePath {
            get {
                return chart.GetResourceData(ResourceType.bmp, -1).dataPath ?? string.Empty;
            }
        }
        public string BannerFilePath {
            get {
                return chart.GetResourceData(ResourceType.bmp, -2).dataPath ?? string.Empty;
            }
        }
        public bool BannerFileLoaded { get { return bannerFileLoaded; } }
        public bool StageFileLoaded { get { return stageFileLoaded; } }
        
        bool parseHeader, parseBody, parseResHeader, parseAll;
        ResourceObject stageFileObject, bannerFileObject;
        Thread loadBMSThread;

        public event Action OnStageFileLoaded;
        public event Action OnBannerFileLoaded;

        public bool HasRandom {
            get { return chart.Randomized; }
        }

        void StopPreviousBMSLoadJob() {
            if(loadBMSThread == null) return;
            if(loadBMSThread.IsAlive)
                loadBMSThread.Abort();
        }

        void ReloadTimeline(bool parseHeader, bool parseBody, bool parseResHeader, bool direct) {
            StopPreviousBMSLoadJob();
            this.parseHeader = parseHeader;
            this.parseBody = parseBody;
            this.parseResHeader = parseResHeader;
            bmsLoaded = false;
            if(!direct) {
                loadBMSThread = new Thread(ReloadTimelineInThread) {
                    Priority = ThreadPriority.BelowNormal,
                    IsBackground = true
                };
                if(parseHeader) {
                    bannerFileLoaded = false;
                    stageFileLoaded = false;
                }
                StartCoroutine(ReloadTimelineHelperCoroutine(parseHeader, parseBody));
                loadBMSThread.Start();
            } else {
                ReloadTimelineInThread();
            }
        }

        IEnumerator ReloadTimelineHelperCoroutine(bool header, bool body) {
            if(header) {
                stageFileObject = null;
                bannerFileObject = null;
                while(stageFileObject == null && !bmsLoaded) yield return null;
                if(stageFileObject != null) {
                    yield return StartCoroutine(new ResourceLoader(resourcePath).LoadResource(stageFileObject));
                    stageFile = stageFileObject.texture;
                    stageFileLoaded = true;
                    if(OnStageFileLoaded != null)
                        OnStageFileLoaded.Invoke();
                } else
                    stageFileLoaded = true;

                while(bannerFileObject == null && !bmsLoaded) yield return null;
                if(bannerFileObject != null) {
                    yield return StartCoroutine(new ResourceLoader(resourcePath).LoadResource(bannerFileObject));
                    bannerFile = bannerFileObject.texture;
                    bannerFileLoaded = true;
                    if(OnBannerFileLoaded != null)
                        OnBannerFileLoaded.Invoke();
                } else
                    bannerFileLoaded = true;
            }
            yield break;
        }

        void ReloadTimelineInThread() {
            try {
                ParseType parseType = ParseType.None;
                if(parseHeader) parseType |= ParseType.Header | ParseType.ContentSummary;
                if(parseResHeader) parseType |= ParseType.Resources;
                if(parseBody) parseType |= ParseType.Content;
                chart.Parse(parseType);
                if(parseHeader) {
                    ConvertToResourceObject(ref stageFileObject, -1);
                    ConvertToResourceObject(ref bannerFileObject, -2);
                }
                if(parseResHeader)
                    foreach(BMSResourceData resData in chart.IterateResourceData()) {
                        switch(resData.type) {
                            case ResourceType.wav:
                                if(!wavObjects.ContainsKey((int)resData.resourceId))
                                    wavObjects[(int)resData.resourceId] = new ResourceObject(
                                        (int)resData.resourceId, ResourceType.wav, resData.dataPath);
                                break;
                            case ResourceType.bmp:
                                if(!bmpObjects.ContainsKey((int)resData.resourceId))
                                    bmpObjects[(int)resData.resourceId] = new ResourceObject(
                                        (int)resData.resourceId, ResourceType.bmp, resData.dataPath);
                                break;
                            case ResourceType.bga:
                                object[] data = resData.additionalData as object[];
                                Vector2 pos1 = new Vector2((float)data[1], (float)data[2]);
                                Vector2 pos2 = new Vector2((float)data[3], (float)data[4]);
                                bgaObjects.Add((int)resData.resourceId, new BGAObject {
                                    index = (int)data[0],
                                    clipArea = new Rect(pos1, pos2 - pos1),
                                    offset = new Vector2((float)data[5], (float)data[6])
                                });
                                break;
                        }
                    }
                if(parseBody) {
                    hasBGA = chart.Events.Any(ev => ev.type == BMSEventType.BMP);
                    startPos = chart.Events.FirstOrDefault(ev => ev.IsNote).time;
                    duration = mainTimingHelper.EndTime;
                    endTimeTheshold = duration + TimeSpan.FromSeconds(noteOffsetThesholds.Last());
                }
            } catch(ThreadAbortException) {
                Debug.LogWarning("BMS parsing aboarted.");
            } catch(Exception ex) {
                Debug.LogException(ex);
            } finally {
                bmsLoaded = true;
                if(OnBMSLoaded != null)
                    OnBMSLoaded.Invoke();
            }
        }

        void ConvertToResourceObject(ref ResourceObject resObj, int index) {
            if(bmpObjects.TryGetValue(index, out resObj))
                return;
            BMSResourceData resData;
            if(chart.TryGetResourceData(ResourceType.bmp, index, out resData)) {
                resObj = new ResourceObject(index, ResourceType.bmp, resData.dataPath);
                bmpObjects[index] = resObj;
                return;
            }
            resObj = null;
        }
    }
}
