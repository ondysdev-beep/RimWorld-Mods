using System;
using System.Collections.Generic;
using Verse;

namespace RimVerse.Client.Session
{
    public static class JointSessionManager
    {
        public static bool IsInSession { get; private set; }
        public static Random SessionRNG { get; private set; }
        public static string CurrentSessionId { get; private set; }
        public static long CurrentTick { get; private set; }
        public static DateTime DeterministicTime { get; private set; }

        private static readonly List<InputCommand> _pendingInputs = new List<InputCommand>();
        private static readonly Dictionary<long, uint> _tickHashes = new Dictionary<long, uint>();

        public static void StartSession(string sessionId, long rngSeed)
        {
            CurrentSessionId = sessionId;
            SessionRNG = new Random((int)rngSeed);
            CurrentTick = 0;
            DeterministicTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            IsInSession = true;
            _pendingInputs.Clear();
            _tickHashes.Clear();

            Log.Message($"[RimVerse] Joint session started: {sessionId}");
        }

        public static void EndSession()
        {
            Log.Message($"[RimVerse] Joint session ended: {CurrentSessionId}");
            IsInSession = false;
            CurrentSessionId = null;
            SessionRNG = null;
            _pendingInputs.Clear();
            _tickHashes.Clear();
        }

        public static void AdvanceTick()
        {
            if (!IsInSession) return;
            CurrentTick++;
            DeterministicTime = DeterministicTime.AddSeconds(1);
        }

        public static void EnqueueInput(InputCommand command)
        {
            if (!IsInSession) return;
            _pendingInputs.Add(command);
        }

        public static List<InputCommand> FlushInputs()
        {
            var copy = new List<InputCommand>(_pendingInputs);
            _pendingInputs.Clear();
            return copy;
        }

        public static void ReportTickHash(long tick, uint hash)
        {
            _tickHashes[tick] = hash;
        }

        public static uint? GetTickHash(long tick)
        {
            return _tickHashes.TryGetValue(tick, out var hash) ? hash : (uint?)null;
        }
    }

    public class InputCommand
    {
        public string Type { get; set; }
        public string PlayerId { get; set; }
        public string DataJson { get; set; }
        public long Tick { get; set; }
    }
}
