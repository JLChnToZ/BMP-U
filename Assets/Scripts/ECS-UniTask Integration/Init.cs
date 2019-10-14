using UnityEngine;
using UniRx.Async;
using Unity.Entities;

public static class StaticInit {
#if !UNITY_2019_3_OR_NEWER
    // Hack function for both ECS and UniTask
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() {
        // Get ECS Loop.
        var playerLoop = ScriptBehaviourUpdateOrder.CurrentPlayerLoop;

        // Setup UniTask's PlayerLoop.
        PlayerLoopHelper.Initialize(ref playerLoop);
    }
#endif
}
