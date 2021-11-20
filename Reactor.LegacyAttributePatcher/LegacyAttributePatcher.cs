using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Preloader.Core.Patching;
using HarmonyLib;
using Mono.Cecil;
using MonoMod.Utils;
using Version = SemanticVersioning.Version;

namespace Reactor.LegacyAttributePatcher
{
    [PatcherPluginInfo("gg.reactor.LegacyAttributePatcher", "LegacyAttributePatcher", "1.0.0")]
    public class LegacyAttributePatcher : BasePatcher
    {
        public override void Initialize()
        {
            Harmony.CreateAndPatchAll(typeof(LegacyAttributePatcher));
        }

        private static readonly MethodInfo _nameSetter = AccessTools.PropertySetter(typeof(BepInPlugin), "Name");
        private static readonly MethodInfo _versionSetter = AccessTools.PropertySetter(typeof(BepInPlugin), "Version");

        private static readonly Func<string, Version> _tryParseLongVersion
            = AccessTools
              .Method(typeof(BepInPlugin), "TryParseLongVersion")
              .CreateDelegate<Func<string, Version>>();

        [HarmonyPatch(typeof(BepInPlugin), "FromCecilType")]
        [HarmonyPostfix]
        public static void FromCecilTypePatch(TypeDefinition td, ref BepInPlugin __result)
        {
            var assembly = td.Module.Assembly;

            if (__result.Name == null)
            {
                var name = assembly.Name.Name;

                var nameAttribute = assembly.GetCustomAttribute(typeof(AssemblyTitleAttribute).FullName);
                if (nameAttribute != null && nameAttribute.ConstructorArguments.Count == 1)
                {
                    name = (string)nameAttribute.ConstructorArguments.Single().Value;
                }

                _nameSetter.Invoke(__result, new object[] { name });
            }

            if (__result.Version == null)
            {
                var version = assembly.Name.Version.ToString();

                var versionAttribute =
                    assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute).FullName);
                if (versionAttribute != null && versionAttribute.ConstructorArguments.Count == 1)
                {
                    version = (string)versionAttribute.ConstructorArguments.Single().Value;
                }

                _versionSetter.Invoke(__result, new object[] { _tryParseLongVersion(version) });
            }
        }

        [HarmonyPatch(typeof(MetadataHelper), nameof(MetadataHelper.GetMetadata), typeof(Type))]
        [HarmonyPostfix]
        public static void GetMetadataPatch(Type pluginType, BepInPlugin __result)
        {
            var assembly = pluginType.Assembly;

            if (__result.Name == null)
            {
                var name = assembly.GetName().Name;

                var nameAttribute = MetadataHelper.GetAttributes<AssemblyTitleAttribute>(assembly).Single();
                if (nameAttribute != null)
                {
                    name = nameAttribute.Title;
                }

                _nameSetter.Invoke(__result, new object[] { name });
            }

            if (__result.Version == null)
            {
                var version = assembly.GetName().Version.ToString();

                var versionAttribute = MetadataHelper.GetAttributes<AssemblyInformationalVersionAttribute>(assembly)
                                                     .Single();
                if (versionAttribute != null)
                {
                    version = versionAttribute.InformationalVersion;
                }

                _versionSetter.Invoke(__result, new object[] { _tryParseLongVersion(version) });
            }
        }
    }
}
