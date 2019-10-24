using System;
using UniRx.Async;

namespace BananaBeats.Utils {
    public interface IGameLoopItem {
        bool DisposeRequested { get; }

        void Update();
    }

    public abstract class GameLoopItem: IGameLoopItem, IDisposable {
        private bool disposeRequested;

        bool IGameLoopItem.DisposeRequested => disposeRequested;

        public abstract void Update();

        public virtual void Dispose() => disposeRequested = true;

        public static void Run<T>(PlayerLoopTiming timing = PlayerLoopTiming.Update)
            where T : IGameLoopItem, new() =>
            PlayerLoopHelper.AddAction(timing, new GameLoopAdaptor(new T()));
    }

    internal class ContinurusAction: GameLoopItem {
        private readonly Action updateFunction;

        public ContinurusAction(Action updateFunction) =>
            this.updateFunction = updateFunction ??
            throw new ArgumentNullException(nameof(updateFunction));

        public override void Update() => updateFunction();
    }

    public struct GameLoopAdaptor: IPlayerLoopItem, IEquatable<GameLoopAdaptor> {
        public readonly IGameLoopItem gameLoopItem;

        public GameLoopAdaptor(IGameLoopItem gameLoopItem) =>
            this.gameLoopItem = gameLoopItem ??
            throw new ArgumentNullException(nameof(gameLoopItem));

        public bool MoveNext() {
            if(gameLoopItem == null || gameLoopItem.DisposeRequested)
                return false;
            gameLoopItem.Update();
            return !gameLoopItem.DisposeRequested;
        }

        public void Register(PlayerLoopTiming timing = PlayerLoopTiming.Update) =>
            PlayerLoopHelper.AddAction(timing, this);

        public bool Equals(GameLoopAdaptor other) =>
            Equals(gameLoopItem, other.gameLoopItem);

        public override bool Equals(object obj) =>
            obj is GameLoopAdaptor other && Equals(other);

        public override int GetHashCode() {
            if(gameLoopItem == null) return 0;
            var hashCode = gameLoopItem.GetHashCode();
            return unchecked((hashCode << 16) ^ (hashCode >> 16));
        }
    }

    public static class GameLoop {
        public static void Run(
            this IGameLoopItem gameLoopItem, PlayerLoopTiming timing = PlayerLoopTiming.Update) =>
            PlayerLoopHelper.AddAction(timing, new GameLoopAdaptor(gameLoopItem));

        public static IDisposable RunAsUpdate(
            this Action action, PlayerLoopTiming timing = PlayerLoopTiming.Update) {
            var continurus = new ContinurusAction(action);
            continurus.Run(timing);
            return continurus;
        }
    }
}