# Audio

**What it does:** provides **spoken dialogue audio** for dialogue lines. The API routes each
dialogue statement/choice to an `IAudioProvider`, which returns an `AudioClip`. One provider ships
in the box — `AssetBundleAudioProvider`, reading per-actor AssetBundles — and you can swap in your
own provider or handler.

Entry points:
- `Drova_Modding_API.Systems.Audio.AudioManager` (static) — swap the provider/handler, compute clip IDs.
- `IAudioProvider` — supplies clips. Built-in: `AssetBundleAudioProvider`.
- `IAudioConnector` / `IAudioHandler` — lower-level hooks the manager uses.

> The AssetBundle-backed connector is already installed, so shipping audio needs no registration
> code at all. Whether any audio plays is gated by the mod's **"enable dialogue audio"** option
> (see [Config](./config.md)).

## Quick example — ship per-actor AssetBundles

`AssetBundleAudioProvider` loads clips from per-actor bundles under `Audio/bundles/`, named after
the lower-cased actor (e.g. `jendrik`, `player`). Each bundle holds that actor's clips, addressed by
the key the dialogue system builds for the line, without a file extension.

```
…/Drova - Forsaken Kin/Mods/Modding_API/Audio/bundles/
  player        ← AssetBundle of the player's clips
  jendrik       ← AssetBundle of Jendrik's clips
```

Inside a bundle, the clip names follow the same scheme:

```
<dialogName>_<locaKey>_<filePath>_<actorName>
<dialogName>_<locaKey>_<filePath>            ← fallback without actor
<…>_CAVE                                     ← optional cave-specific variant
```

The provider builds that key from the dialogue tree name, the line's loca key, its file path, and
the actor. When the player is in a [cave](./area-region.md), a `_CAVE` variant is preferred if
present. Any audio format Unity can import works — the format is baked into the bundle, so nothing
is decoded at runtime by the API.

This is the default connector. Register it explicitly only to get back to it after swapping in
another provider:

```csharp
using Drova_Modding_API.Systems.Audio;

AudioManager.ReplaceDialogueAudioConnector(
    new DefaultDialogueAudioConnector(new AssetBundleAudioProvider()));
```

## How do I…?

#### Keep bundle loading off the main thread

`AssetBundleAudioProvider` resolves clips **synchronously on the main thread** when a dialogue tree
loads — Unity's `AssetBundle` APIs are main-thread-only under Il2Cpp, so the work cannot be moved
to a background thread. How the clips were imported into the bundle therefore decides whether
opening a dialogue stutters:

- **Set the clips' load type to `Compressed In Memory`** (or `Streaming`) and enable **Load In
  Background** when building the bundle. `LoadAsset` then only pulls the compressed bytes into memory
  and the decode happens lazily at play time.
- **Avoid `Decompress On Load`.** With it, every `LoadAsset` decodes the full PCM on the main thread
  during tree load — with many lines per tree that's the visible hang.

The provider also only calls `LoadAsset` for keys the bundle actually contains (misses cost a hash
lookup, not an Il2Cpp round-trip) and caches decoded clips, so the per-line cost is dominated by the
import setting above.

### Compute the clip ID for a line

If you build your own provider, `AudioManager` exposes the same ID scheme it uses internally:

```csharp
string id = AudioManager.GetUniqueIDStatement(tree, statementNode);
string choiceId = AudioManager.GetUniqueIDChoice(tree, choice);
// overloads taking raw (treeName, locaKey, path[, actorName]) also exist
```

### Write a custom provider

Implement `IAudioProvider` and return a clip for the requested line (or `null` for "no audio"):

```csharp
using Drova_Modding_API.Systems.Audio;
using UnityEngine;

public sealed class MyProvider : IAudioProvider
{
    public Task<AudioClip> GetAudioClip(string dialogeName, string filePath, string locaKey,
                                        string actorName, int? choiceId)
    {
        // Look up / synthesize a clip; return Task.FromResult<AudioClip>(null) to skip.
        return Task.FromResult(LoadClipSomehow(dialogeName, locaKey, actorName));
    }
}

AudioManager.ReplaceDialogueAudioConnector(new DefaultDialogueAudioConnector(new MyProvider()));
```

### Customize subtitle / choice handling

Swap the `IAudioHandler` to control how subtitle and multiple-choice audio requests are routed:

```csharp
AudioManager.ReplaceAudioHandler(new MyAudioHandler());
```

## API reference

### `AudioManager` (static)

| Member                                                                 | Description                                                            |
|------------------------------------------------------------------------|------------------------------------------------------------------------|
| `void ReplaceDialogueAudioConnector(IAudioConnector)`                  | Swap the connector (wraps your provider).                              |
| `void ReplaceAudioHandler(IAudioHandler)`                              | Swap the subtitle/choice handler.                                      |
| `string GetUniqueIDStatement(DialogueTree, DS_StatementNode)`          | Clip ID for a statement (overloads for raw strings / generic + actor). |
| `string GetUniqueIDChoice(DialogueTree, DS_MultipleChoiceNode.Choice)` | Clip ID for a choice.                                                  |
| `readonly string DEFAULT_CAVE_AUDIO_PREFIX`                            | `"_CAVE"` — suffix for cave-variant clips.                             |

### `IAudioProvider`

| Member                                                                                                               | Description                            |
|----------------------------------------------------------------------------------------------------------------------|----------------------------------------|
| `Task<AudioClip> GetAudioClip(string dialogeName, string filePath, string locaKey, string actorName, int? choiceId)` | Return the clip for a line, or `null`. |

Built-in: `AssetBundleAudioProvider` (per-actor bundles; `UnloadAll()` frees cached bundles).

### `IAudioHandler`

| Member                                                                         | Description              |
|--------------------------------------------------------------------------------|--------------------------|
| `void HandleSubtitleRequest(SubtitlesRequestInfo, DS_DialogueUGUI)`            | Handle subtitle display. |
| `void HandleMultipleChoiceRequest(MultipleChoiceRequestInfo, DS_DialogueUGUI)` | Handle choice audio.     |

### `IAudioConnector`

| Member                                            | Description                                        |
|---------------------------------------------------|----------------------------------------------------|
| `void OnDialogueTreeLoaded(DialogueTree)`         | Hook a tree's audio when it loads.                 |
| `void OnWorldDialogueStatement(DS_StatementNode)` | Load audio immediately for a world (generic) line. |

## Notes & gotchas

- **Audio is opt-in at runtime.** The built-in provider returns `null` (silent) unless the mod's
  "enable dialogue audio" option is on — see [Config](./config.md).
- **Clip IDs are derived from the dialogue tree + node** (tree name, loca key, file path, actor). The
  file/bundle name must match that key. Use `AudioManager.GetUniqueID*` to compute them.
- **AssetBundles can't be loaded twice from the same file** — `AssetBundleAudioProvider` caches them;
  call `UnloadAll()` if you need to free them.
- **Bundle clip import settings drive dialogue-open performance.** Use `Compressed In Memory` /
  `Streaming` + Load In Background, not `Decompress On Load` — see
  [Keep bundle loading off the main thread](#keep-bundle-loading-off-the-main-thread).
- **Loose `.ogg` files are no longer supported.** `FileAudioProvider` and its NVorbis decoder were
  removed in 0.6.0; pack the clips into a per-actor bundle instead. Cave variants keep the `_CAVE`
  suffix, now on the clip name rather than the file name.
- Register a custom provider during gameplay init; the built-in provider reads the
  [AreaNameSystem](./area-region.md) to pick cave variants, which only exists in gameplay.
