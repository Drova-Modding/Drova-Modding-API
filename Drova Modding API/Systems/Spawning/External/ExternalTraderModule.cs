using Drova_Modding_API.Systems.Spawning.Modules;
using System.Globalization;
using System.Text.Json;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// External NPC module that turns a spawned NPC into a trader with a configurable item stock
    /// (per-item amounts) and money, backed by <see cref="TraderModule"/>.
    /// </summary>
    internal sealed class ExternalTraderModule : IExternalNpcModule
    {
        public const string ModuleKey = "trader";

        private readonly ReadableIdModuleSupport.EditorUiState _editorUiState = new();
        private TraderModuleState _cachedState = new();
        private string _lastPayload = string.Empty;
        private string _serializedPayload = string.Empty;
        private string _rawInput = string.Empty;
        private string _moneyInput = "0";

        public string Key => ModuleKey;
        public string DisplayName => "Trader";

        public string CreateDefaultPayload() => JsonSerializer.Serialize(new TraderModuleState());

        public string DrawWizardUiAndSerialize(string payload)
        {
            EnsureUiState(payload);

            bool changed = false;

            // ── Trader name ──────────────────────────────────────────────────
            GUILayout.Label("Trader name (blank = NPC name)");
            string updatedName = GUILayout.TextField(_cachedState.Name ?? string.Empty);
            if (!string.Equals(updatedName, _cachedState.Name, StringComparison.Ordinal))
            {
                _cachedState.Name = updatedName;
                changed = true;
            }

            // ── Trader money ─────────────────────────────────────────────────
            GUILayout.Space(6f);
            GUILayout.Label("Trader money");
            string previousMoneyInput = _moneyInput;
            string updatedMoneyInput = GUILayout.TextField(_moneyInput);
            if (!string.Equals(updatedMoneyInput, previousMoneyInput, StringComparison.Ordinal))
            {
                if (float.TryParse(updatedMoneyInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedMoney))
                {
                    float clampedMoney = Mathf.Max(0f, parsedMoney);
                    if (!Mathf.Approximately(clampedMoney, _cachedState.Money))
                    {
                        _cachedState.Money = clampedMoney;
                        changed = true;
                    }
                    _moneyInput = updatedMoneyInput;
                }
                else
                {
                    _moneyInput = previousMoneyInput;
                }
            }

            // ── Item picker ──────────────────────────────────────────────────
            GUILayout.Space(6f);
            string? rawInput = _rawInput;
            ReadableIdModuleSupport.DrawEditor(
                "Trader item readable ids",
                ref rawInput,
                _editorUiState,
                out List<string> selectedIds,
                ModuleKey);

            if (!string.Equals(rawInput, _rawInput, StringComparison.Ordinal))
            {
                _rawInput = rawInput ?? string.Empty;
                _cachedState.Items = Reconcile(selectedIds, _cachedState.Items);
                changed = true;
            }

            // ── Per-item amounts ─────────────────────────────────────────────
            if (_cachedState.Items.Count > 0)
            {
                GUILayout.Space(6f);
                GUILayout.Label("Stock amounts");
                for (int i = 0; i < _cachedState.Items.Count; i++)
                {
                    TraderItemState entry = _cachedState.Items[i];
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(entry.ReadableId, GUILayout.ExpandWidth(true));
                    string amountInput = GUILayout.TextField(entry.Amount.ToString(CultureInfo.InvariantCulture), GUILayout.Width(80f));
                    GUILayout.EndHorizontal();

                    if (int.TryParse(amountInput, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedAmount))
                    {
                        int clampedAmount = Mathf.Max(1, parsedAmount);
                        if (clampedAmount != entry.Amount)
                        {
                            entry.Amount = clampedAmount;
                            changed = true;
                        }
                    }
                }
            }

            return changed ? SerializeCachedState() : _serializedPayload;
        }

        public void ApplyToCreator(NpcCreator creator, string? payload)
        {
            TraderModuleState state = Parse(payload);

            if (state.Items.Count == 0 && state.Money <= 0f)
                return;

            TraderModule module = new();
            module.WithName(state.Name ?? string.Empty);
            module.WithMoney(state.Money);
            for (int i = 0; i < state.Items.Count; i++)
            {
                TraderItemState entry = state.Items[i];
                module.WithItem(entry.ReadableId, entry.Amount);
            }

            creator.WithModule(module);
        }

        private static List<TraderItemState> Reconcile(List<string> selectedIds, List<TraderItemState> existing)
        {
            Dictionary<string, int> amountsById = new(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < existing.Count; i++)
                amountsById[existing[i].ReadableId] = existing[i].Amount;

            List<TraderItemState> reconciled = new(selectedIds.Count);
            for (int i = 0; i < selectedIds.Count; i++)
            {
                string id = selectedIds[i];
                int amount = amountsById.GetValueOrDefault(id, 1);
                reconciled.Add(new TraderItemState { ReadableId = id, Amount = Math.Max(1, amount) });
            }

            return reconciled;
        }

        private static TraderModuleState Parse(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return new TraderModuleState();

            try
            {
                TraderModuleState state = JsonSerializer.Deserialize<TraderModuleState>(payload) ?? new TraderModuleState();
                state.Name ??= string.Empty;
                state.Items ??= [];
                for (int i = 0; i < state.Items.Count; i++)
                    state.Items[i].Amount = Math.Max(1, state.Items[i].Amount);
                return state;
            }
            catch
            {
                return new TraderModuleState();
            }
        }

        private void EnsureUiState(string? payload)
        {
            string normalizedPayload = payload ?? string.Empty;
            if (string.Equals(_lastPayload, normalizedPayload, StringComparison.Ordinal))
                return;

            _cachedState = Parse(payload);
            _moneyInput = _cachedState.Money.ToString(CultureInfo.InvariantCulture);
            _rawInput = string.Join(Environment.NewLine, _cachedState.Items.Select(item => item.ReadableId));
            _serializedPayload = JsonSerializer.Serialize(_cachedState);
            _lastPayload = normalizedPayload;
        }

        private string SerializeCachedState()
        {
            _serializedPayload = JsonSerializer.Serialize(_cachedState);
            _lastPayload = _serializedPayload;
            return _serializedPayload;
        }

        private sealed class TraderModuleState
        {
            public string? Name { get; set; } = string.Empty;
            public float Money { get; set; }
            public List<TraderItemState> Items { get; set; } = [];
        }

        private sealed class TraderItemState
        {
            public string ReadableId { get; set; } = string.Empty;
            public int Amount { get; set; } = 1;
        }
    }
}
