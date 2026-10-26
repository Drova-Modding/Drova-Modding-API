using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Il2CppDrova.ABI;
using Il2CppInterop.Runtime;
using Il2CppCollections = Il2CppSystem.Collections.Generic;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Reads and writes the values of ability data (<c>ABIData&lt;T&gt;</c>), such as <c>ABI_HealthValue</c> or <c>Dmg_MaxHitCount</c>.
    /// The interop reads <c>_value</c> of the fully shared value-type <c>T</c> at a wrong offset,
    /// so it is read directly right after <c>ABIBaseData._key</c>.
    /// </summary>
    public static class AbilityDataAccess
    {
        private static int _valueOffset = -1;

        private static int ValueOffset
        {
            get
            {
                if (_valueOffset < 0)
                {
                    IntPtr keyField = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<ABIBaseData>.NativeClassPtr, "_key");
                    _valueOffset = (int)IL2CPP.il2cpp_field_get_offset(keyField) + IntPtr.Size;
                }

                return _valueOffset;
            }
        }

        /// <summary>
        /// The property with that key in an ability's data, or null.
        /// </summary>
        public static bool TryGet(ABI_Ability? ability, string key, [NotNullWhen(true)] out ABIBaseData? data)
        {
            data = ability != null ? ability.GetABIDataContainer().GetGenericPropertyData(key) : null;
            return data != null;
        }

        /// <summary>
        /// Reads a float property. False when it holds another type.
        /// </summary>
        public static bool TryGetFloat(ABIBaseData data, out float value)
        {
            value = 0f;
            if (!Holds(data, "System.Single"))
            {
                return false;
            }

            value = BitConverter.Int32BitsToSingle(Marshal.ReadInt32(data.Pointer + ValueOffset));
            return true;
        }

        /// <summary>
        /// Reads an int property. False when it holds another type.
        /// </summary>
        public static bool TryGetInt(ABIBaseData data, out int value)
        {
            value = 0;
            if (!Holds(data, "System.Int32"))
            {
                return false;
            }

            value = Marshal.ReadInt32(data.Pointer + ValueOffset);
            return true;
        }

        /// <summary>
        /// Reads a bool property. False when it holds another type.
        /// </summary>
        public static bool TryGetBool(ABIBaseData data, out bool value)
        {
            value = false;
            if (!Holds(data, "System.Boolean"))
            {
                return false;
            }

            value = Marshal.ReadByte(data.Pointer + ValueOffset) != 0;
            return true;
        }

        /// <summary>
        /// Writes a number into a float, int or bool property (non-zero is true). False for any other type.
        /// Every instance spawned from the ability afterwards copies the new value.
        /// </summary>
        public static bool TrySet(ABIBaseData data, double value)
        {
            if (Holds(data, "System.Single"))
            {
                Marshal.WriteInt32(data.Pointer + ValueOffset, BitConverter.SingleToInt32Bits((float)value));
                return true;
            }

            if (Holds(data, "System.Int32"))
            {
                Marshal.WriteInt32(data.Pointer + ValueOffset, (int)Math.Round(value));
                return true;
            }

            if (Holds(data, "System.Boolean"))
            {
                Marshal.WriteByte(data.Pointer + ValueOffset, value != 0 ? (byte)1 : (byte)0);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Writes the amount of one entry of a damage list property (<c>Dmg_Properties</c>, <c>Dmg_Properties_NPCs</c>):
        /// what a hit deals of that entry's damage type before the attacker's stats scale it. False without such an entry.
        /// </summary>
        public static bool TrySetDamage(ABIBaseData data, int index, double value)
        {
            Il2CppCollections.List<ABI_Ability.DamageInfo>? damage = data.GetValue()?.TryCast<Il2CppCollections.List<ABI_Ability.DamageInfo>>();
            if (damage == null || index < 0 || index >= damage.Count || damage[index] == null)
            {
                return false;
            }

            damage[index].Value = (float)value;
            return true;
        }

        /// <summary>
        /// The property's value as text, for diagnostics and editors.
        /// </summary>
        public static string Describe(ABIBaseData data)
        {
            if (TryGetFloat(data, out float number))
            {
                return number.ToString(CultureInfo.InvariantCulture);
            }

            if (TryGetInt(data, out int integer))
            {
                return integer.ToString(CultureInfo.InvariantCulture);
            }

            if (TryGetBool(data, out bool flag))
            {
                return flag ? "true" : "false";
            }

            Il2CppSystem.Object? value = data.GetValue();
            Il2CppCollections.List<ABI_Ability.DamageInfo>? damage = value?.TryCast<Il2CppCollections.List<ABI_Ability.DamageInfo>>();
            if (damage != null)
            {
                List<string> entries = [];
                for (int i = 0; i < damage.Count; i++)
                {
                    ABI_Ability.DamageInfo? info = damage[i];
                    entries.Add(info == null ? "null" : (info.DamageStat != null ? info.DamageStat.name : "?") + ":" + info.Value.ToString(CultureInfo.InvariantCulture));
                }

                return "[" + string.Join(" ", entries) + "]";
            }

            return value != null ? value.ToString() : "null";
        }

        private static bool Holds(ABIBaseData data, string typeName)
            => data.GetIl2CppType().FullName.Contains("[[" + typeName + ",", StringComparison.Ordinal);
    }
}
