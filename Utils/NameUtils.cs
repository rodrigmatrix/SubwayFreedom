using System;
using Game.SceneFlow;
using Game.UI;
using HarmonyLib;

namespace SubwayFreedomAssetPack.Utils
{
    public static class NameSystemExtensions
    {
        public static AccessTools.StructFieldRef<NameSystem.Name, NameSystem.NameType> fieldRefNameType =
            HarmonyLib.AccessTools.StructFieldRefAccess<NameSystem.Name, NameSystem.NameType>("m_NameType");

        public static AccessTools.StructFieldRef<NameSystem.Name, string> fieldRefNameId =
            HarmonyLib.AccessTools.StructFieldRefAccess<NameSystem.Name, string>("m_NameID");

        public static AccessTools.StructFieldRef<NameSystem.Name, string[]> fieldRefNameArgs =
            HarmonyLib.AccessTools.StructFieldRefAccess<NameSystem.Name, string[]>("m_NameArgs");

        public static NameSystem.NameType GetNameType(this NameSystem.Name name) => fieldRefNameType(ref name);
        public static string GetNameID(this NameSystem.Name name) => fieldRefNameId(ref name);
        public static string[] GetNameArgs(this NameSystem.Name name) => fieldRefNameArgs(ref name);

        public static string Translate(this NameSystem.Name name)
        {
            try
            {
                var type = name.GetNameType();
                var nameId = name.GetNameID();
                if (string.IsNullOrEmpty(nameId))
                {
                    return "";
                }
                switch (type)
                {
                    default:
                    case NameSystem.NameType.Custom:
                        return nameId;
                    case NameSystem.NameType.Localized:
                        return GameManager.instance.localizationManager.activeDictionary.TryGetValue(nameId,
                            out var value)
                            ? value
                            : nameId;
                    case NameSystem.NameType.Formatted:
                        var activeDictionary = GameManager.instance.localizationManager.activeDictionary;
                        var format = activeDictionary.TryGetValue(nameId, out var value2) ? value2 : nameId;
                        var args = name.GetNameArgs();
                        for (var i = 0; i < args.Length; i += 2)
                        {
                            format = format.Replace($"{{{args[i]}}}",
                                activeDictionary.TryGetValue(args[i + 1], out value2) ? value2 : args[i + 1]);
                        }
                        return format;
                }
            }
            catch (Exception e)
            {
                Mod.log.Info(e);
                return "";
            }
        }
    }
}
