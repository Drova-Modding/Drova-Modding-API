using Drova_Modding_API.Access;
using Il2CppDrova;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning;

/// <summary>
/// Small stateless helpers for definition-centric operations shared across wizard UI pieces.
/// </summary>
internal static class NpcWizardDefinitionHelpers
{
    internal static ExternalNpcPlacementSystem.ExternalNpcDefinition CreateDefaultDefinitionAtPlayerPosition()
    {
        ExternalNpcPlacementSystem.ExternalNpcDefinition definition = ExternalNpcPlacementSystem.CreateDefaultDefinition();
        if (PlayerAccess.TryGetPlayer(out Actor player) && player != null)
        {
            Vector3 playerPosition = player.transform.position;
            definition.PositionX = playerPosition.x;
            definition.PositionY = playerPosition.y;
        }

        return definition;
    }

    internal static bool TryTeleportPlayerToDefinition(ExternalNpcPlacementSystem.ExternalNpcDefinition definition)
    {
        if (!PlayerAccess.TryGetPlayer(out Actor player) || player == null)
            return false;

        Vector3 current = player.transform.position;
        player.transform.position = new Vector3(definition.PositionX, definition.PositionY, current.z);
        return true;
    }

    internal static bool ValidateForSave(ExternalNpcPlacementSystem.ExternalNpcDefinition definition, out string status)
    {
        definition.Id = definition.Id.Trim();
        definition.Name = definition.Name.Trim();

        if (string.IsNullOrWhiteSpace(definition.Id))
        {
            status = "Definition Id is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(definition.Name))
        {
            status = "Display Name is required.";
            return false;
        }

        status = string.Empty;
        return true;
    }
}

