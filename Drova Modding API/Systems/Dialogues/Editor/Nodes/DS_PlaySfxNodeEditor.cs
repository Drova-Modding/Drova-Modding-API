using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppCustomFramework.Audio;
using Il2CppNodeCanvas.DialogueTrees.UI;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="DS_PlaySfx"/>
    /// </summary>
    internal class DS_PlaySfxNodeEditor : DrawNodeEditor
    {
        private DS_PlaySfx _castedNode;
        private GUIDropdownWithFilter _sfxDropdown;
        private AudioContainer[] _audioContainers;

        public DS_PlaySfxNodeEditor()
        {
            NodeSizeInternal = new Vector2(450, 80);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_PlaySfx>();
            if (_castedNode == null) return;
            _audioContainers = Resources.FindObjectsOfTypeAll<AudioContainer>();
            int selectedIndex = -1;
            if (_castedNode._sfx != null)
            {
                selectedIndex = Array.FindIndex(_audioContainers, (s) => s._containerName == _castedNode._sfx._containerName);
            }
            _sfxDropdown = new GUIDropdownWithFilter(_audioContainers.Select(s => s.name).ToArray(), selectedIndex, 20);
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return;
            }
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            GUI.color = Color.green;
            GUI.Box(new(position.x, position.y, 450, 80), "DS_PlaySfx");
            GUI.color = Color.white;
            GUI.depth = 10;
            GUI.Label(new Rect(position.x + 10, position.y + 20, 85, 20), "SFX:");
            if (_sfxDropdown != null && _sfxDropdown.Draw(new Rect(position.x + 100, position.y + 20, 200, 20)))
            {
                _castedNode._sfx = _audioContainers[_sfxDropdown.SelectedIndex];
            }
            GUI.color = previousColor;
            GUI.depth = previousDepth;

        }
    }
}
