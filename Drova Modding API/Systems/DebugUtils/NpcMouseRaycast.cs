
using UnityEngine;
using MelonLoader;
using Il2CppDrova;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace Drova_Modding_API.Systems.DebugUtils
{
    /// <summary>
    /// A class that casts a ray from the mouse position and detects if it hits an NPC.
    /// </summary>
    /// <param name="ptr">Do not instantiate with new</param>
    [RegisterTypeInIl2Cpp]
    public class NpcMouseRaycast(IntPtr ptr) : MonoBehaviour(ptr)
    {
#if DEBUG
        private readonly Il2CppStructArray<RaycastHit2D> hits = new RaycastHit2D[10]; // Pre-allocated array for raycast results.
        private readonly string[] IGNORED_LAYERs = ["VisibleTrigger", "HitReceiver_CombatMusic", "HitReceiver_GroupEntities"]; // Layer to ignore when casting ray.


        internal void Update()
        {
            // Detect left mouse button click.
            if (Input.GetMouseButtonDown(0))
            {
                CastRayFromMouse();
            }
        }

        void CastRayFromMouse()
        {
            // Convert mouse position to world coordinates.
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePosition2D = new(mousePosition.x, mousePosition.y);

            // Cast a ray from the mouse position.
            int hitCount = Physics2D.RaycastNonAlloc(mousePosition2D, Vector2.down, hits, 10f);


            // Check if the ray hits something.
            if (hitCount > 0)
            {
                for (int i = 0; i < hitCount; i++)
                {
                    RaycastHit2D hit = hits[i];
                    MelonLogger.Msg("Hit Object: " + hit.collider.name);
                    if (IGNORED_LAYERs.Any((ignore) => hit.collider.name == ignore)) continue;
                    var npc = hit.collider.GetComponent<Actor>();
                    // Npc Shadow
                    if (npc != null)
                    {
                        MelonLogger.Msg("NPC Name: " + npc.name);
                        DebugManager.TriggerNpcSelected(npc);
                        return;
                    }
                    npc = hit.collider.transform.parent.GetComponent<Actor>();
                    // NPC trigger colliders
                    if(npc != null)
                    {
                        DebugManager.TriggerNpcSelected(npc);
                        MelonLogger.Msg("NPC Name hit 2: " + npc.name);
                        return;
                    }

                }
            }
        }
#endif
    }
}
