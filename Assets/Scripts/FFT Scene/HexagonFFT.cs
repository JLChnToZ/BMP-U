using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
 
public class HexagonFFT : MonoBehaviour {
     
	public AudioSource audioObject;
	public GameObject hexCylinder;
     
	int count;
	public int maxGeneration;
	public int samplesCount = 512;
	public float size;
	public float scale;
	public float startDirection;
	public float lerpDelta;
	public FFTWindow FFTMode;
     
	readonly List<Transform> cylinders = new List<Transform>();
	readonly List<MeshRenderer> cylinderRenderers = new List<MeshRenderer>();
	float[] samples;
     
 
	void Start() {
		samplesCount = Mathf.ClosestPowerOfTwo(samplesCount);
		cylinders.Clear();
		cylinderRenderers.Clear();
		samples = new float[samplesCount];
        int geneIndex = 0, direction = 0;
		Vector2 currentCoord = getNextCoord(Vector2.zero, startDirection + 60, -size);
		CreateCylinder(Vector2.zero);
		for(int i = 1, g = 1; g < maxGeneration; i++) {
			CreateCylinder(currentCoord);
			if(g > 0 && geneIndex > 0 && geneIndex % g == 0)
				direction += 60;
			if(geneIndex >= g * 6 - 1) {
				currentCoord = getNextCoord(currentCoord, direction + startDirection, size);
				currentCoord = getNextCoord(currentCoord, startDirection + 240, size);
				g++;
				geneIndex = 0;
				direction = 0;
			} else {
				currentCoord = getNextCoord(currentCoord, direction + startDirection, size);
				geneIndex++;
			}
		}
		count = cylinders.Count;
	}
     
	void CreateCylinder(Vector2 position) {
		var go = Instantiate(hexCylinder);
		var goTransform = go.transform;
        go.layer = gameObject.layer;
        goTransform.SetParent(transform, false);
		goTransform.localPosition = new Vector3(position.x, 0, position.y);
		cylinders.Add(goTransform);
		cylinderRenderers.Add(go.GetComponent<MeshRenderer>());
	}
     
	Vector2 getNextCoord(Vector2 original, float direction, float distance = 1) {
		direction *= Mathf.Deg2Rad;
		return original + new Vector2(Mathf.Cos(direction), Mathf.Sin(direction)) * distance;
	}
     
	void Update() {
        if(audioObject == null)
            AudioListener.GetSpectrumData(samples, 0, FFTMode);
        else
            audioObject.GetSpectrumData(samples, 0, FFTMode);
		float deltaTime = Time.deltaTime;
		float oldSize, index, sample, newSize;
		Transform cylinTransform;
		MeshRenderer meshRender;
        Material mat;
        Color c;
		for(int i = 0; i < count; i++) {
			cylinTransform = cylinders[i];
			meshRender = cylinderRenderers[i];
            mat = meshRender.material;
            oldSize = cylinTransform.localScale.y;
			index = Mathf.Sqrt((float)i / count) * samplesCount;
			sample = GetSample(index);
			if(sample > 0)
				sample = Mathf.Sqrt(sample);
			newSize = sample * (1 + Mathf.Sqrt(index / samplesCount)) * scale;
			cylinTransform.localScale = new Vector3(1, newSize > oldSize ? newSize : Mathf.Lerp(oldSize, newSize, lerpDelta * deltaTime), 1);
            c = HelperFunctions.ColorFromHSL((1.6F - (float)i / count), 1, 1 - sample, 1);
            mat.color = Color.black;
            mat.SetColor("_EmissionColor", c);
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