using System;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using ManagedBass;
#endif

namespace BananaBeats {
    public static class ConfigConstants {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init() {
            AudioResource.InitEngine();

            // Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            BMSPlayableManager.ScoreConfig = new ScoreConfig {
                comboBonusRatio = 0.4F,
                maxScore = 10000000,
                timingConfigs = new[] {
                    new TimingConfig { rankType = 0, score = 1F, secondsDiff = 0.07F, },
                    new TimingConfig { rankType = 1, score = 0.8F, secondsDiff = 0.2F, },
                    new TimingConfig { rankType = 2, score = 0.5F, secondsDiff = 0.4F, },
                    new TimingConfig { rankType = -1, score = 0F, secondsDiff = 1F, },
                },
            };

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
#endif
        }

#if UNITY_EDITOR
        private static void PlayModeStateChanged(PlayModeStateChange state) {
            if(state == PlayModeStateChange.ExitingPlayMode) {
                Bass.Free();
                EditorApplication.playModeStateChanged -= PlayModeStateChanged;
            }
        }
#endif
    }
}
