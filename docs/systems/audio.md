# Audio

**What it does:** provides **spoken dialogue audio** for dialogue lines. The API routes each
dialogue statement/choice to an `IAudioProvider`, which returns an `AudioClip`. Two providers ship
in the box: one that reads loose `.ogg` files, and one that reads per-actor AssetBundles. You can
swap in your own provider or handler.

Entry points:
- `Drova_Modding_API.Systems.Audio.AudioManager` (static) — swap the provider/handler, compute clip IDs.
- `IAudioProvider` — supplies clips. Built-ins: `FileAudioProvider`, `AssetBundleAudioProvider`.
- `IAudioConnector` / `IAudioHandler` — lower-level hooks the manager uses.

> By default, the manager uses an AssetBundle-backed connector. Whether any audio plays at all is
> gated by the mod's **"enable dialogue audio"** option (see [Config](./config.md)).

## Quick example — ship loose `.ogg` files

The simplest setup: use `FileAudioProvider` and drop `.ogg` files into the audio folder.

```csharp
using Drova_Modding_API.Systems.Audio;

// Register once during mod init (e.g. on gameplay load):
AudioManager.ReplaceDialogueAudioConnector(
    new DefaultDialogueAudioConnector(new FileAudioProvider()));
```

Files live under:

```
…/Drova - Forsaken Kin/Mods/Modding_API/Audio/
  <dialogName>_<locaKey>_<filePath>_<actorName>.ogg
  <dialogName>_<locaKey>_<filePath>.ogg            ← fallback without actor
  <…>_CAVE.ogg                                     ← optional cave-specific variant
```

The provider builds the file name from the dialogue tree name, the line's loca key, its file path,
and the actor, then loads the matching `.ogg`. When the player is in a [cave](./area-region.md), a
`_CAVE` variant is preferred if present.

## How do I…?

### Use AssetBundles instead of loose files

`AssetBundleAudioProvider` loads clips from per-actor bundles under `Audio/bundles/`, named after
the lower-cased actor (e.g. `jendrik`, `player`). Each bundle holds that actor's clips, addressed by
the same key the file provider uses (minus the `.ogg`).

```csharp
AudioManager.ReplaceDialogueAudioConnector(
    new DefaultDialogueAudioConnector(new AssetBundleAudioProvider()));
```

```
…/Modding_API/Audio/bundles/
  player        ← AssetBundle of the player's clips
  jendrik       ← AssetBundle of Jendrik's clips
```

#### Keep bundle loading off the main thread

`AssetBundleAudioProvider` resolves clips **synchronously on the main thread** when a dialogue tree
loads (Unity's `AssetBundle` APIs are main-thread-only under Il2Cpp, so this can't be moved to a
background thread the way the `.ogg` provider does). How the clips were imported into the bundle
therefore decides whether opening a dialogue stutters:

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

Built-ins: `FileAudioProvider` (loose `.ogg`), `AssetBundleAudioProvider` (per-actor bundles;
`UnloadAll()` frees cached bundles).

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

- **Audio is opt-in at runtime.** Both built-in providers return `null` (silent) unless the mod's
  "enable dialogue audio" option is on — see [Config](./config.md).
- **Clip IDs are derived from the dialogue tree + node** (tree name, loca key, file path, actor). The
  file/bundle name must match that key. Use `AudioManager.GetUniqueID*` to compute them.
- **AssetBundles can't be loaded twice from the same file** — `AssetBundleAudioProvider` caches them;
  call `UnloadAll()` if you need to free them.
- **Bundle clip import settings drive dialogue-open performance.** Use `Compressed In Memory` /
  `Streaming` + Load In Background, not `Decompress On Load` — see
  [Keep bundle loading off the main thread](#keep-bundle-loading-off-the-main-thread).
- **`.ogg` only** for the file provider (decoded via NVorbis). Cave variants use the `_CAVE` suffix.
- Register your provider during gameplay init; the providers read the [AreaNameSystem](./area-region.md)
  to pick cave variants, which only exist in gameplay.
