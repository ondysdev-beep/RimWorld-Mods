using System;
using HarmonyLib;
using RimVerse.Client.Session;
using Verse;

namespace RimVerse.Client.Patches
{
    [HarmonyPatch(typeof(Rand), nameof(Rand.Range), typeof(int), typeof(int))]
    public static class Patch_Rand_RangeInt
    {
        static bool Prefix(int minInclusive, int maxExclusive, ref int __result)
        {
            if (JointSessionManager.IsInSession)
            {
                __result = JointSessionManager.SessionRNG.Next(minInclusive, maxExclusive);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Rand), nameof(Rand.Range), typeof(float), typeof(float))]
    public static class Patch_Rand_RangeFloat
    {
        static bool Prefix(float minInclusive, float maxInclusive, ref float __result)
        {
            if (JointSessionManager.IsInSession)
            {
                __result = (float)(JointSessionManager.SessionRNG.NextDouble() * (maxInclusive - minInclusive) + minInclusive);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Rand), nameof(Rand.Value), MethodType.Getter)]
    public static class Patch_Rand_Value
    {
        static bool Prefix(ref float __result)
        {
            if (JointSessionManager.IsInSession)
            {
                __result = (float)JointSessionManager.SessionRNG.NextDouble();
                return false;
            }
            return true;
        }
    }
}
