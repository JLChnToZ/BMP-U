using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using UnityEngine;

namespace BMS {
    public partial class BMSManager: MonoBehaviour {
        public byte[] Serialize() {
            using(var ms = new MemoryStream()) {
                using(var msWrite = new BinaryWriter(ms, new UTF8Encoding(false))) {
                    msWrite.Write(new[] { 'B', 'M', 'S', 'C' }, 0, 4);
                    msWrite.Write(title);
                    msWrite.Write(subTitle);
                    msWrite.Write(artist);
                    msWrite.Write(subArtist);
                    msWrite.Write(genre);
                    msWrite.Write(comments);
                    msWrite.Write((sbyte)playLevel);
                    msWrite.Write((byte)playerCount);
                    msWrite.Write((byte)rank);
                    msWrite.Write((byte)lnType);
                    msWrite.Write(volume);
                    msWrite.Write(new FileInfo(stageFilePath).Name);

                    msWrite.Write("_WAV_");
                    msWrite.Write((short)wavObjects.Count);
                    foreach(var wav in wavObjects) {
                        msWrite.Write((short)wav.Key);
                        msWrite.Write(new FileInfo(wav.Value.path).Name);
                    }

                    msWrite.Write("_BMP_");
                    msWrite.Write((short)bmpObjects.Count);
                    foreach(var bmp in bmpObjects) {
                        msWrite.Write((short)bmp.Key);
                        msWrite.Write(new FileInfo(bmp.Value.path).Name);
                    }

                    msWrite.Write("_BGA_");
                    msWrite.Write((short)bgaObjects.Count);
                    foreach(var bga in bgaObjects) {
                        msWrite.Write((short)bga.Key);
                        msWrite.Write(bga.Value.clipArea.xMin);
                        msWrite.Write(bga.Value.clipArea.yMin);
                        msWrite.Write(bga.Value.clipArea.width);
                        msWrite.Write(bga.Value.clipArea.height);
                        msWrite.Write(bga.Value.offset.x);
                        msWrite.Write(bga.Value.offset.y);
                    }

                    msWrite.Write("_BMS_");
                    var timeLineKeyframes = new List<TimeLineKeyFrame>();
                    foreach(var timeLine in timeLines)
                        foreach(var keyFrame in timeLine.Value.KeyFrames)
                            timeLineKeyframes.Add(new TimeLineKeyFrame((short)timeLine.Key, keyFrame.TimePosition, (short)keyFrame.Value, (short)keyFrame.RandomGroup, (short)keyFrame.RandomIndex));
                    timeLineKeyframes.Sort();
                    msWrite.Write(timeLineKeyframes.Count);
                    foreach(var keyFrame in timeLineKeyframes) {
                        msWrite.Write(keyFrame.timePosition);
                        msWrite.Write(keyFrame.timeLineId);
                        msWrite.Write(keyFrame.value);
                        msWrite.Write(keyFrame.randomGroup);
                        msWrite.Write(keyFrame.randomIndex);
                    }
                }
                return ms.ToArray();
            }
        }

    }
}
