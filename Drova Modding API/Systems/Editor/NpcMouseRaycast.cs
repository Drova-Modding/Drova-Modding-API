#if DEBUG
using Il2CppDrova;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Editor
{
    /// <summary>
    /// A class that casts a ray from the mouse position and detects if it _hits an NPC.
    /// </summary>
    /// <param name="ptr">Do not instantiate with new</param>
    [RegisterTypeInIl2Cpp]
    public class NpcMouseRaycast(IntPtr ptr) : MonoBehaviour(ptr)
    {
        private readonly Il2CppStructArray<RaycastHit2D> _hits = new RaycastHit2D[10]; // Pre-allocated array for raycast results.
        private readonly string[] _ignoredLayers = ["VisibleTrigger", "HitReceiver_CombatMusic", "HitReceiver_GroupEntities"]; // Layer to ignore when casting ray.
        private Camera _camera;
        
        internal void Awake()
        {
            _camera = Camera.main!;
        }

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
            Vector3 mousePosition = _camera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePosition2D = new(mousePosition.x, mousePosition.y);

            // Cast a ray from the mouse position.
            int hitCount = Physics2D.RaycastNonAlloc(mousePosition2D, Vector2.down, _hits, 10f);

            // Check if the ray _hits something.
            if (hitCount > 0)
            {
                for (int i = 0; i < hitCount; i++)
                {
                    RaycastHit2D hit = _hits[i];
                    if (_ignoredLayers.Any((ignore) => hit.collider.name == ignore)) continue;
                    Actor npc = hit.collider.GetComponent<Actor>();
                    // Npc Shadow
                    if (npc != null)
                    {
                        EditorManager.TriggerNpcSelected(npc);
                        return;
                    }
                    npc = hit.collider.transform.parent.GetComponent<Actor>();
                    // NPC trigger colliders
                    if (npc != null)
                    {
                        EditorManager.TriggerNpcSelected(npc);
                        return;
                    }
                }
            }
        }
    }
}
#endif
