# World Utilities — Streamed Scenes, Sprite Overlays, Teleporters, GUI Content

Four small systems for mods that touch world content. Each encapsulates a trap that is easy to
hit and costly to rediscover.

## Streamed scenes (`SceneStreamAccess`)

Drova streams chunk scenes (`Scene_Overworld_<Layer>_<x>_<y>`, area interiors) continuously
while the player travels. World objects appear and disappear with their scene — anything you
parent onto them dies with them, and anything you search for may simply not be loaded yet.

```csharp
using Drova_Modding_API.Access;

SceneStreamAccess.AddSceneListener("Scene_Overworld_Interaction_14_37", scene =>
{
    foreach (var renderer in SceneStreamAccess.GetComponentsInScene<SpriteRenderer>(scene))
    {
        // only this scene's renderers - never the whole heap
    }
});
```

- `AddSceneListener(name, handler)` / `RemoveSceneListener` — called when that scene loads
  (and immediately if it is already loaded). Case-insensitive.
- `OnSceneLoaded` / `OnSceneUnloaded` — untargeted firehose variants; prefer targeted listeners.
- `GetComponentsInScene<T>(scene)` — scoped component sweep, includes inactive objects, can
  never return prefab assets.

**The performance rule:** never run `Resources.FindObjectsOfTypeAll` — or even a full-scene
renderer walk — on *every* scene load. Chunk scenes stream constantly, carry thousands of
sprites, and every check is an interop call; doing this per-load tanks the frame rate while
walking. Determine offline (or once) *which* scenes hold your
objects, register listeners for exactly those, and let every other load cost one dictionary
lookup. Scene names can be extracted from the game bundles: the `AssetBundle` object's
`m_SceneHashes` maps scene paths to serialized-file CABs (note the cab list's order does NOT
match the container order, and casing differs).

## Sprite overlays (`Systems.Rendering.SpriteOverlay`)

Draw mod content over existing world art (signs, plates, boards) without it looking pasted on:

```csharp
using Drova_Modding_API.Systems.Rendering;

// CreateRuntimeSprite returns null if pixels.Length != w*h or the dimensions are invalid.
Sprite? sprite = SpriteOverlay.CreateRuntimeSprite(pixels, w, h, parent.sprite.pixelsPerUnit, "MyMod_Overlay");
GameObject? overlay = sprite == null ? null : SpriteOverlay.Create(parent, sprite, localOffset, "MyMod_Overlay");
// later:
SpriteOverlay.Destroy(overlay);
```

What it handles for you:

1. **Material** — Unity's default sprite material is unlit and lacks Drova's shader properties;
   overlays copy the parent's Just2D material so lighting, water, and tinting work.
2. **Interact highlight** — the outline/flash goes through the art's `Just2D_SpriteRenderer`
   wrapper (`_OverrideColor`/`_OutlineColor` property blocks on every renderer in its list);
   overlays register there and flash in lockstep with the art. `Destroy` deregisters first.
3. **Atlas trim** — packed sprites are trimmed (a 72×72 plate stores 67×67 content at
   ~(2.08, 3.08)); `Sprite.pivot` is rect-relative, so positions measured on the packed image
   must add `TryGetPackedContentOffset`.
4. **Pixel-art settings** — point filtering, clamp, `HideAndDontSave`, centered pivot.

Give your runtime assets a recognizable name prefix and skip assets that already carry it, so a
swapped asset is never re-swapped.

## Teleporters (`TeleporterAccess`)

Every area transition in Drova is an `OW_Teleporter` whose serialized `_targetPos` /
`_targetMoveDir` is an authored arrival anchor beside its partner gate; links are strict
bidirectional pairs.

```csharp
using Drova_Modding_API.Access;

TeleporterAccess.SetDestinationOverride("Teleporter_CaveX_Mouth", new TeleporterAccess.GateDestination
{
    TargetPos = new Vector2(1234f, 5678f),
    TargetMoveDir = new Vector2(0f, -1f),
});
```

Overrides are enforced persistently: on every existing gate immediately, on each gate as its
chunk streams in (OnEnable), and re-applied when the player is found — a connect-time sweep can
run before `Scene_Teleporters` has instantiated its gates. Mods that never register an override
pay nothing (the hook is lazy). Registering many overrides at once runs one heap sweep instead
of one per call — prefer `SetDestinationOverrides(...)` over a loop of `SetDestinationOverride`.

**Safety rule:** rewire whole pairs, never single directions — a one-way rewire can strand the
player. Given vanilla (M1↔I1) and (M2↔I2), a safe shuffle produces (M1↔I2)/(M2↔I1) by swapping
anchors on all four gates.

## Letters & drawing puzzles (`WindowContentAccess`)

- `OnLetterShown` — fires after `GUI_Window_Letter` builds its content, for world letters *and*
  journal re-reads (the two-arg `ShowLetterContent` overload is the single funnel; the one-arg
  overload delegates to it). Mutate the window's children in the handler, e.g. swap
  `Image.sprite`s.
- `RegisterDrawingTargetTransformer(func)` — replace the rune-drawing window's required pattern
  before it is compared. The drawn result is checked per-pixel against your replacement (alpha,
  plus RGB for multi-color windows), so replacements must be pixel-exact 8×8 grids.

Both hooks are lazy — nothing is patched until the first subscription.

## Debug: `api_dumpnearby` (Debug builds only)

Debug builds register the `api_dumpnearby [radius]` console command: stand next to a world
object and dump every renderer around the player (position, GameObject name, sprite name) to
the log. Useful when art is hard to find statically — it can sit far from the object it belongs
to. Sprite names are the stable identification key; GameObject names are often generic.

## Notes & gotchas

- `Resources.FindObjectsOfTypeAll` returns **prefab assets** along with scene objects; always
  check `gameObject.scene.IsValid()` before mutating, or you corrupt every future instance of
  the prefab. (`GetComponentsInScene` cannot hit this by construction.)
- Types the game declares without a namespace land in the interop namespace `Il2Cpp`
  (e.g. `Il2Cpp.Just2D_SpriteRenderer` from `Il2CppJust2D.dll`).
- IL2CPP interop exposes private fields/methods as public members — no reflection needed
  (`teleporter._targetPos` above is a private serialized field).
