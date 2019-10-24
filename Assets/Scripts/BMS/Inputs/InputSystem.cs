using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace BananaBeats.Inputs {
    public static class InputSystem {
        private static GamePlayInputs inputs;
        private static readonly Dictionary<Guid, int> idMap = new Dictionary<Guid, int>();

        public static GamePlayInputs Inputs {
            get {
                if(inputs == null) {
                    inputs = new GamePlayInputs();
                    idMap.Clear();
                    var gameplay = inputs.Gameplay;
                    SetInputCallback(gameplay.Channel11, 11);
                    SetInputCallback(gameplay.Channel12, 12);
                    SetInputCallback(gameplay.Channel13, 13);
                    SetInputCallback(gameplay.Channel14, 14);
                    SetInputCallback(gameplay.Channel15, 15);
                    SetInputCallback(gameplay.Channel16, 16);
                    SetInputCallback(gameplay.Channel17, 17);
                    SetInputCallback(gameplay.Channel18, 18);
                    SetInputCallback(gameplay.Channel19, 19);
                    SetInputCallback(gameplay.Channel21, 21);
                    SetInputCallback(gameplay.Channel22, 22);
                    SetInputCallback(gameplay.Channel23, 23);
                    SetInputCallback(gameplay.Channel24, 24);
                    SetInputCallback(gameplay.Channel25, 25);
                    SetInputCallback(gameplay.Channel26, 26);
                    SetInputCallback(gameplay.Channel27, 27);
                    SetInputCallback(gameplay.Channel28, 28);
                    SetInputCallback(gameplay.Channel29, 29);
                }
                return inputs;
            }
        }

        private static void SetInputCallback(InputAction inputAction, int channel) {
            idMap[inputAction.id] = channel;
            inputAction.started += InputUpdated;
            inputAction.canceled += InputUpdated;
        }

        private static void InputUpdated(InputAction.CallbackContext ctx) {
            var player = BMSPlayableManager.Instance;
            if(player == null || !idMap.TryGetValue(ctx.action.id, out int channel))
                return;
            switch(ctx.phase) {
                case InputActionPhase.Started: player.HitNote(channel, true); break;
                case InputActionPhase.Canceled: player.HitNote(channel, false); break;
            }
        }
    }
}
