using System;
using BepInEx;
using HarmonyLib;
using System.Collections.Generic;

using Assets.Scripts.Objects;
using Assets.Scripts.Sound;
using BepInEx.Configuration;

namespace MisophoniaMod;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class MisophoniaModPlugin : BaseUnityPlugin
{
    public const string PluginGuid = ThisModInfo.ModID;
    public const string PluginName = ThisModInfo.AssemblyName;
    public const string PluginVersion = ThisModInfo.Version;
    public const string PluginVersionGit = ThisModInfo.VersionGit;
    public static readonly string PluginBuildTime = DateTime.Parse(ThisModInfo.BuildTime).ToLocalTime().ToString("o");

    private Harmony _harmony;

    public static readonly HashSet<int> MutedAudioClipIds = [];
    public static readonly Dictionary<string, int> AllowedAudioClipIds = GetAllowedAudioClipIds();
    public static readonly Dictionary<string, ConfigEntry<bool>> ConfigEntries = [];

    private void Awake()
    {
        try
        {
            L.SetLogger(this.Logger);
            L.Info($"Awake {ThisModInfo.Info}");
            L.Info(
                $"[{PluginName}] Awake (Guid: {PluginGuid}, Version: {PluginVersionGit}, BuildTime: {PluginBuildTime})"
            );

            BindAllConfigs();

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();
        }
        catch (Exception ex)
        {
            L.Error($"[{PluginName}] Error during Awake: {ex}");
        }
    }

    private void OnDestroy()
    {
        L.Info($"OnDestroy of ${ThisModInfo.Info}, cleaning up patches");

#if DEBUG
        try
        {
            if (!ModUtils.IsLoadedByScriptEngine(typeof(MisophoniaModPlugin)))
                return;

            _harmony.UnpatchSelf();
        }
        catch (Exception ex)
        {
            L.Error(
                $"[{PluginName}] Error while unpatching Harmony in {nameof(OnDestroy)}: {ex}"
            );
        }
        finally
        {
            _harmony = null;
        }
#endif
    }

    static Dictionary<string, int> GetDefaultMutedAudioClipIds()
    {
        var defaultMuted = new Dictionary<string, int>
        {
            {"Eating", Item.EatingHash },
            {"EatingFinished", Item.EatingFinishedHash },
            {"Drink", Item.DrinkingHash },
            {"DrinkFinished", Item.DrinkingFinishedHash }
        };
        return defaultMuted;
    }

    static Dictionary<string, int> GetAllowedAudioClipIds()
    {
        var ids = GetDefaultMutedAudioClipIds();

        var additionalIds = new Dictionary<string, int>
        {
            { "WaterBottleFill", Item.WaterBottleFillHash },
            { "EquipSoundDefault", Item.EquipSoundDefaultHash },
            { "UnEquipSoundDefault", Item.UnEquipSoundDefaultHash },
            { "DuctTapeUsing", DuctTape.DuctTapeUsingHash },
            { "DuctTapeFinished", DuctTape.DuctTapeFinishedHash },
            { "PipeStressHash", AtmosphericAudioHandler.PipeStressHash},
            { "MetalWallStressHash", AtmosphericAudioHandler.MetalWallStressHash},
            { "CompositeWallStressHash", AtmosphericAudioHandler.CompositeWallStressHash},
           	{ "GlassWallStressHash", AtmosphericAudioHandler.GlassWallStressHash}
        };

        foreach (var id in additionalIds)
            ids.Add(id.Key, id.Value);

        // scan all valid fields in Assets.Scripts.Utils.Defines.Sound 
        var soundType = typeof(Assets.Scripts.Util.Defines.Sounds);
        var fields = soundType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        foreach (var field in fields)
        {
            if (field.FieldType == typeof(int))
            {
                var value = (int)field.GetValue(null);
                ids[field.Name] = value;
            }
        }
        return ids;
    }


    private void BindAllConfigs()
    {
        MutedAudioClipIds.Clear();
        var defaultMutedIds = GetDefaultMutedAudioClipIds();
        ConfigEntries.Clear();

        foreach (var kvp in AllowedAudioClipIds)
        {
            var clipName = kvp.Key;
            if (clipName.EndsWith("Hash"))
                clipName = clipName.Substring(0, clipName.Length - 4);
            if (clipName.EndsWith("Default"))
                clipName = clipName.Substring(0, clipName.Length - 7);

            var clipId = kvp.Value;

            ConfigEntries[kvp.Key] = Config.Bind(
                "MutedAudioClips",
                clipName,
                defaultMutedIds.ContainsKey(kvp.Key),
                $"Mute the audio clip '{clipName}' (ID: {clipId})"
            );
        }
        LoadConfig();
    }

    public static void LoadConfig()
    {
        L.Info($"Loading config for {ThisModInfo.Info}");
        MutedAudioClipIds.Clear();
        foreach (var kvp in ConfigEntries)
        {
            if (kvp.Value.Value)
            {
                if (AllowedAudioClipIds.TryGetValue(kvp.Key, out int clipId))
                {
                    MutedAudioClipIds.Add(clipId);
                }
            }

        }
    }
}
