using System;
using System.Collections.Generic;
using Sevenfold.Core;
using UnityEngine;

namespace Sevenfold.Dice
{
    public readonly struct DiceResult : IEquatable<DiceResult>
    {
        public int A { get; }
        public int B { get; }
        public int Total { get; }

        public DiceResult(int a, int b)
        {
            A = a;
            B = b;
            Total = a + b;
        }

        public override string ToString() => $"DiceResult({A}, {B}, Total={Total})";

        public bool Equals(DiceResult other) => A == other.A && B == other.B && Total == other.Total;

        public override bool Equals(object obj) => obj is DiceResult other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(A, B, Total);

        public static bool operator ==(DiceResult left, DiceResult right) => left.Equals(right);

        public static bool operator !=(DiceResult left, DiceResult right) => !left.Equals(right);
    }

    /// <summary>
    /// Pure C# dice generation service. Generates roll outcomes using System.Random or forced rolls.
    /// Logic is completely decoupled from visual representation.
    /// </summary>
    public class DiceService
    {
        private readonly GameConfigSO _config;
        private readonly System.Random _rng;
        private readonly Queue<Vector2Int> _forcedRolls = new Queue<Vector2Int>();

        public int ForcedRollCount => _forcedRolls.Count;

        public DiceService(GameConfigSO config)
        {
            _config = config;

            int seed = (config != null && config.UseFixedSeed)
                ? config.FixedSeed
                : Environment.TickCount;

            _rng = new System.Random(seed);

            if (config != null && config.ForcedRolls != null)
            {
                for (int i = 0; i < config.ForcedRolls.Count; i++)
                {
                    _forcedRolls.Enqueue(config.ForcedRolls[i]);
                }
            }
        }

        /// <summary>
        /// Enqueues a deterministic roll outcome for testing and validation.
        /// </summary>
        public void EnqueueForcedRoll(int a, int b)
        {
            _forcedRolls.Enqueue(new Vector2Int(a, b));
        }

        /// <summary>
        /// Clears any queued forced rolls.
        /// </summary>
        public void ClearForcedRolls()
        {
            _forcedRolls.Clear();
        }

        /// <summary>
        /// Rolls two dice. Dequeues next forced roll if available, otherwise draws randomly from System.Random.
        /// Logs "Dice: A + B = S" only when Debug.isDebugBuild.
        /// </summary>
        public DiceResult Roll()
        {
            int a, b;

            if (_forcedRolls.Count > 0)
            {
                var forced = _forcedRolls.Dequeue();
                a = forced.x;
                b = forced.y;
            }
            else
            {
                int sides = (_config != null && _config.DiceSides >= 2) ? _config.DiceSides : 6;
                a = _rng.Next(1, sides + 1);
                b = _rng.Next(1, sides + 1);
            }

            var result = new DiceResult(a, b);

            if (Debug.isDebugBuild)
            {
                Debug.Log($"Dice: {result.A} + {result.B} = {result.Total}");
            }

            return result;
        }
    }
}
