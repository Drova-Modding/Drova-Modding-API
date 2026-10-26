"""Lists the prop kinds baked into Drova's overworld chunk scenes, with one source scene and path per kind.

Writes the map editor's scene prop catalog, Drova Modding API/Systems/MapEditor/MapScenePropCatalog.g.cs, from the AssetRipper
export of the game. Rerun after a game update and a fresh export:
    python Python/scenePropCatalog.py ["<export Assets dir>"]

A prop is a GameObject three levels deep (<category root>/<sublayer>/<prop>) with a SpriteRenderer in its subtree. Kinds that are
already addressable environment prefabs (MapPrefabCatalog.g.cs), actors, observers, perception clues and hit volumes are left out.
"""
import collections
import glob
import multiprocessing
import os
import re
import sys

ASSETS = sys.argv[1] if len(sys.argv) > 1 else r"C:\Users\fpabs\Documents\Projekte\DrovaDecompiled\ExportedProject\Assets"
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "Drova Modding API", "Systems", "MapEditor", "MapScenePropCatalog.g.cs")
PREFABS = os.path.join(HERE, "..", "Drova Modding API", "Systems", "MapEditor", "MapPrefabCatalog.g.cs")

# The export references every MonoBehaviour by an assembly guid plus fileID = MD4("s\0\0\0" + namespace + class)[:4]; only the
# classes that decide the tier or exclude a kind are named, every other script counts as gameplay.
SCRIPTS = {
    393728827: "OW_ActivatedObject", -631205108: "WindEffected", -130292110: "Just2D_SpriteRenderer", -272679315: "GuidComponent",
    1494763865: "TransformGuidComponent", 700178070: "SaveRoot_Guid", -755969736: "NonFloorBaking", -1317915998: "Fading_SpriteRenderer",
    1259376928: "BecameVisibleCallback", -1640194001: "RainBlocker", 1853146046: "DisableGameObjectThroughOption",
    570157379: "CullAnimatorPlaybackGraph", 866761768: "OnStart_SyncAnimWithTilemap", 1543645015: "OnStart_SetAnimatorFloatParam",
    1486955471: "PeriodicAnimation", -599546215: "FireFlicker", 1783474909: "AnimatedSpriteModule",
    -1669509954: "Actor", -667238774: "LazyActor",
}
STATIC = {"SpriteRenderer", "OW_ActivatedObject", "PolygonCollider2D", "BoxCollider2D", "CircleCollider2D", "CapsuleCollider2D",
          "EdgeCollider2D", "Rigidbody2D", "SortingGroup", "WindEffected", "Just2D_SpriteRenderer", "GuidComponent", "TransformGuidComponent",
          "SaveRoot_Guid", "NonFloorBaking", "Fading_SpriteRenderer", "BecameVisibleCallback", "RainBlocker", "DisableGameObjectThroughOption"}
ANIM = {"Animator", "CullAnimatorPlaybackGraph", "OnStart_SyncAnimWithTilemap", "OnStart_SetAnimatorFloatParam", "PeriodicAnimation",
        "ParticleSystem", "ParticleSystemRenderer", "FireFlicker", "AnimatedSpriteModule"}
LOGIC_NAME = re.compile(r"OBS|Observer|\w*Bhvrs|Filter|Perception|HitReceiver")
VARIANT = re.compile(r"[_/](Chap\d|Destroyed|Untouched)")
DOC = re.compile(r"^--- !u!(\d+) &(-?\d+)[^\n]*\n(\w+):\n", re.M)
FID = re.compile(r"fileID: (-?\d+)")


def field(body, name):
    m = re.search(r"^  " + name + r": ?(.*)$", body, re.M)
    return m.group(1).strip() if m else ""


def scan(path):
    """Every prop-level instance of one scene as (name, component types, scene, path, root name duplicated, any name duplicated)."""
    text = open(path, encoding="utf-8", errors="replace").read()
    heads = list(DOC.finditer(text))
    names, comps, types, father, children, go_of = {}, {}, {}, {}, {}, {}
    for i, m in enumerate(heads):
        oid, kind = int(m.group(2)), m.group(3)
        body = text[m.end(): heads[i + 1].start() if i + 1 < len(heads) else len(text)]
        if kind == "GameObject":
            names[oid] = field(body, "m_Name")
            comps[oid] = [int(x) for x in re.findall(r"component: \{fileID: (-?\d+)\}", body)]
        elif kind in ("Transform", "RectTransform"):
            go_of[oid] = int(FID.search(field(body, "m_GameObject")).group(1))
            father[oid] = int(FID.search(field(body, "m_Father")).group(1))
            children[oid] = [int(x) for x in re.findall(r"^  - \{fileID: (-?\d+)\}", body.split("m_Children:")[1].split("m_Father:")[0], re.M)]
        elif kind == "MonoBehaviour":
            types[oid] = SCRIPTS.get(int(FID.search(field(body, "m_Script")).group(1)), "MonoBehaviour")
        else:
            types[oid] = kind

    def segments(siblings):
        seen, dup = collections.Counter(), collections.Counter(names[go_of[t]] for t in siblings)
        for t in siblings:
            name = names[go_of[t]]
            k = seen[name]
            seen[name] += 1
            yield t, name + ("[%d]" % k if k or name.endswith("]") else ""), dup[name] > 1

    def subtree(t, acc):
        acc.update(types.get(c, "") for c in comps[go_of[t]])
        for c in children[t]:
            subtree(c, acc)
        return acc

    scene = os.path.basename(path)[:-6]
    roots = sorted(t for t, f in father.items() if f == 0)
    out = []
    for r, rseg, rdup in segments(roots):
        for s, sseg, sdup in segments(children[r]):
            for p, pseg, pdup in segments(children[s]):
                kinds = subtree(p, set()) - {"Transform", "RectTransform", ""}
                out.append((names[go_of[p]], kinds, scene, "/".join((rseg, sseg, pseg)), rdup, rdup or sdup or pdup))
    return out


def main():
    files = glob.glob(os.path.join(ASSETS, "Scenes", "Chunks", "*", "Scene_Overworld_*.unity"))
    addressable = {os.path.basename(p)[:-7] for p in re.findall(r'\("([^"]+\.prefab)"', open(PREFABS, encoding="utf-8").read())}
    kinds, instances = collections.defaultdict(set), collections.defaultdict(list)
    with multiprocessing.Pool() as pool:
        for rows in pool.imap_unordered(scan, files, chunksize=16):
            for name, types, scene, path, rdup, dup in rows:
                kinds[name] |= types
                instances[name].append((rdup, bool(VARIANT.search(scene + "/" + path)), dup, scene, path))

    props = []
    for name, types in kinds.items():
        if "SpriteRenderer" not in types or name in addressable or types & {"Actor", "LazyActor"} or LOGIC_NAME.match(name):
            continue
        tier = "Decor" if types <= STATIC else "Animated" if types <= STATIC | ANIM else "Gameplay"
        rdup, _, dup, scene, path = min(instances[name])
        layer = re.match(r"Scene_Overworld_(.+)_-?\d+_-?\d+$", scene).group(1)
        props.append((tier + "/" + layer, name, scene, path, rdup, dup))
    props.sort()

    def cs(s):
        return '"' + s.replace("\\", "\\\\").replace('"', '\\"') + '"'

    lines = [
        "// <auto-generated> by Python/scenePropCatalog.py from the AssetRipper export of the chunk scenes. Do not edit. </auto-generated>",
        "namespace Drova_Modding_API.Systems.MapEditor",
        "{",
        "    internal static partial class MapScenePropCatalog",
        "    {",
        "        /// <summary>",
        "        /// Every prop kind baked into the overworld chunk scenes: name, category, one source scene and the prop's path in it.",
        "        /// </summary>",
        "        internal static readonly (string Name, string Category, string Scene, string Path)[] Props =",
        "        [",
    ]
    lines += ["            (%s, %s, %s, %s)," % (cs(n), cs(c), cs(s), cs(p)) for c, n, s, p, _, _ in props]
    lines += ["        ];", "    }", "}", ""]
    open(OUT, "w", encoding="utf-8", newline="\r\n").write("\n".join(lines))
    tiers = collections.Counter(c.split("/")[0] for c, *_ in props)
    print("%d scenes, %d props -> %s" % (len(files), len(props), os.path.normpath(OUT)))
    print("  " + ", ".join("%s %d" % t for t in sorted(tiers.items())))
    print("  %d with a duplicated root name, %d with any duplicated name on the path" % (sum(p[4] for p in props), sum(p[5] for p in props)))


if __name__ == "__main__":
    main()
