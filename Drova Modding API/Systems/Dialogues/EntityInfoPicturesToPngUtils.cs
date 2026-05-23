using Il2CppDrova;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues
{
    /**
     * A class to convert all EntityInfos to PNGs. Not working at the moment tho, its packed image, which isn't readable at runtime
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
                //var texture = new Texture2D(portrait.texture.width, portrait.texture.height, TextureFormat.BC7, false);
                //texture.SetPixels(portrait.texture.GetPixels());
                //texture.Apply();
                var bytes = portrait.texture.EncodeToPNG();
                File.WriteAllBytes(Path.Combine(Utils.SavePath, "/pngs/", $"EntityInfoPortrait_{allEntityInfosWithPictures[i].GetLocalizedName()}.png"), bytes);
            }
        }
    }
}
