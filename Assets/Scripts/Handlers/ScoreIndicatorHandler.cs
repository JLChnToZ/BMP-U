using UnityEngine;

public class ScoreIndicatorHandler: MonoBehaviour {
    private static ScoreIndicatorHandler instance;

    public static ScoreIndicatorHandler Instance {
        get { return instance; }
    }

    [SerializeField]
    private ParticleSystem[] particles;

    public void Spawn(int type, Vector3 position) {
        if(type < 0) type = particles.Length - 1;
        particles[type].Emit(new ParticleSystem.EmitParams { position = position }, 1);
    }

    private void Awake() {
        instance = this;

        // HACK: Work around on crashing in some versions
        // https://issuetracker.unity3d.com/issues/unity-crashes-when-particles-are-awaken-by-script
        foreach(ParticleSystem p in particles) {
            p.Emit(1);
            p.Clear();
        }
    }
}
