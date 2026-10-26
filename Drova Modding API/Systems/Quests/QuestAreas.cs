using Il2CppDrova.GlobalVarSystem;
using Il2CppDrova.HitSystem;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Drova_Modding_API.Systems.Quests
{
    /// <summary>
    /// The quest areas of <see cref="QuestSystem"/>: one vanilla trigger volume per area that sets its bool when the player
    /// enters, present while its conditions hold and its bool is still false.
    /// </summary>
    internal static class QuestAreas
    {
        private static readonly Dictionary<string, GameObject> AreasByKey = new(StringComparer.OrdinalIgnoreCase);
        private static Scene _scene;

        internal static void Initialize() => GVarConditions.Changed += Refresh;

        /// <summary>
        /// Places the areas of every quest. Runs on each world load.
        /// </summary>
        internal static void Spawn(Scene scene)
        {
            AreasByKey.Clear();
            _scene = scene;
            Refresh();
        }

        private static void Refresh()
        {
            if (!_scene.isLoaded)
            {
                return;
            }

            foreach (QuestDefinition quest in QuestSystem.Quests.Values)
            {
                foreach (QuestAreaDefinition area in quest.Areas)
                {
                    string key = quest.Id + "/" + area.Id;
                    bool reached = GVarConditions.IsMet(new GVarCondition { GVarList = quest.GVarList, Var = area.SetGBool, Value = "true" });
                    bool wanted = !reached && GVarConditions.AreMetForPlacement(area.Conditions);
                    AreasByKey.TryGetValue(key, out GameObject? existing);

                    if (wanted && existing == null)
                    {
                        GameObject? created = Create(quest, area, key);
                        if (created != null)
                        {
                            AreasByKey[key] = created;
                        }
                    }
                    else if (!wanted && existing != null)
                    {
                        Object.Destroy(existing);
                        AreasByKey.Remove(key);
                    }
                }
            }
        }

        private static GameObject? Create(QuestDefinition quest, QuestAreaDefinition area, string key)
        {
            AGVarBase? gvar = GVarConditions.Find(quest.GVarList, area.SetGBool);
            GBool? gbool = gvar != null ? gvar.TryCast<GBool>() : null;
            if (gbool == null)
            {
                MelonLogger.Warning($"[QuestSystem] Area '{key}': no bool '{area.SetGBool}' in list '{quest.GVarList}'.");
                return null;
            }

            // Built while inactive: the handler reads its collider and bhvrs on Awake.
            GameObject volume = new("Modding_API_QuestArea_" + key.Replace('/', '_'));
            volume.SetActive(false);
            volume.transform.position = new Vector3(area.X, area.Y, 0f);

            CircleCollider2D collider = volume.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = area.Radius;
            Rigidbody2D body = volume.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;

            GBoolOperation operation = new();
            operation.SetValues(gbool, GBool.Operator.Set, true);
            HitReceiveBhvr_SetGVars setter = volume.AddComponent<HitReceiveBhvr_SetGVars>();
            setter._operations.Add(operation.Cast<IGVarOperation>());

            // Player-only volumes poll the player's position themselves, as the vanilla quest areas do.
            HitReceiveHandler handler = volume.AddComponent<HitReceiveHandler>();
            handler._onlyCheckWithPlayer = true;
            handler._rgbd = body;
            handler._receiveBhvrs.Add(setter);

            SceneManager.MoveGameObjectToScene(volume, _scene);
            volume.SetActive(true);
            return volume;
        }
    }
}
