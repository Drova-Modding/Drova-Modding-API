using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.ModdingUI;
using Il2CppDrova;
using Il2CppDrova.Saveables;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Il2CppCollections = Il2CppSystem.Collections.Generic;
using Object = UnityEngine.Object;

namespace Drova_Modding_API.Systems.MapEditor
{
    /// <summary>
    /// The in-game map editor: place environment props and invisible blockers, select and drag them, and hide vanilla
    /// objects. Opened from the Modding Tools window. Every change is saved to <c>MapEditor/map_editor.json</c> at once.
    /// </summary>
    internal static class MapEditorWindow
    {
        private const string InputOwner = "MapEditor";
        private const string BlockerKey = "";
        private const string AreaKey = "area";
        private const float MinAreaSize = 64f;
        private const string DynamicKeyPrefix = "dynamic:";
        private const float Width = 380f;
        private const float Top = 64f;
        private const float Margin = 16f;
        private const float Row = 24f;
        private const float PickRadius = 48f;
        private const float ScaleStep = 0.1f;
        private const float SizeStep = 16f;
        private const float TurnStep = 15f;
        private const float AreaStep = 64f;

        private static readonly List<(string Guid, AsyncOperationHandle<GameObject> Handle)> GhostLoads = [];

        private static bool _open;
        private static string _search = string.Empty;
        private static List<(string Key, string Label, MapObjectDefinition Source)>? _matches;
        private static int _firstRow;
        private static List<(string Key, string Label, MapObjectDefinition Source)>? _entries;
        private static string? _placingKey;
        private static MapObjectDefinition? _placingSource;
        private static string? _ghostScene;
        private static GameObject? _ghost;
        private static AsyncOperationHandle<GameObject>? _ghostHandle;
        private static string? _selectedId;
        private static Vector2 _dragOffset;
        private static bool _dragging;
        private static bool _hiding;
        private static bool _stamping;
        private static string? _selectedArea;
        private static Vector2? _areaStart;
        private static bool _areaMoved;
        private static Transform? _hideCandidate;

        internal static void Toggle() => SetOpen(!_open);

        /// <summary>
        /// Draws the window and handles clicks in the world. Called from the API melon's <c>OnGUI</c>.
        /// </summary>
        internal static void Draw()
        {
            FinishGhostLoads();
            if (!_open)
            {
                return;
            }

            Rect panel = new(Margin, Top, Width, Screen.height - Top - Margin);
            GUI.Box(panel, GUIContent.none, FlatGuiStyles.Panel);
            float x = panel.x + 10f;
            float y = panel.y + 8f;
            float width = Width - 20f;

            GUI.Label(new Rect(x, y, width, Row), "Map Editor", FlatGuiStyles.Title);
            Event key = Event.current;
            bool undoKey = key.type == EventType.KeyDown && key.keyCode == KeyCode.Z && key.control;
            if (GUI.Button(new Rect(panel.xMax - 174f, y, 80f, Row), "Undo", MapLayoutSystem.CanUndo ? FlatGuiStyles.Button : FlatGuiStyles.DisabledButton) || undoKey)
            {
                if (undoKey)
                {
                    key.Use();
                }

                StopPlacing();
                _selectedId = null;
                _selectedArea = null;
                MapLayoutSystem.Undo();
            }

            if (GUI.Button(new Rect(panel.xMax - 90f, y, 80f, Row), "Close", FlatGuiStyles.Button))
            {
                SetOpen(false);
                return;
            }

            y += Row + 4f;
            string hint = _hiding ? "Click: pick a vanilla object. Right click: cancel. T: jump here."
                : "Click: place or select. Drag: move. Right click: cancel. T: jump here.";
            GUI.Label(new Rect(x, y, width, Row), hint, FlatGuiStyles.Hint);
            y += Row;

            y = DrawHiding(x, y, width);
            if (!_hiding)
            {
                y = DrawSelection(x, y, width);
                DrawPrefabList(panel, x, y, width);
            }

            HandleWorldInput(panel);
            if (Event.current.type == EventType.Repaint)
            {
                DrawWorldMarkers();
            }
        }

        private static float DrawHiding(float x, float y, float width)
        {
            float half = (width - 4f) / 2f;
            if (GUI.Button(new Rect(x, y, half, Row), "Hide vanilla objects", _hiding ? FlatGuiStyles.SelectedButton : FlatGuiStyles.Button))
            {
                _hiding = !_hiding;
                _hideCandidate = null;
                StopPlacing();
                _selectedId = null;
            }

            int removed = MapLayoutSystem.RemovedCount;
            GUIStyle restoreStyle = removed > 0 ? FlatGuiStyles.Button : FlatGuiStyles.DisabledButton;
            if (GUI.Button(new Rect(x + half + 4f, y, half, Row), $"Restore last ({removed})", restoreStyle))
            {
                MapLayoutSystem.UnhideLast();
            }

            y += Row + 4f;
            Transform? candidate = _hideCandidate;
            if (!_hiding || candidate == null)
            {
                return y;
            }

            GUI.Label(new Rect(x, y, width, Row), candidate.name, FlatGuiStyles.Label);
            y += Row;
            GUI.Label(new Rect(x, y, width, Row), candidate.gameObject.scene.name, FlatGuiStyles.Hint);
            y += Row + 2f;
            DrawButtons(x, y, width,
                ("Parent", () => _hideCandidate = candidate.parent != null ? candidate.parent : candidate),
                ("Hide", () =>
                {
                    MapLayoutSystem.Hide(candidate.gameObject);
                    _hideCandidate = null;
                }));
            return y + Row + 8f;
        }

        private static float DrawSelection(float x, float y, float width)
        {
            MapAreaDefinition? area = SelectedArea();
            if (area != null)
            {
                GUI.Label(new Rect(x, y, width, Row), $"{area.Id}  ({area.X:0}, {area.Y:0})  {area.Width:0} x {area.Height:0}", FlatGuiStyles.Label);
                y += Row + 2f;
                DrawButtons(x, y, width,
                    ("W-", () => ResizeArea(area, -AreaStep, 0f)),
                    ("W+", () => ResizeArea(area, AreaStep, 0f)),
                    ("H-", () => ResizeArea(area, 0f, -AreaStep)),
                    ("H+", () => ResizeArea(area, 0f, AreaStep)));
                y += Row + 4f;
                DrawButtons(x, y, width,
                    (area.Indoor ? "Indoor: on" : "Indoor: off", () =>
                    {
                        area.Indoor = !area.Indoor;
                        MapLayoutSystem.UpdateArea(area);
                    }),
                    ("Delete area", () =>
                    {
                        MapLayoutSystem.RemoveArea(area.Id);
                        _selectedArea = null;
                    }));
                return y + Row + 8f;
            }

            if (_selectedId == null || !MapLayoutSystem.Objects.TryGetValue(_selectedId, out MapObjectDefinition? selected))
            {
                return y;
            }

            GUI.Label(new Rect(x, y, width, Row), $"{selected.Id}  ({selected.X:0}, {selected.Y:0})  x{selected.Scale:0.0}", FlatGuiStyles.Label);
            y += Row + 2f;
            if (selected.IsBlocker)
            {
                DrawButtons(x, y, width,
                    ("W-", () => Resize(selected, -SizeStep, 0f)),
                    ("W+", () => Resize(selected, SizeStep, 0f)),
                    ("H-", () => Resize(selected, 0f, -SizeStep)),
                    ("H+", () => Resize(selected, 0f, SizeStep)),
                    ("Delete", () => Delete(selected)));
            }
            else
            {
                DrawButtons(x, y, width,
                    ("Flip", () =>
                    {
                        selected.FlipX = !selected.FlipX;
                        MapLayoutSystem.Update(selected);
                    }),
                    ("Smaller", () =>
                    {
                        selected.Scale = Mathf.Max(ScaleStep, selected.Scale - ScaleStep);
                        MapLayoutSystem.Update(selected);
                    }),
                    ("Bigger", () =>
                    {
                        selected.Scale += ScaleStep;
                        MapLayoutSystem.Update(selected);
                    }),
                    ("Turn", () =>
                    {
                        selected.Rotation = (selected.Rotation + TurnStep) % 360f;
                        MapLayoutSystem.Update(selected);
                    }),
                    ("Delete", () => Delete(selected)));
            }

            return y + Row + 8f;
        }

        private static void DrawButtons(float x, float y, float width, params (string Label, Action OnClick)[] buttons)
        {
            float button = (width - 4f * (buttons.Length - 1)) / buttons.Length;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (GUI.Button(new Rect(x + (button + 4f) * i, y, button, Row), buttons[i].Label, FlatGuiStyles.Button))
                {
                    buttons[i].OnClick();
                }
            }
        }

        private static void Resize(MapObjectDefinition blocker, float width, float height)
        {
            blocker.Width = Mathf.Max(SizeStep, blocker.Width + width);
            blocker.Height = Mathf.Max(SizeStep, blocker.Height + height);
            MapLayoutSystem.Update(blocker);
        }

        private static void ResizeArea(MapAreaDefinition area, float width, float height)
        {
            area.Width = Mathf.Max(MinAreaSize, area.Width + width);
            area.Height = Mathf.Max(MinAreaSize, area.Height + height);
            MapLayoutSystem.UpdateArea(area);
        }

        private static MapAreaDefinition? SelectedArea() => _selectedArea == null ? null : MapAreas.All.FirstOrDefault(candidate => candidate.Id == _selectedArea);

        private static void Delete(MapObjectDefinition selected)
        {
            MapLayoutSystem.Remove(selected.Id);
            _selectedId = null;
        }

        private static void DrawPrefabList(Rect panel, float x, float y, float width)
        {
            float third = (width - 8f) / 3f;
            if (GUI.Button(new Rect(x, y, third, Row), "Blocker", _placingKey == BlockerKey ? FlatGuiStyles.SelectedButton : FlatGuiStyles.Button))
            {
                StartPlacing(BlockerKey, new MapObjectDefinition());
            }

            if (GUI.Button(new Rect(x + third + 4f, y, third, Row), "Paint ground", _stamping ? FlatGuiStyles.SelectedButton : FlatGuiStyles.Button))
            {
                _stamping = !_stamping;
            }

            if (GUI.Button(new Rect(x + (third + 4f) * 2f, y, third, Row), "Area", _placingKey == AreaKey ? FlatGuiStyles.SelectedButton : FlatGuiStyles.Button))
            {
                StopPlacing();
                _placingKey = AreaKey;
                _selectedId = null;
                _selectedArea = null;
            }

            y += Row + 4f;
            string search = GUI.TextField(new Rect(x, y, width, Row), _search, FlatGuiStyles.Field);
            if (search != _search)
            {
                _search = search;
                _firstRow = 0;
                _matches = null;
            }

            y += Row + 4f;
            List<(string Key, string Label, MapObjectDefinition Source)> matches = _matches ??= GetEntries()
                .Where(entry => _search.Length == 0 || entry.Label.Contains(_search, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Rect list = new(x, y, width, panel.yMax - y - 8f);
            int visible = Mathf.Max(1, (int)(list.height / (Row + 2f)));
            Event current = Event.current;
            if (current.type == EventType.ScrollWheel && list.Contains(current.mousePosition))
            {
                _firstRow += current.delta.y > 0 ? 3 : -3;
                current.Use();
            }

            _firstRow = Mathf.Clamp(_firstRow, 0, Mathf.Max(0, matches.Count - visible));
            for (int i = _firstRow; i < matches.Count && i < _firstRow + visible; i++)
            {
                (string key, string label, MapObjectDefinition source) = matches[i];
                GUIStyle style = key == _placingKey ? FlatGuiStyles.SelectedButton : FlatGuiStyles.Button;
                if (GUI.Button(new Rect(x, list.y + (i - _firstRow) * (Row + 2f), width, Row), label, style))
                {
                    StartPlacing(key, source);
                }
            }
        }

        /// <summary>
        /// Everything placeable: the game's dynamic objects (traps), the props baked into chunk scenes, and the addressable prefabs.
        /// </summary>
        private static List<(string Key, string Label, MapObjectDefinition Source)> GetEntries()
        {
            if (_entries != null)
            {
                return _entries;
            }

            _entries = [];
            Il2CppCollections.List<SubDatabase_DynamicSaveObjects.Entry> dynamics = GameDatabase.Instance.DynamicObjects._data;
            for (int i = 0; i < dynamics.Count; i++)
            {
                string assetId = dynamics[i].AssetId;
                _entries.Add((DynamicKeyPrefix + assetId, assetId + "   Dynamic", new MapObjectDefinition { Dynamic = assetId }));
            }

            foreach ((string name, string category, string scene, string path) in MapScenePropCatalog.Props)
            {
                _entries.Add((scene + "|" + path, name + "   " + category, new MapObjectDefinition { Scene = scene, Path = path }));
            }

            foreach ((string path, string guid) in MapPrefabCatalog.Prefabs)
            {
                _entries.Add((guid, MapPrefabCatalog.NameOf(path) + "   " + MapPrefabCatalog.CategoryOf(path), new MapObjectDefinition { Prefab = guid }));
            }

            return _entries;
        }

        private static void HandleWorldInput(Rect panel)
        {
            Event current = Event.current;
            if (panel.Contains(current.mousePosition) || !TryGetMouseWorld(current.mousePosition, out Vector2 world))
            {
                if (current.type == EventType.MouseUp)
                {
                    _dragging = false;
                    _areaStart = null;
                }

                return;
            }

            if (_ghost != null && current.type is EventType.Repaint or EventType.MouseMove or EventType.MouseDrag)
            {
                _ghost.transform.position = new Vector3(world.x, world.y, 0f);
            }

            switch (current.type)
            {
                case EventType.MouseDown when current.button == 0 && _placingKey == AreaKey:
                    _areaStart = world;
                    current.Use();
                    break;
                case EventType.MouseUp when current.button == 0 && _placingKey == AreaKey && _areaStart is { } start:
                    _areaStart = null;
                    Vector2 size = new(Mathf.Abs(world.x - start.x), Mathf.Abs(world.y - start.y));
                    if (size.x >= MinAreaSize && size.y >= MinAreaSize)
                    {
                        Vector2 center = (start + world) / 2f;
                        _selectedArea = MapLayoutSystem.AddArea(new MapAreaDefinition { X = center.x, Y = center.y, Width = size.x, Height = size.y }).Id;
                        _placingKey = null;
                    }

                    current.Use();
                    break;
                case EventType.MouseDown when current.button == 0 && _hiding:
                    _hideCandidate = PickVanilla(world);
                    current.Use();
                    break;
                case EventType.MouseDown when current.button == 0 && _placingSource != null:
                    _selectedId = MapLayoutSystem.Add(new MapObjectDefinition
                    {
                        Prefab = _placingSource.Prefab,
                        Scene = _placingSource.Scene,
                        Path = _placingSource.Path,
                        Dynamic = _placingSource.Dynamic,
                        Stamp = _stamping && !_placingSource.IsBlocker,
                        X = world.x,
                        Y = world.y
                    }).Id;
                    current.Use();
                    break;
                case EventType.MouseDown when current.button == 0:
                    _selectedId = PickAt(world);
                    MapAreaDefinition? pickedArea = _selectedId == null ? MapAreas.At(world) : null;
                    _selectedArea = pickedArea?.Id;
                    _dragging = _selectedId != null || pickedArea != null;
                    _areaMoved = false;
                    if (_selectedId != null && MapLayoutSystem.Objects.TryGetValue(_selectedId, out MapObjectDefinition? picked))
                    {
                        _dragOffset = new Vector2(picked.X, picked.Y) - world;
                    }
                    else if (pickedArea != null)
                    {
                        _dragOffset = new Vector2(pickedArea.X, pickedArea.Y) - world;
                    }

                    current.Use();
                    break;
                case EventType.MouseDrag when _dragging && _selectedId != null && MapLayoutSystem.Objects.TryGetValue(_selectedId, out MapObjectDefinition? dragged):
                    dragged.X = Mathf.Round(world.x + _dragOffset.x);
                    dragged.Y = Mathf.Round(world.y + _dragOffset.y);
                    if (MapLayoutSystem.TryGetInstance(dragged.Id, out GameObject? instance))
                    {
                        instance.transform.position = new Vector3(dragged.X, dragged.Y, 0f);
                    }

                    current.Use();
                    break;
                case EventType.MouseDrag when _dragging && SelectedArea() is { } draggedArea:
                    draggedArea.X = Mathf.Round(world.x + _dragOffset.x);
                    draggedArea.Y = Mathf.Round(world.y + _dragOffset.y);
                    _areaMoved = true;
                    current.Use();
                    break;
                case EventType.MouseUp when current.button == 0:
                    if (_dragging && _selectedId != null && MapLayoutSystem.Objects.TryGetValue(_selectedId, out MapObjectDefinition? moved))
                    {
                        MapLayoutSystem.Update(moved);
                    }
                    else if (_dragging && _areaMoved && SelectedArea() is { } movedArea)
                    {
                        MapLayoutSystem.UpdateArea(movedArea);
                    }

                    _areaMoved = false;

                    _dragging = false;
                    break;
                case EventType.MouseDown when current.button == 1:
                    StopPlacing();
                    _selectedId = null;
                    _selectedArea = null;
                    _areaStart = null;
                    _hideCandidate = null;
                    current.Use();
                    break;
                case EventType.KeyDown when current.keyCode == KeyCode.T:
                    if (PlayerAccess.TryGetPlayer(out Actor? player))
                    {
                        player.TeleportTo(world, portWithGroup: false, updateCam: true);
                    }

                    current.Use();
                    break;
            }
        }

        /// <summary>
        /// The placed prop or blocker under the click: inside a blocker, or the prop whose position is nearest within reach.
        /// </summary>
        private static string? PickAt(Vector2 world)
        {
            string? best = null;
            float bestDistance = PickRadius;
            foreach (MapObjectDefinition definition in MapLayoutSystem.Objects.Values)
            {
                Vector2 position = new(definition.X, definition.Y);
                if (definition.IsBlocker && new Rect(position - BlockerSize(definition) / 2f, BlockerSize(definition)).Contains(world))
                {
                    return definition.Id;
                }

                float distance = Vector2.Distance(world, position);
                if (distance < bestDistance)
                {
                    best = definition.Id;
                    bestDistance = distance;
                }
            }

            return best;
        }

        /// <summary>
        /// The smallest vanilla renderer under the click. Actors and the layout's own props are skipped.
        /// </summary>
        internal static Transform? PickVanilla(Vector2 world)
        {
            Transform? best = null;
            float bestArea = float.MaxValue;
            foreach (Renderer renderer in Object.FindObjectsOfType<Renderer>())
            {
                if (!renderer.enabled)
                {
                    continue;
                }

                Bounds bounds = renderer.bounds;
                float area = bounds.size.x * bounds.size.y;
                if (area >= bestArea || world.x < bounds.min.x || world.x > bounds.max.x || world.y < bounds.min.y || world.y > bounds.max.y)
                {
                    continue;
                }

                if (MapLayoutSystem.IsOwn(renderer.transform) || renderer.GetComponentInParent<Actor>() != null)
                {
                    continue;
                }

                best = renderer.transform;
                bestArea = area;
            }

            return best;
        }

        private static void StartPlacing(string key, MapObjectDefinition source)
        {
            StopPlacing();
            _hiding = false;
            _placingKey = key;
            _placingSource = source;
            _selectedId = null;
            if (source.Scene.Length > 0)
            {
                _ghostScene = source.Scene;
                MapSceneTemplates.Use(source.Scene);
            }
            if (source.Prefab.Length > 0)
            {
                GhostLoads.Add((key, new AssetReferenceGameObject(source.Prefab).InstantiateAsync(Vector3.zero, Quaternion.identity, null)));
            }
        }

        private static void StopPlacing()
        {
            _placingKey = null;
            _placingSource = null;
            if (_ghostScene != null)
            {
                MapSceneTemplates.Unuse(_ghostScene);
                _ghostScene = null;
            }
            if (_ghostHandle != null)
            {
                ReleaseGhost(_ghostHandle);
            }
            else if (_ghost != null)
            {
                Object.Destroy(_ghost);
            }

            _ghost = null;
            _ghostHandle = null;
        }

        /// <summary>
        /// Takes the ghost of the prefab being placed, with its colliders off, and releases every other finished load.
        /// Copies of world props and dynamic objects get a ghost of their sprites only, so no saveable copy wakes up.
        /// </summary>
        private static void FinishGhostLoads()
        {
            if (_ghost == null && _placingSource is { } placing && (placing.Scene.Length > 0 || placing.Dynamic.Length > 0))
            {
                GameObject? source = null;
                if (placing.Dynamic.Length > 0)
                {
                    source = GameDatabase.Instance.DynamicObjects.TryGetPrefab(placing.Dynamic, out SaveRoot_Dynamic prefab) && prefab != null ? prefab.gameObject : null;
                }
                else
                {
                    MapSceneTemplates.TryGet(placing.Scene, placing.Path, out source);
                }

                if (source != null)
                {
                    _ghost = MapStamps.CopySprites(source, "Map_Ghost");
                }
            }

            for (int i = GhostLoads.Count - 1; i >= 0; i--)
            {
                (string guid, AsyncOperationHandle<GameObject> handle) = GhostLoads[i];
                if (!handle.IsDone)
                {
                    continue;
                }

                GhostLoads.RemoveAt(i);
                GameObject? loaded = handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null;
                if (loaded == null || guid != _placingKey || _ghost != null)
                {
                    ReleaseGhost(handle);
                    continue;
                }

                foreach (Collider2D collider in loaded.GetComponentsInChildren<Collider2D>(true))
                {
                    collider.enabled = false;
                }

                _ghost = loaded;
                _ghostHandle = handle;
            }
        }

        private static void ReleaseGhost(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.IsValid())
            {
                Addressables.ReleaseInstance(handle);
            }
        }

        private static void DrawWorldMarkers()
        {
            foreach (MapObjectDefinition definition in MapLayoutSystem.Objects.Values)
            {
                if (definition.IsBlocker)
                {
                    DrawWorldRect(new Vector2(definition.X, definition.Y), BlockerSize(definition));
                }
            }

            foreach (MapAreaDefinition area in MapAreas.All)
            {
                DrawWorldRect(new Vector2(area.X, area.Y), new Vector2(area.Width, area.Height));
            }

            if (_placingKey == BlockerKey && TryGetMouseWorld(Event.current.mousePosition, out Vector2 mouse))
            {
                DrawWorldRect(mouse, new Vector2(64f, 64f));
            }

            if (_areaStart is { } start && TryGetMouseWorld(Event.current.mousePosition, out Vector2 corner))
            {
                DrawWorldRect((start + corner) / 2f, new Vector2(Mathf.Abs(corner.x - start.x), Mathf.Abs(corner.y - start.y)));
            }

            if (_hiding && _hideCandidate != null)
            {
                Bounds? bounds = null;
                foreach (Renderer renderer in _hideCandidate.GetComponentsInChildren<Renderer>())
                {
                    Bounds next = renderer.bounds;
                    if (bounds is { } current)
                    {
                        current.Encapsulate(next);
                        next = current;
                    }

                    bounds = next;
                }

                if (bounds is { } area)
                {
                    DrawWorldRect(area.center, area.size);
                }
            }

            if (_selectedId == null || !MapLayoutSystem.Objects.TryGetValue(_selectedId, out MapObjectDefinition? selected) || Camera.main == null)
            {
                return;
            }

            Vector3 screen = Camera.main.WorldToScreenPoint(new Vector3(selected.X, selected.Y, 0f));
            GUI.Box(new Rect(screen.x - 24f, Screen.height - screen.y - 24f, 48f, 48f), GUIContent.none, FlatGuiStyles.Outline);
        }

        private static void DrawWorldRect(Vector2 center, Vector2 size)
        {
            Camera? camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            Vector3 min = camera.WorldToScreenPoint(center - size / 2f);
            Vector3 max = camera.WorldToScreenPoint(center + size / 2f);
            GUI.Box(Rect.MinMaxRect(min.x, Screen.height - max.y, max.x, Screen.height - min.y), GUIContent.none, FlatGuiStyles.Outline);
        }

        private static Vector2 BlockerSize(MapObjectDefinition blocker)
        {
            float scale = blocker.Scale <= 0f ? 1f : blocker.Scale;
            return new Vector2(blocker.Width, blocker.Height) * scale;
        }

        private static bool TryGetMouseWorld(Vector2 guiPosition, out Vector2 world)
        {
            world = default;
            Camera? camera = Camera.main;
            if (camera == null)
            {
                return false;
            }

            Vector3 point = camera.ScreenToWorldPoint(new Vector3(guiPosition.x, Screen.height - guiPosition.y, -camera.transform.position.z));
            world = new Vector2(Mathf.Round(point.x), Mathf.Round(point.y));
            return true;
        }

        private static void SetOpen(bool open)
        {
            if (_open == open)
            {
                return;
            }

            _open = open;
            InputAccess.SetGameplayInputBlocked(InputOwner, open);
            if (!open)
            {
                StopPlacing();
                _selectedId = null;
                _selectedArea = null;
                _areaStart = null;
                _dragging = false;
                _hiding = false;
                _hideCandidate = null;
            }
        }
    }
}
