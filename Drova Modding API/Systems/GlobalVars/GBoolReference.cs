using Drova_Modding_API.Access;
using Il2CppDrova.GlobalVarSystem;

namespace Drova_Modding_API.Systems.GlobalVars
{
    /// <summary>
    /// Serializable reference to a <see cref="GBool"/> stored by list GUID and variable GUID.
    /// </summary>
    public sealed class GBoolReference
    {
        /// <summary>
        /// GUID of the parent global-variable list.
        /// </summary>
        public string GVarListGuid { get; set; } = string.Empty;

        /// <summary>
        /// GUID of the GBool within the list.
        /// </summary>
        public string GBoolGuid { get; set; } = string.Empty;

        /// <summary>
        /// Creates a persistent reference from a resolved GBool instance.
        /// </summary>
        public static GBoolReference? From(GBool? boolVar)
        {
            if (boolVar == null)
                return null;

            GVarList? parent = boolVar.GetParent();
            if (parent == null)
                return null;

            return new GBoolReference
            {
                GVarListGuid = parent.Guid,
                GBoolGuid = boolVar.GetGVarId()
            };
        }

        /// <summary>
        /// Resolves the reference back into the live GBool instance if it still exists.
        /// </summary>
        public GBool? Resolve()
        {
            if (string.IsNullOrWhiteSpace(GVarListGuid) || string.IsNullOrWhiteSpace(GBoolGuid))
                return null;

            GVarList? gvarList = ProviderAccess.GVarDatabase.GetGVarListByGuid(GVarListGuid);
            if (gvarList == null)
                return null;

            return gvarList.GetGVarById(GBoolGuid).TryCast<GBool>();
        }
    }
}