using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SimpleGame
{
    public readonly struct PrefabPoolDiagnostics
    {
        public PrefabPoolDiagnostics(
            int createdCount,
            int reusedCount,
            int releasedCount,
            int discardedCount,
            int managedCount,
            int inactiveCount,
            int maximumInactive)
        {
            CreatedCount = createdCount;
            ReusedCount = reusedCount;
            ReleasedCount = releasedCount;
            DiscardedCount = discardedCount;
            ManagedCount = managedCount;
            InactiveCount = inactiveCount;
            MaximumInactive = maximumInactive;
        }

        public int CreatedCount { get; }
        public int ReusedCount { get; }
        public int ReleasedCount { get; }
        public int DiscardedCount { get; }
        public int ManagedCount { get; }
        public int InactiveCount { get; }
        public int MaximumInactive { get; }
    }

    public static class ComponentPrefabPool<T>
        where T : Component
    {
        private static readonly Dictionary<T, PoolState> states = new();
        private static readonly Dictionary<T, PoolState> stateByInstance =
            new();
        private static readonly List<T> destroyedInstances = new();
        private static bool clearing;

        static ComponentPrefabPool()
        {
            ComponentPrefabPoolRuntime.Register(
                Clear,
                PruneDestroyedInstances);
        }

        public static T Acquire(T prefab, int maximumInactive)
        {
            if (prefab == null)
            {
                return null;
            }

            if (!states.TryGetValue(prefab, out PoolState state))
            {
                state = new PoolState();
                states.Add(prefab, state);
            }

            state.MaximumInactive = Mathf.Max(0, maximumInactive);
            TrimInactiveToLimit(state);
            T instance = null;
            while (state.Inactive.Count > 0 && instance == null)
            {
                T candidate = state.Inactive.Pop();
                state.InactiveSet.Remove(candidate);
                if (candidate == null)
                {
                    state.Managed.Remove(candidate);
                    stateByInstance.Remove(candidate);
                    continue;
                }

                instance = candidate;
            }

            if (instance == null)
            {
                instance = UnityEngine.Object.Instantiate(prefab);
                state.Managed.Add(instance);
                stateByInstance[instance] = state;
                state.CreatedCount++;
            }
            else
            {
                state.ReusedCount++;
            }

            instance.gameObject.SetActive(true);
            return instance;
        }

        public static void Release(T instance)
        {
            if (instance == null)
            {
                return;
            }

            if (!stateByInstance.TryGetValue(
                    instance,
                    out PoolState state))
            {
                Destroy(instance.gameObject);
                return;
            }

            if (!state.InactiveSet.Add(instance))
            {
                return;
            }

            state.ReleasedCount++;
            instance.gameObject.SetActive(false);
            if (state.Inactive.Count >= state.MaximumInactive)
            {
                state.InactiveSet.Remove(instance);
                state.Managed.Remove(instance);
                stateByInstance.Remove(instance);
                state.DiscardedCount++;
                Destroy(instance.gameObject);
                return;
            }

            state.Inactive.Push(instance);
        }

        public static PrefabPoolDiagnostics GetDiagnostics(T prefab)
        {
            PruneDestroyedInstances();
            if (prefab == null ||
                !states.TryGetValue(prefab, out PoolState state))
            {
                return default;
            }

            return new PrefabPoolDiagnostics(
                state.CreatedCount,
                state.ReusedCount,
                state.ReleasedCount,
                state.DiscardedCount,
                state.Managed.Count,
                state.InactiveSet.Count,
                state.MaximumInactive);
        }

        public static void Forget(T instance)
        {
            if (clearing || instance == null ||
                !stateByInstance.TryGetValue(
                    instance,
                    out PoolState state))
            {
                return;
            }

            stateByInstance.Remove(instance);
            state.Managed.Remove(instance);
            state.InactiveSet.Remove(instance);
        }

        public static void Clear()
        {
            clearing = true;
            foreach (PoolState state in states.Values)
            {
                foreach (T instance in state.Managed)
                {
                    if (instance != null)
                    {
                        instance.gameObject.SetActive(false);
                        Destroy(instance.gameObject);
                    }
                }
            }

            states.Clear();
            stateByInstance.Clear();
            destroyedInstances.Clear();
            clearing = false;
        }

        private static void PruneDestroyedInstances()
        {
            destroyedInstances.Clear();
            foreach (KeyValuePair<T, PoolState> pair in states)
            {
                if (pair.Key == null)
                {
                    destroyedInstances.Add(pair.Key);
                    continue;
                }

                PoolState state = pair.Value;
                state.PruneDestroyedInactive();
                state.Managed.RemoveWhere(instance => instance == null);
                state.InactiveSet.RemoveWhere(instance => instance == null);
            }

            foreach (T destroyedPrefab in destroyedInstances)
            {
                if (!states.TryGetValue(
                        destroyedPrefab,
                        out PoolState state))
                {
                    continue;
                }

                ReleaseState(state);
                states.Remove(destroyedPrefab);
            }

            destroyedInstances.Clear();
            foreach (T instance in stateByInstance.Keys)
            {
                if (instance == null)
                {
                    destroyedInstances.Add(instance);
                }
            }

            foreach (T instance in destroyedInstances)
            {
                stateByInstance.Remove(instance);
            }

            destroyedInstances.Clear();
        }

        private static void TrimInactiveToLimit(PoolState state)
        {
            while (state.Inactive.Count > state.MaximumInactive)
            {
                T instance = state.Inactive.Pop();
                state.InactiveSet.Remove(instance);
                state.Managed.Remove(instance);
                stateByInstance.Remove(instance);
                if (instance == null)
                {
                    continue;
                }

                state.DiscardedCount++;
                Destroy(instance.gameObject);
            }
        }

        private static void ReleaseState(PoolState state)
        {
            foreach (T instance in state.Managed)
            {
                stateByInstance.Remove(instance);
                if (instance != null)
                {
                    instance.gameObject.SetActive(false);
                    Destroy(instance.gameObject);
                }
            }

            state.Managed.Clear();
            state.InactiveSet.Clear();
            state.Inactive.Clear();
        }

        private static void Destroy(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private sealed class PoolState
        {
            public Stack<T> Inactive { get; } = new();
            public HashSet<T> InactiveSet { get; } = new();
            public HashSet<T> Managed { get; } = new();
            public int MaximumInactive { get; set; }
            public int CreatedCount { get; set; }
            public int ReusedCount { get; set; }
            public int ReleasedCount { get; set; }
            public int DiscardedCount { get; set; }

            public void PruneDestroyedInactive()
            {
                while (Inactive.Count > 0)
                {
                    T instance = Inactive.Pop();
                    if (instance != null)
                    {
                        pruneBuffer.Push(instance);
                    }
                }

                while (pruneBuffer.Count > 0)
                {
                    Inactive.Push(pruneBuffer.Pop());
                }
            }

            private readonly Stack<T> pruneBuffer = new();
        }
    }

    internal static class ComponentPrefabPoolRuntime
    {
        private static readonly List<Action> clearActions = new();
        private static readonly List<Action> pruneActions = new();

        internal static void Register(
            Action clear,
            Action prune)
        {
            if (!clearActions.Contains(clear))
            {
                clearActions.Add(clear);
            }

            if (!pruneActions.Contains(prune))
            {
                pruneActions.Add(prune);
            }
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPools()
        {
            foreach (Action clear in clearActions)
            {
                clear();
            }
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallSceneCleanup()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            foreach (Action prune in pruneActions)
            {
                prune();
            }
        }
    }
}
