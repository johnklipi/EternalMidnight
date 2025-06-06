using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;
using UnityEngine;

namespace EternalMidnight;

public static class Main
{
    public static ManualLogSource? modLogger;
    private const float DARKENING_FACTOR = 0.65f;
    public static void Load(ManualLogSource logger)
    {
        modLogger = logger;
        Harmony.CreateAndPatchAll(typeof(Main));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Tile), nameof(Tile.GetVisualSkinTypeForTile))]
    private static void Tile_GetVisualSkinTypeForTile(ref SkinType __result, Tile __instance)
    {
        if (__result == SkinType.DarkElf && __instance.data.climate != 13)
        {
            __result = __instance.data.Skin;
        }
    }

    // [HarmonyPostfix]
    // [HarmonyPatch(typeof(ClientInteraction), nameof(ClientInteraction.UpdateMouseInput))]
    private static void ClientInteraction_UpdateMouseInput()
    {
        if (!InputManager.HasTouches())
        {
            WorldMouseCatcher.StopSunriseTimer();
        }
        if (SystemManager.IsMobile && !InputManager.HasTouches())
        {
            return;
        }
        if (InputManager.HasTouches())
        {
            SunriseBg.ToggleSunrise();
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Tile), nameof(Tile.RenderTerrain))]
    private static void Tile_RenderTerrain(Tile __instance, SkinVisualsTransientData transientSkinningData)
    {
        MakeTwilighted(__instance.forestRenderer, __instance.data, true);
        MakeTwilighted(__instance.mountainRenderer, __instance.data, true);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Resource), nameof(Resource.UpdateObject), typeof(SkinVisualsTransientData))]
    private static bool Resource_UpdateObject_Prefix(Resource __instance, SkinVisualsTransientData transientSkinData)
    {
        if (transientSkinData.tileOwnerSettings.skin == SkinType.DarkElf && __instance.data.type == ResourceData.Type.Game)
        {
            transientSkinData.tileClimateSettings = new TribeAndSkin(transientSkinData.tileClimateSettings.tribe, SkinType.DarkElf, transientSkinData.tileOwnerSettings.color);
        }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Resource), nameof(Resource.UpdateObject), typeof(SkinVisualsTransientData))]
    private static void Resource_UpdateObject(Resource __instance, SkinVisualsTransientData transientSkinData)
    {
        if (__instance.tile.data.owner > 0 && GameManager.GameState.TryGetPlayer(__instance.tile.data.owner, out PlayerState playerState))
        {
            if (playerState.skinType == SkinType.DarkElf)
            {
                MakeTwilighted(__instance.spriteRenderer, null);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TerrainRenderer), nameof(TerrainRenderer.UpdateGraphics))]
    private static void TerrainRenderer_UpdateGraphics(TerrainRenderer __instance, Tile tile, TribeData.Type climate, SkinType skin, bool shouldDesaturate)
    {
        if(!tile.data.IsWater)
            MakeTwilighted(__instance.SpriteRenderer, tile.data, true);
    }

    private static bool MakeTwilighted(PolytopiaSpriteRenderer renderer, TileData? tileData, bool makeMidnighted = false)
    {
        if (tileData == null)
        {
            if (makeMidnighted)
            {
                Color original = TerrainMaterialHelper.MOONSHINE;
                Color MIDNIGHT = new Color(
                    original.r * (DARKENING_FACTOR * 2),
                    original.g * DARKENING_FACTOR,
                    original.b,
                    original.a
                );
                TerrainMaterialHelper.SetSpriteTint(renderer, MIDNIGHT);
            }
            else
            {
                TerrainMaterialHelper.SetSpriteTint(renderer, TerrainMaterialHelper.MOONSHINE);
            }
            return true;
        }
        if (tileData.owner > 0 && GameManager.GameState.TryGetPlayer(tileData.owner, out PlayerState playerState))
        {
            if (playerState.skinType == SkinType.DarkElf && tileData.climate != 13)
            {
                if (makeMidnighted)
                {
                    Color original = TerrainMaterialHelper.MOONSHINE;
                    Color MIDNIGHT = new Color(
                        original.r * DARKENING_FACTOR,
                        original.g * DARKENING_FACTOR,
                        original.b,
                        original.a
                    );
                    TerrainMaterialHelper.SetSpriteTint(renderer, MIDNIGHT);
                }
                else
                {
                    TerrainMaterialHelper.SetSpriteTint(renderer, TerrainMaterialHelper.MOONSHINE);
                }
                return true;
            }
        }
        return false;
    }
}
