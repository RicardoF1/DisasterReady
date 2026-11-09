using System;
using System.Collections.Generic;
using UnityEngine;

namespace DisasterReady
{
    public enum GameState { Menu, Preparation, Alarm, Earthquake, Evacuation, Result }

    [Serializable]
    public struct ItemData
    {
        public string Id, Name;
        public int Size, Points;
        public ItemData(string id, string name, int size, int points)
        { Id = id; Name = name; Size = size; Points = points; }
    }

    public interface IBackpack
    {
        int Used { get; }
        int Capacity { get; }
        IReadOnlyList<ItemData> Items { get; }
        bool Contains(string id);
        bool TryAdd(ItemData item);
    }

    public interface IPlayerAgent
    {
        Vector3 Position { get; }
        bool Crouched { get; }
        bool Holding { get; }
        bool Running { get; }
        IBackpack Backpack { get; }
    }

    public interface IWorldQuery
    {
        bool IsSheltered(Vector3 feet);
        bool NearGlass(Vector3 feet);
    }

    public interface IInteractable
    {
        string Prompt { get; }
        void Interact(IPlayerAgent player);
    }

    // Solo notificaciones: las reglas y las mutaciones pertenecen a GameManager.
    public static class GameEvents
    {
        public static event Action<GameState> OnStateChanged;
        public static event Action<int> OnScoreChanged;
        public static event Action<float> OnPlayerHit;
        public static event Action<ItemData> OnItemAdded;
        public static event Action<string> OnMessage;
        public static event Action<bool> OnTremorChanged;
        public static event Action<string> OnSound;
        public static void State(GameState value) => OnStateChanged?.Invoke(value);
        public static void Score(int value) => OnScoreChanged?.Invoke(value);
        public static void Hit(float value) => OnPlayerHit?.Invoke(value);
        public static void Item(ItemData value) => OnItemAdded?.Invoke(value);
        public static void Message(string value) => OnMessage?.Invoke(value);
        public static void Tremor(bool value) => OnTremorChanged?.Invoke(value);
        public static void Sound(string value) => OnSound?.Invoke(value);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            OnStateChanged = null; OnScoreChanged = null; OnPlayerHit = null;
            OnItemAdded = null; OnMessage = null; OnTremorChanged = null; OnSound = null;
        }
    }
}
