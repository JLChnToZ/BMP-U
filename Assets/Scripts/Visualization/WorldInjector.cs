using System;
using UnityEngine;
using Unity.Entities;
using UniRx.Async;
using System.Text;

namespace BananaBeats.Visualization {
    public class WorldInjector: MonoBehaviour {
        World world;
        public static event Action OnInitWorld;

        public void Awake() {
            world = new World("BMSNoteRain");
            var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default);
            var sb = new StringBuilder();
            foreach(var system in systems)
                sb.AppendLine(system.ToString());
            Debug.Log($"System Scanned:\n{sb}");
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);
            ComponentSystemScheduler.Create<InitializationSystemGroup>(world, PlayerLoopTiming.Initialization);
            ComponentSystemScheduler.Create<SimulationSystemGroup>(world, PlayerLoopTiming.Update);
            ComponentSystemScheduler.Create<PresentationSystemGroup>(world, PlayerLoopTiming.PreLateUpdate);
            NoteDisplayManager.World = world;
            OnInitWorld?.Invoke();
        }

        public void OnDestroy() {
            try {
                if(world != null && world.IsCreated)
                    world.Dispose();
            } catch {}
            if(NoteDisplayManager.World == world)
                NoteDisplayManager.World = null;
            world = null;
        }
    }

    internal class ComponentSystemScheduler: IPlayerLoopItem {
        private readonly World world;
        private readonly ComponentSystemBase componentSystem;

        protected ComponentSystemScheduler(World world, ComponentSystemBase componentSystem) {
            this.world = world;
            this.componentSystem = componentSystem;
        }

        public bool MoveNext() {
            componentSystem.Update();
            return world.IsCreated;
        }

        public static ComponentSystemScheduler Create<T>(World world, PlayerLoopTiming timing)
            where T : ComponentSystemBase {
            var scheduler = new ComponentSystemScheduler(world, world.GetOrCreateSystem<T>());
            PlayerLoopHelper.AddAction(timing, scheduler);
            return scheduler;
        }
    }
}
