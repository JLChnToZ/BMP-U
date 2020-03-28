using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Entities;
using UnityEngine.LowLevel;

namespace BananaBeats.Visualization {
    public class WorldInjector: MonoBehaviour {
        World world;
        public static event Action OnInitWorld;

        public void Awake() {
            world = new World("BMSNoteRain");
            var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default);
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);
            ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world, PlayerLoop.GetCurrentPlayerLoop());
            NoteDisplayManager.World = world;
            OnInitWorld?.Invoke();
        }

        public void OnDestroy() {
            try {
                if(world != null)
                    world.Dispose();
            } catch {}
            if(NoteDisplayManager.World == world)
                NoteDisplayManager.World = null;
            world = null;
        }
    }
}
