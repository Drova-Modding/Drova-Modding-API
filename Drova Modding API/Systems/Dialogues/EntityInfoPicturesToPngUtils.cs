using Il2CppDrova;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues
{
    /**
     * Converts all EntityInfo portraits to PNGs. Does not work: the portraits are packed textures that are not readable at runtime.
     */
    internal static class EntityInfoPicturesToPngUtils
    {
        public static void ReadAllEntityInfos()
        {
            var allEntityInfos = Resources.FindObjectsOfTypeAll<EntityInfo>();
            var allEntityInfosWithPictures = allEntityInfos.Where(e =>
            {
                return e._entityPortrait != null && e._entityPortrait.HasPortrait();
            }).ToList();
            var potratis = allEntityInfosWithPictures.Select(x => x._entityPortrait.GetPortrait()).ToList();
            for (int i = 0; i < potratis.Count; i++)
            {
                var portrait = potratis[i];
                var bytes = portrait.texture.EncodeToPNG();
                File.WriteAllBytes(Path.Combine(Utils.SavePath, "/pngs/", $"EntityInfoPortrait_{allEntityInfosWithPictures[i].GetLocalizedName()}.png"), bytes);
            }
        }
    }
}
