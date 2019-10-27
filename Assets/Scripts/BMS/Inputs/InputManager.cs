using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using BMS;
using BananaBeats.Layouts;
using BananaBeats.Utils;
using BananaBeats.PlayerData;

namespace BananaBeats.Inputs {
    public static class InputManager {
        private static BMSKeyLayout currentLayout;
        private static GamePlayInputs inputs;
        private static readonly Dictionary<Guid, int> idMap = new Dictionary<Guid, int>();
        public static Dictionary<BMSKeyLayout, Dictionary<Guid, string>> bindings = new Dictionary<BMSKeyLayout, Dictionary<Guid, string>>();

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

        public static void Load(PlayerDataManager playerDataManager) {
            foreach(var binding in playerDataManager.GetKeyBinding())
                bindings.GetOrConstruct(binding.LayoutType)[binding.Guid] = binding.Path;
        }

        public static void Save(PlayerDataManager playerDataManager) {
            foreach(var binding in bindings)
                foreach(var mapping in binding.Value)
                    playerDataManager.SetKeyBinding(mapping.Key, binding.Key, mapping.Value);
        }

        public static void SwitchBindingLayout(BMSKeyLayout layout) {
            currentLayout = layout;
            foreach(var action in inputs.Gameplay.Get())
                SwitchBinding(action, layout);
        }

        public static void SwitchBinding(Guid id, BMSKeyLayout layout) =>
            SwitchBinding(inputs.Gameplay.Get().FindAction(id), layout);

        public static void SwitchBinding(InputAction action, BMSKeyLayout layout) {
            if(action == null) return;
            action.RemoveAllBindingOverrides();
            if(bindings.TryGetValue(layout, out var mapping) && mapping.TryGetValue(action.id, out var path))
                action.ApplyBindingOverride(path);
            else if(bindings.TryGetValue(layout.GetFallbackLayout(), out mapping) && mapping.TryGetValue(action.id, out path))
                action.ApplyBindingOverride(path);
            else if(bindings.TryGetValue(BMSKeyLayout.None, out mapping) && mapping.TryGetValue(action.id, out path))
                action.ApplyBindingOverride(path);
        }

        public static void ApplyBinding(Guid id, bool all = false) =>
            ApplyBinding(inputs.Gameplay.Get().FindAction(id), all);

        public static void ApplyBinding(InputAction action, bool all = false) {
            if(action == null) return;
            var bindingPath = GetBindingPath(action.bindings[0]);
            if(all) {
                foreach(var mapping in bindings.Values)
                    mapping.Remove(action.id);
                bindings.GetOrConstruct(BMSKeyLayout.None)[action.id] = bindingPath;
            } else {
                if(bindings.TryGetValue(currentLayout, out var mapping))
                    mapping[action.id] = bindingPath;
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

        public static string GetBindingPath(InputBinding binding) {
            var overridePath = binding.overridePath;
            return string.IsNullOrWhiteSpace(overridePath) ? binding.path : overridePath;
        }
    }
}
