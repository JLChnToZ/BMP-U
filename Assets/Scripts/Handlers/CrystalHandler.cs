using UnityEngine;
using JLChnToZ.Voxels;
using BMS;
using System;

public class CrystalHandler : MonoBehaviour {
    public BMSManager bmsManager;
    public MarchingCubeChunks voxelChunks;

    public FFTWindow FFTMode;
    public int samplesCount = 512;

    public int crystalGenerationCount = 16;
    public int crystalResoultion = 64;
    public int crystalChunkSize = 16;

    float[] samples;

    void Start () {
        if(bmsManager!= null) {
            bmsManager.OnGameStarted += GameStarted;
            bmsManager.OnGameEnded += GameEnded;
            bmsManager.OnBeatFlow += BeatFlow;
        }
        samples = new float[samplesCount];
    }

    void OnDestroy() {
        if(bmsManager != null) {
            bmsManager.OnGameStarted -= GameStarted;
            bmsManager.OnGameEnded -= GameEnded;
            bmsManager.OnBeatFlow -= BeatFlow;
        }
    }

    void GameStarted() {
        voxelChunks.Initialize(crystalChunkSize);
    }

    void GameEnded() {
        voxelChunks.StartUpdateQueue();
    }

    void BeatFlow(float measure, float beat) {
        AudioListener.GetSpectrumData(samples, 0, FFTMode);
        float location, sample;
        Vector3 pos;
        for(int i = 0; i < crystalGenerationCount; i++) {
            location = (float)i / crystalGenerationCount;
            sample = GetSample(Mathf.Sqrt(location) * samplesCount);
            pos = Quaternion.Euler(location * 180 - 90, bmsManager.Accuracy * 0.36F, 0) * Vector3.forward * (sample * crystalResoultion / 2);
            voxelChunks.ExplodeBrush(pos.x, pos.y, pos.z, 1);
        }
    }

    float GetSample(float index) {
        int floor = Mathf.FloorToInt(index);
        if(Mathf.Approximately(floor, index))
            return samples[floor];
        int ceiling = Mathf.CeilToInt(index);
        return Mathf.Sqrt(Mathf.Lerp(samples[floor], samples[ceiling], index - floor));
    }
}
