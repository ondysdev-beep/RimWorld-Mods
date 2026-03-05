using System.Collections.Generic;
using System.Linq;
using RimVerse.Shared.Crypto;
using Verse;

namespace RimVerse.Client.Compat
{
    public static class ModHasher
    {
        public static string ComputeCurrentModpackHash()
        {
            var modIds = GetActiveModPackageIds();
            return HashHelper.ComputeModpackHash(modIds);
        }

        public static string[] GetActiveModPackageIds()
        {
            var mods = LoadedModManager.RunningMods;
            var ids = new List<string>();
            foreach (var mod in mods)
            {
                if (mod.PackageId != null)
                    ids.Add(mod.PackageId.ToLowerInvariant());
            }
            return ids.ToArray();
        }

        public static List<string> CompareWithManifest(string[] requiredMods, string[] currentMods)
        {
            var missing = new List<string>();
            var currentSet = new HashSet<string>(currentMods.Select(m => m.ToLowerInvariant()));

            foreach (var required in requiredMods)
            {
                if (!currentSet.Contains(required.ToLowerInvariant()))
                    missing.Add(required);
            }

            return missing;
        }
    }
}
