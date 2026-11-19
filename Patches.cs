using Assets.Scripts;
using Assets.Scripts.Sound;
using HarmonyLib;
using Sound;
using System;

namespace MisophoniaMod;

// todo: fix ambigious overload, use this variant: 	public PooledAudioSource PlayAudioClipsData(IAudioParent parent, int clipsDataHash, Vector3 localPosition, ChannelData channelData = null, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
[HarmonyPatch(typeof(AudioManager), nameof(AudioManager.PlayAudioClipsData), new Type[] { typeof(Audio.IAudioParent), typeof(int), typeof(UnityEngine.Vector3), typeof(ChannelData), typeof(float), typeof(float) })]
public static class Patch_AudioManager_PlayAudioClipsData
{
    [HarmonyPrefix]
    public static bool Prefix(AudioManager __instance, ref int clipsDataHash, ref float volumeMultiplier, ref PooledAudioSource __result)
    {
        if (MisophoniaModPlugin.MutedAudioClipIds.Contains(clipsDataHash))
        {
            L.Info($"Muted audio clip with hash {clipsDataHash} (volume multiplier: {volumeMultiplier})");
            __result = null; // Method should return null
            return false; // Skip original method
        }
        return true;
    }
}

[HarmonyPatch(typeof(GameManager), nameof(GameManager.UpdateThingsOnGameStart))]
public static class Patch_ReloadConfigOnGameStart
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        MisophoniaModPlugin.LoadConfig();
    }
}