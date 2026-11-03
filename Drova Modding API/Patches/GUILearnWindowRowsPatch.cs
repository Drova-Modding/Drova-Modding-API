using HarmonyLib;
using Il2CppDrova.GUI;
using Il2CppDrova.GUI.LearnGUI;
using Il2CppDrova.Talent;
using MelonLoader;
using UnityEngine;
using Il2CppCollections = Il2CppSystem.Collections.Generic;
using Object = UnityEngine.Object;

namespace Drova_Modding_API.Patches
{
    /// <summary>
    /// The learn window has one row of four talent buttons and drops every talent past them.
    /// A teacher with more talents gets more rows, cloned from the first, and the window grows upward by them.
    /// </summary>
    [HarmonyPatch(typeof(GUI_LearnWindow), nameof(GUI_LearnWindow.SetLearnableTalents))]
    internal static class GUILearnWindowRowsPatch
    {
        /// <summary>
        /// Height of one row: a 28 high talent slot and the 17 high cost label under it.
        /// </summary>
        private const float RowHeight = 46f;

        private static readonly Dictionary<int, Rows> RowsByWindow = new();

        static void Prefix(GUI_LearnWindow __instance, Il2CppCollections.List<TeachableTalent>? talents)
        {
            // A throw here would abort the game's fill and leave the window empty.
            try
            {
                AddRows(__instance, talents);
            }
            catch (Exception e)
            {
                MelonLogger.Error("[LearnWindow] adding talent rows failed: " + e);
            }
        }

        private static void AddRows(GUI_LearnWindow window, Il2CppCollections.List<TeachableTalent>? talents)
        {
            Il2CppCollections.List<GUI_Talent_LearnButton> buttons = window._talentButtons;
            if (talents == null || buttons == null || buttons.Count == 0 || window._panel == null)
            {
                return;
            }

            int id = window.GetInstanceID();
            if (!RowsByWindow.TryGetValue(id, out Rows? rows))
            {
                RectTransform first = buttons[0].transform.parent.Cast<RectTransform>();
                rows = new Rows(first, first.anchoredPosition, buttons.Count);
                RowsByWindow[id] = rows;
            }

            int extra = Math.Max(0, (talents.Count + rows.PerRow - 1) / rows.PerRow - 1);
            while (rows.Added.Count < extra)
            {
                RectTransform clone = Object.Instantiate(rows.First.gameObject, rows.First.parent).transform.Cast<RectTransform>();
                rows.Added.Add(clone);
                foreach (GUI_Talent_LearnButton button in clone.GetComponentsInChildren<GUI_Talent_LearnButton>(true))
                {
                    buttons.Add(button);
                }
            }

            for (int i = 0; i < rows.Added.Count; i++)
            {
                rows.Added[i].gameObject.SetActive(i < extra);
            }

            // The panel keeps its bottom edge: the first row moves up, and the added rows fill in below it.
            float grow = (extra - rows.Shown) * RowHeight;
            Vector2 size = window._panel.sizeDelta;
            window._panel.sizeDelta = new Vector2(size.x, size.y + grow);
            Vector2 position = window._panel.anchoredPosition;
            window._panel.anchoredPosition = new Vector2(position.x, position.y + grow / 2f);
            rows.Shown = extra;

            float top = rows.Origin.y + extra * RowHeight / 2f;
            rows.First.anchoredPosition = new Vector2(rows.Origin.x, top);
            for (int i = 0; i < extra; i++)
            {
                rows.Added[i].anchoredPosition = new Vector2(rows.Origin.x, top - (i + 1) * RowHeight);
            }
        }

        private sealed class Rows(RectTransform first, Vector2 origin, int perRow)
        {
            public RectTransform First { get; } = first;

            public Vector2 Origin { get; } = origin;

            public int PerRow { get; } = perRow;

            public List<RectTransform> Added { get; } = [];

            public int Shown { get; set; }
        }
    }
}
