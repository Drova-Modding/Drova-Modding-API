using Il2CppDrova;
using Il2CppDrova.GUI.LearnGUI;
using Il2CppDrova.Talent;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning.Modules
{
    /// <summary>
    /// Handles applying teacher configs to spawned NPCs.
    /// </summary>
    public class TeacherModule : INpcModule
    {
        private int _maxAttributeLevel = 130;
        private readonly HashSet<TeacherConfig.Stat> _teacherStats = [];
        private readonly HashSet<TeachableTalent> _teacherTalents = [];

        /// <summary>
        /// Sets the max teachable attribute level
        /// </summary>
        /// <param name="maxAttributeLevel">max level, default 130</param>
        /// <returns></returns>
        public TeacherModule With(int maxAttributeLevel)
        {
            _maxAttributeLevel = maxAttributeLevel;
            return this;
        }

        /// <summary>
        /// Sets the teachable stats
        /// </summary>
        /// <param name="stats">Stats</param>
        /// <returns></returns>
        public TeacherModule With(params TeacherConfig.Stat[] stats)
        {
            foreach (var stat in stats)
            {
                _teacherStats.Add(stat);
            }
            return this;
        }

        /// <summary>
        /// Sets the teachable talents
        /// </summary>
        /// <param name="talents"></param>
        /// <returns></returns>
        public TeacherModule With(params TeachableTalent[] talents)
        {
            foreach (var talent in talents)
            {
                _teacherTalents.Add(talent);
            }
            return this;
        }

        /// <summary>
        /// Applies the teacher config to the actor
        /// </summary>
        /// <param name="context"></param>
        public void Apply(ModuleContext context)
        {
            var actor = context.GetComponent<Actor>();
            if (actor._teacherProvider == null)
            {
                var teacherProvider = new TeacherConfigProvider();
                actor._teacherProvider = teacherProvider.TryCast<ITeacherConfigProvider>();
                teacherProvider._teacherConfig = ScriptableObject.CreateInstance<TeacherConfig>();
                teacherProvider._teacherConfig._maxAttributeLevel = _maxAttributeLevel;
                for (int i = 0; i < this._teacherStats.Count; i++)
                {
                    teacherProvider._teacherConfig._stats.Add(this._teacherStats.ElementAt(i));
                }
                for (int i = 0; i < this._teacherTalents.Count; i++)
                {
                    teacherProvider._teacherConfig._talents.Add(this._teacherTalents.ElementAt(i));
                }
            }
            else
            {
                var teacherProvider = actor._teacherProvider.TryCast<TeacherConfigProvider>();
                if (teacherProvider == null)
                {
                    MelonLogger.Error("Teacher provider is  not of type TeacherConfigProvider");
                    return;
                }

                teacherProvider._teacherConfig = ScriptableObject.CreateInstance<TeacherConfig>();
                teacherProvider._teacherConfig._maxAttributeLevel = _maxAttributeLevel;
                for (int i = 0; i < this._teacherStats.Count; i++)
                {
                    teacherProvider._teacherConfig._stats.Add(this._teacherStats.ElementAt(i));
                }
                for (int i = 0; i < this._teacherTalents.Count; i++)
                {
                    teacherProvider._teacherConfig._talents.Add(this._teacherTalents.ElementAt(i));
                }
            }
        }
    }
}