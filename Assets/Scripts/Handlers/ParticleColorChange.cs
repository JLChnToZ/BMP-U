using UnityEngine;
using System.Collections;

public class ParticleColorChange : MonoBehaviour {
    public ParticleSystem particles;
    public ColorToneHandler colors;

	void Update () {
        ParticleSystem.MainModule main = particles.main;
        main.startColor = colors.ResultColor;
    }
}
