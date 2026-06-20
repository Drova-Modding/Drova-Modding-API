using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning;

/// <summary>
/// Renders and manages the secondary definitions browser window, including cached list refresh.
/// </summary>
internal sealed class NpcWizardDefinitionsBrowser
{
    private Vector2 _scroll;
    private readonly List<ExternalNpcPlacementSystem.ExternalNpcDefinition> _cachedDefinitions = [];

    internal void Refresh()
    {
        _cachedDefinitions.Clear();
        _cachedDefinitions.AddRange(ExternalNpcPlacementSystem.ReadAllDefinitions());
    }

    internal bool DrawWindow(
        Action<ExternalNpcPlacementSystem.ExternalNpcDefinition> onLoad,
        Action<string> setStatus)
    {
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh", GUILayout.Width(90f)))
            Refresh();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Close", GUILayout.Width(90f)))
        {
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            return false;
        }
        GUILayout.EndHorizontal();

        _scroll = GUILayout.BeginScrollView(_scroll);
        for (int i = 0; i < _cachedDefinitions.Count; i++)
        {
            ExternalNpcPlacementSystem.ExternalNpcDefinition definition = _cachedDefinitions[i];
            bool isSpawned = ExternalNpcPlacementSystem.IsDefinitionSpawned(definition.Id);

            GUILayout.BeginVertical("box");
            GUILayout.Label($"{definition.Name} ({definition.Id})");
            GUILayout.Label($"Pos: {definition.PositionX:0.##}, {definition.PositionY:0.##}  |  Enabled: {(definition.Enabled ? "yes" : "no")}  |  Spawned: {(isSpawned ? "yes" : "no")}");
            GUILayout.Label($"Source: {ExternalNpcPlacementSystem.GetDefinitionSourceFileName(definition.Id)}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Load", GUILayout.Width(90f)))
                onLoad(definition);

            if (GUILayout.Button("Teleport", GUILayout.Width(90f)))
                setStatus(NpcWizardDefinitionHelpers.TryTeleportPlayerToDefinition(definition)
                    ? $"Teleported player to '{definition.Id}'."
                    : "Player was not found. Teleport failed.");

            if (GUILayout.Button("Spawn", GUILayout.Width(90f)))
            {
                bool spawned = ExternalNpcPlacementSystem.SpawnDefinition(definition, skipIfAlreadySpawned: true, requireEnabled: false);
                setStatus(spawned ? $"Spawned '{definition.Id}'." : $"Spawn skipped for '{definition.Id}'.");
                Refresh();
            }

            GUI.enabled = isSpawned;
            if (GUILayout.Button("Despawn", GUILayout.Width(90f)))
            {
                bool despawned = ExternalNpcPlacementSystem.DespawnDefinition(definition.Id);
                setStatus(despawned ? $"Despawned '{definition.Id}'." : $"Despawn skipped for '{definition.Id}'.");
                Refresh();
            }
            GUI.enabled = true;

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
        return true;
    }
}


