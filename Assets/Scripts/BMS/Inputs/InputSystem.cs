using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace BananaBeats.Inputs {
    public static class InputSystem {
        private static InputActionMap actionMap;
        private static readonly Dictionary<Guid, int> idMap = new Dictionary<Guid, int>();

        public static InputActionMap GetActionMap() {
            if(actionMap == null) {
                actionMap = new InputActionMap("Gameplay");
                string binding = "zawsedqrfxtgyhujolik";
                for(int i = 0; i < 20; i++) {
                    var act = actionMap.AddAction($"Channel{i + 10}", binding: $"<keyboard>/{binding[i]}");
                    act.started += InputUpdated;
                    act.canceled += InputUpdated;
                    idMap[act.id] = i + 10;
                }
                actionMap.Enable();
            }
            return actionMap;
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
