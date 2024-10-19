using Drova_Modding_API.UI;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;

[assembly: MelonInfo(typeof(Drova_Modding_API.Core), "Drova Modding API", "0.1.0", "Drova Modding", null)]
[assembly: MelonGame("Just2D", "Drova")]
[assembly: MelonPriority(1000)]
namespace Drova_Modding_API
{
    public class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Initialized Modding API.");
            ClassInjector.RegisterTypeInIl2Cpp<GUI_ConfigOption_Slider_Float>();
        }
    }
}