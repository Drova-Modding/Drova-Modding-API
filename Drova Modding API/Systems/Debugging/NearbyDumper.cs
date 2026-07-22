using Drova_Modding_API.Access;
using Il2CppCommandTerminal;
using Il2CppDrova;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Debugging
{
    /// <summary>
    /// Registers the <c>api_dumpnearby [radius]</c> cheat command: stand next to a world object
    /// and dump every renderer around the player (position, GameObject name, sprite name) to the
    /// MelonLoader log.
    ///
    /// Useful for locating world art that is hard to find statically (it can sit far from the
    /// object it belongs to). Sprite names are the stable key for identifying art; GameObject
    /// names are often generic.
    /// </summary>
    public static class NearbyDumper
    {
        private const float DefaultRadius = 120f;
        private static bool _registered;

        /// <summary>
        /// Register the cheat command (queued until cheat mode is enabled). Idempotent.
        /// </summary>
        public static void Register()
        {
            if (_registered)
            {
                return;
            }
            _registered = true;
            CheatMenuAccess.RegisterCheat(
                "api_dumpnearby",
                (Action<Il2CppReferenceArray<CommandArg>>)OnDumpNearby,
                0,
                1,
                "api_dumpnearby [radius]",
                "Log renderers near the player (for identifying world objects)");
        }

        private static void OnDumpNearby(Il2CppReferenceArray<CommandArg> args)
        {
            try
            {
                float radius = args.Length > 0 ? args[0].Float : DefaultRadius;
                if (!PlayerAccess.TryGetPlayer(out Actor player))
                {
                    MelonLogger.Warning("api_dumpnearby: no player yet.");
                    return;
                }
                Vector2 origin = player.GetFeetPosition();
                MelonLogger.Msg($"[dumpnearby] objects within {radius} of ({origin.x:F0}, {origin.y:F0}):");

                int shown = 0;
                foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(Il2CppType.Of<Renderer>()))
                {
                    Renderer? renderer = obj == null ? null : obj.TryCast<Renderer>();
                    if (renderer == null || !renderer.gameObject.scene.IsValid())
                    {
                        continue;
                    }
                    Vector3 pos = renderer.transform.position;
                    if (Mathf.Abs(pos.x - origin.x) > radius || Mathf.Abs(pos.y - origin.y) > radius)
                    {
                        continue;
                    }
                    SpriteRenderer? spriteRenderer = renderer.TryCast<SpriteRenderer>();
                    string detail = spriteRenderer != null
                        ? "sprite=" + (spriteRenderer.sprite != null ? spriteRenderer.sprite.name : "<none>")
                        : "type=" + renderer.GetIl2CppType().Name;
                    MelonLogger.Msg($"[dumpnearby] ({pos.x:F0},{pos.y:F0}) go='{renderer.gameObject.name}' {detail}");
                    shown++;
                }
                MelonLogger.Msg($"[dumpnearby] {shown} renderer(s). Log file has the full list.");
            }
            catch (Exception e)
            {
                MelonLogger.Error("api_dumpnearby failed: " + e);
            }
        }
    }
}
