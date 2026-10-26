using System.Globalization;
using Il2CppDrova.Terrain;
using UnityEngine;

namespace Drova_Modding_API.Systems.MapEditor
{
    /// <summary>
    /// Ground painting: a stamp is a layout object that shows only its source's sprites, by default on the floor sorting
    /// layer under water, bridges and decals. The ground itself is baked into each chunk, so painting lays sprites on it.
    /// A stamp can also change the footstep sound inside its bounds.
    /// </summary>
    internal static class MapStamps
    {
        private const string DefaultLayer = "Floor";
        private const int DefaultOrder = -10;
        private const float TerrainCell = 4f;

        private static readonly Dictionary<long, TerrainType2D.EType> TerrainByCell = new();
        private static readonly Dictionary<string, List<long>> CellsById = new(StringComparer.OrdinalIgnoreCase);

        private static HarmonyLib.Harmony? _harmony;
        private static bool _terrainHooked;

        // The terrain hook waits for the first stamp with a terrain: GetTerrainAt runs per pathfinding node and footstep.
        internal static void Initialize(HarmonyLib.Harmony harmony)
        {
            _harmony = harmony;
        }

        /// <summary>
        /// A plain copy of the sprites of <paramref name="source"/>: no behaviours, colliders or saveables.
        /// </summary>
        internal static GameObject CopySprites(GameObject source, string name)
        {
            GameObject copy = new(name);
            Vector3 sourceScale = source.transform.lossyScale;
            foreach (SpriteRenderer renderer in source.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.sprite == null)
                {
                    continue;
                }

                GameObject part = new(renderer.name);
                part.transform.SetParent(copy.transform, false);
                part.transform.localPosition = renderer.transform.position - source.transform.position;
                Vector3 scale = renderer.transform.lossyScale;
                part.transform.localScale = new Vector3(scale.x / sourceScale.x, scale.y / sourceScale.y, 1f);
                SpriteRenderer sprite = part.AddComponent<SpriteRenderer>();
                sprite.sprite = renderer.sprite;
                sprite.color = renderer.color;
                sprite.flipX = renderer.flipX;
                sprite.flipY = renderer.flipY;
                sprite.sharedMaterial = renderer.sharedMaterial;
                sprite.sortingLayerID = renderer.sortingLayerID;
                sprite.sortingOrder = renderer.sortingOrder;
            }

            return copy;
        }

        /// <summary>
        /// The stamp of a definition, under <paramref name="parent"/>, with its sorting, tint and footstep sound applied.
        /// </summary>
        internal static GameObject Create(MapObjectDefinition definition, GameObject source, Transform parent)
        {
            GameObject stamp = CopySprites(source, "Map_" + definition.Id);
            stamp.transform.SetParent(parent, false);
            string layer = definition.SortingLayer ?? DefaultLayer;
            int order = definition.SortingOrder ?? DefaultOrder;
            Color? tint = ParseColor(definition.Color);
            foreach (SpriteRenderer renderer in stamp.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.sortingLayerName = layer;
                renderer.sortingOrder = order;
                if (tint is { } color)
                {
                    renderer.color = color;
                }
            }

            return stamp;
        }

        /// <summary>
        /// Marks the cells under the stamp's sprites with its footstep sound. Call after the stamp has its final transform.
        /// </summary>
        internal static void AddTerrain(MapObjectDefinition definition, GameObject stamp)
        {
            RemoveTerrain(definition.Id);
            if (string.IsNullOrEmpty(definition.Terrain) || !Enum.TryParse(definition.Terrain, true, out TerrainType2D.EType terrain))
            {
                return;
            }

            if (!_terrainHooked && _harmony != null)
            {
                _terrainHooked = true;
                Hooking.TryPostfix(_harmony, typeof(Terrain2D), nameof(Terrain2D.GetTerrainAt), typeof(MapStamps), nameof(GetTerrainAtPostfix));
            }

            List<long> cells = [];
            foreach (SpriteRenderer renderer in stamp.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Bounds bounds = renderer.bounds;
                for (int x = Mathf.FloorToInt(bounds.min.x / TerrainCell); x <= Mathf.FloorToInt(bounds.max.x / TerrainCell); x++)
                {
                    for (int y = Mathf.FloorToInt(bounds.min.y / TerrainCell); y <= Mathf.FloorToInt(bounds.max.y / TerrainCell); y++)
                    {
                        long cell = Key(x, y);
                        TerrainByCell[cell] = terrain;
                        cells.Add(cell);
                    }
                }
            }

            CellsById[definition.Id] = cells;
        }

        internal static void RemoveTerrain(string id)
        {
            if (!CellsById.Remove(id, out List<long>? cells))
            {
                return;
            }

            foreach (long cell in cells)
            {
                TerrainByCell.Remove(cell);
            }
        }

        private static void GetTerrainAtPostfix(Vector2 worldPos, ref TerrainType2D.EType __result)
        {
            if (TerrainByCell.Count > 0
                && TerrainByCell.TryGetValue(Key(Mathf.FloorToInt(worldPos.x / TerrainCell), Mathf.FloorToInt(worldPos.y / TerrainCell)), out TerrainType2D.EType terrain))
            {
                __result = terrain;
            }
        }

        private static long Key(int x, int y) => ((long)x << 32) | (uint)y;

        private static Color? ParseColor(string? hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                return null;
            }

            string digits = hex.TrimStart('#');
            if (digits.Length != 6 && digits.Length != 8 || !uint.TryParse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint value))
            {
                return null;
            }

            if (digits.Length == 6)
            {
                value = (value << 8) | 0xFF;
            }

            return new Color(((value >> 24) & 0xFF) / 255f, ((value >> 16) & 0xFF) / 255f, ((value >> 8) & 0xFF) / 255f, (value & 0xFF) / 255f);
        }
    }
}
