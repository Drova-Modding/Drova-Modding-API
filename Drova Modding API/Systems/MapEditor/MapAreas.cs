using Il2Cpp;
using Il2CppDrova;
using Il2CppDrova.PathfindingUtilities;
using Il2CppDrova.Weather;
using Il2CppPathfinding;
using MelonLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Drova_Modding_API.Systems.MapEditor
{
    /// <summary>
    /// The navmesh of the layout's areas. Drova keeps one Recast graph per interior next to the overworld's, all loaded from
    /// a baked cache with every world. An area gets a copy of an interior graph (its 2D settings), moved to the area and
    /// scanned alone. The cache replaces all graphs on each world load, so a missing area graph is simply made again.
    /// </summary>
    internal static class MapAreas
    {
        private const string GraphPrefix = "Modding_API_Area_";

        private static readonly List<MapAreaDefinition> Areas = [];
        private static readonly HashSet<string> Present = new(StringComparer.Ordinal);
        private static readonly HashSet<string> FailedIds = new(StringComparer.Ordinal);
        private static WeatherArea? _indoor;
        private static WeatherGameHandler? _indoorHandler;

        internal static IReadOnlyList<MapAreaDefinition> All => Areas;

        /// <summary>
        /// Replaces the areas. Their graphs are made again on the next tick.
        /// </summary>
        internal static void Set(IEnumerable<MapAreaDefinition> areas)
        {
            RemoveGraphs();
            FailedIds.Clear();
            Areas.Clear();
            Areas.AddRange(areas);
        }

        /// <summary>
        /// Scans the graph of every area that has none, once the world's navmesh cache is loaded. Called from the layout's tick.
        /// </summary>
        internal static void Tick()
        {
            AstarPath? astar = AstarPath.active;
            Pathfinding_Cache_OnStart? cache = Pathfinding_Cache_OnStart.CachePathfinding;
            if (Areas.Count == 0 || astar == null || astar.data == null || astar.data.graphs == null || cache == null || !cache.HasLoadedPathfinding())
            {
                return;
            }

            Present.Clear();
            RecastGraph? template = null;
            for (int i = 0; i < astar.data.graphs.Length; i++)
            {
                RecastGraph? graph = astar.data.graphs[i]?.TryCast<RecastGraph>();
                if (graph == null)
                {
                    continue;
                }

                if (graph.name.StartsWith(GraphPrefix, StringComparison.Ordinal))
                {
                    Present.Add(graph.name);
                }
                else if (i > 0 && (template == null || graph.forcedBoundsSize.x * graph.forcedBoundsSize.z < template.forcedBoundsSize.x * template.forcedBoundsSize.z))
                {
                    template = graph;
                }
            }

            if (template == null)
            {
                return;
            }

            foreach (MapAreaDefinition area in Areas)
            {
                if (!Present.Contains(GraphPrefix + area.Id) && !FailedIds.Contains(area.Id))
                {
                    Scan(astar, template, area);
                }
            }
        }

        /// <summary>
        /// Registers an indoor weather area with the game while the player stands in an indoor area, as a vanilla interior's
        /// trigger does. Called from the layout's tick.
        /// </summary>
        internal static void UpdateWeather(Vector2 player)
        {
            bool inside = Areas.Any(area => area.Indoor && Contains(area, player));
            WeatherGameHandler.TryGet(out WeatherGameHandler? handler);
            if (inside && handler != null && _indoorHandler != handler)
            {
                if (_indoor == null)
                {
                    GameObject holder = new("Modding_API_IndoorWeather");
                    Object.DontDestroyOnLoad(holder);
                    _indoor = holder.AddComponent<WeatherArea>();
                    _indoor._area = WeatherAreaType.Indoor;
                    _indoor._priority = 1;
                }

                handler.AddWeatherArea(_indoor);
                _indoorHandler = handler;
            }
            else if (!inside && _indoorHandler != null)
            {
                if (_indoorHandler == handler && _indoor != null)
                {
                    handler.RemoveWeatherArea(_indoor);
                }

                _indoorHandler = null;
            }
        }

        /// <summary>
        /// The area whose rectangle holds <paramref name="position"/>, if any.
        /// </summary>
        internal static MapAreaDefinition? At(Vector2 position) => Areas.FirstOrDefault(area => Contains(area, position));

        private static bool Contains(MapAreaDefinition area, Vector2 position)
            => Mathf.Abs(position.x - area.X) <= area.Width / 2f && Mathf.Abs(position.y - area.Y) <= area.Height / 2f;

        private static void Scan(AstarPath astar, RecastGraph template, MapAreaDefinition area)
        {
            try
            {
                RecastGraph graph = astar.data.DuplicateGraph(template).Cast<RecastGraph>();
                graph.name = GraphPrefix + area.Id;
                graph.forcedBoundsCenter = new Vector3(area.X, area.Y, template.forcedBoundsCenter.z);
                graph.forcedBoundsSize = new Vector3(area.Width, template.forcedBoundsSize.y, area.Height);
                astar.Scan(graph);
            }
            catch (Exception e)
            {
                MelonLogger.Error($"[MapEditor] The navmesh of area '{area.Id}' could not be made: {e}");
                FailedIds.Add(area.Id);
            }
        }

        private static void RemoveGraphs()
        {
            AstarPath? astar = AstarPath.active;
            if (astar == null || astar.data == null || astar.data.graphs == null)
            {
                return;
            }

            for (int i = astar.data.graphs.Length - 1; i >= 0; i--)
            {
                NavGraph? graph = astar.data.graphs[i];
                if (graph != null && graph.name.StartsWith(GraphPrefix, StringComparison.Ordinal))
                {
                    astar.data.RemoveGraph(graph);
                }
            }
        }
    }
}
