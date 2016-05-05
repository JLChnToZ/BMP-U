using UnityEngine;
using System.Collections;

public class ParticleColorChange : MonoBehaviour {
    public ParticleSystem particles;
    public ColorToneHandler colors;

	void Update () {
        particles.startColor = colors.ResultColor;
    }
}
