# Outstanding work

Notes from the cleanup pass on `claude/game-repo-overview-yas8ak`.

---

## 1. Finish the Git LFS conversion

**Status: prepared, but must be run locally.**

`.gitattributes` was broken until this branch — every pattern was missing its
glob (`.png` instead of `*.png`), so it only ever matched a file literally named
`.png`. Two full-path Firebase entries had no glob to omit and did work, which is
why `git lfs ls-files` shows exactly two files today.

The patterns are fixed now, so anything touched from here on goes to LFS
automatically. What is still outstanding is converting the binaries already
tracked as plain blobs. `git lfs fsck` reports these as
`should have been a pointer but was not`.

This could not be done from the cloud session: `lfs.github.com` is refused by
that environment's egress policy, and pushing pointer files whose objects never
uploaded would leave every texture, track and native library as an unresolvable
134-byte stub on clone.

```bash
git pull origin claude/game-repo-overview-yas8ak
git add --renormalize .
git commit -m "Convert tracked binaries to LFS pointers"
git push
```

Expect **642 files, ~297 MB**: png, mp3, wav, dll, ttf, psd, and the native
libraries. No text assets are affected — verified that no `.cs`, `.unity`,
`.asset`, `.prefab` or `.meta` is caught by the patterns.

Verify afterwards:

```bash
git lfs ls-files | wc -l   # should be far more than 2
git lfs fsck               # "should have been a pointer" warnings should be gone
```

**Quota:** LFS currently holds 319 MB — that is just the two Firebase libraries
(124 MB + 194 MB). This adds ~297 MB, so roughly 616 MB against GitHub's free
allowance of about 1 GB storage and 1 GB/month bandwidth. Bandwidth is the one to
watch, since it is spent on every clone and CI run.

**This does not shrink the repo.** The historical blobs stay reachable from
earlier commits, so `.git` remains ~450 MB. It only stops the working tree and
`.gitattributes` from disagreeing, so a binary changed from now on is stored once
in LFS instead of as a new blob per edit.

### Optional: the full history rewrite

Only this actually shrinks the repository, and it was deliberately declined:

```bash
git lfs migrate import --everything
```

It rewrites all commits, so every SHA on `main` changes and both branches need a
force push, and every clone must be recreated. Doing it on a side branch does not
help — the migrated branch shares no commits with `main`, cannot be merged back,
and the old blobs stay reachable as long as `main` points at them. Note it would
also push ~450 MB more into LFS, which likely needs a paid data pack.

What is actually in the history, for reference:

| type | size | note |
|---|---|---|
| `*.wav` | 447 MB | 197 MB of it is live, referenced music |
| `*.asset` | 174 MB | Unity YAML, correctly not LFS material |
| `*.a` | 70 MB | Firebase iOS (tvOS has since been removed) |
| `*.unity` | 38 MB | scenes, text, correctly not LFS |
| `*.o` | 22 MB | historical only, no longer in HEAD |

---

## 2. Verify this branch in the editor

**Nothing on this branch has been compiled.** It was written in a cloud session
with no Unity install, so it was checked by static inspection only: usings
cross-referenced against symbol usage, renamed symbols grepped to zero residual
hits, scene GUIDs and prefab YAML keys confirmed by hand.

Worth checking specifically:

- The project compiles at all.
- `Prefab_ObjectMatchable` and `Prefab_ObjectPlus` still show their held colour,
  held scale and swap SFX. Those fields moved into the new
  `Match3SwappableObject` base class. Unity flattens the inheritance chain when
  serialising and the prefab YAML keys are unchanged, so the values should carry,
  but it is worth a look.
- `Main.unity` and `Match3.unity` open with no missing-prefab warnings — the
  `UnityAnalyticsManager` instances were removed from both by editing the scene
  YAML directly.
- `Match3GameManager` shows the new `maxReshuffleAttempts` field at 5.
- `FirebaseManager` and `SaveManager` now create themselves via
  `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`, so neither needs a prefab.
  The `FirebaseManager` prefab and its instances in both scenes were removed.
  Confirm Firebase still initialises and Remote Config still applies.
- **Playtest the reshuffle.** It is the change with the most sequencing risk.
  Easiest way to force it is to temporarily raise `minPossibleMatches` in the
  Match3 scene so the no-moves check trips on an ordinary board.

---

## 3. Known remaining issues

### Persistence — built, needs playtesting

`SaveManager` is implemented and wired up. Unlock rule is completing the previous
level; no stars. Still unverified in the editor, see section 2.

Worth checking: the save file appears at `Application.persistentDataPath/save.json`
after finishing a level, locked buttons show the `lockedLevelLabel` and are not
clickable, mute survives a restart, and the last level shows **Finish** rather
than a dead **Next Level** button. `SaveManager` has a *Delete Save* context-menu
item for resetting between tests.

#### Implemented design: a JSON `SaveManager`

JSON over `PlayerPrefs`. `PlayerPrefs` is fine for a couple of scalars, but this
needs a record per level, and that degenerates into building key names by hand
(`"level_3_stars"`). A single JSON file is inspectable while developing, carries a
version field so the schema can migrate later, and writes in one go.

```csharp
[Serializable]
public class SaveData
{
    public int version = 1;
    public bool muted;
    public int highestLevelUnlocked;   // level index, 0 means only the first is unlocked
    public List<LevelRecord> levels = new List<LevelRecord>();
}

[Serializable]
public class LevelRecord
{
    public string levelName;
    public bool completed;
    public int bestMoves;
    public float bestTime;
    public int bestPiecesCleared;
}
```

`SaveManager`, a singleton alongside `GameManager` with `DontDestroyOnLoad`:

- File at `Application.persistentDataPath + "/save.json"`.
- `JsonUtility.ToJson` / `FromJson`, already used elsewhere in the project, so no
  new dependency.
- **Write atomically** — write `save.json.tmp`, then `File.Replace` onto the real
  file. A process kill mid-write otherwise truncates the save, and on mobile the
  OS can kill the app at any point.
- Load in `Awake` inside a `try`/`catch`, falling back to a fresh `SaveData` if
  the file is missing or unparseable. A corrupt save must never block startup.
- Write on level complete and on mute toggle only, never per frame.
- Key `LevelRecord` by `levelName`, not by array index, so reordering or
  inserting levels does not scramble existing records.

Wiring:

- `LevelSelectionScreen` — render levels above `highestLevelUnlocked` as locked
  instead of every level always being selectable.
- `Match3GameManager` — on `LevelComplete`, write the record and unlock the next.
- `SetNextLevel` — stop at the last level instead of wrapping modulo to level 1,
  and show something for finishing the game.
- `AudioManager` — read and write `muted` so it survives a restart.

Decided: a level unlocks by completing the previous one. No stars, so
`SOMatch3Level` needed no new fields and no level asset was retuned.

Notes:

- `JsonUtility` cannot serialise a `Dictionary`, hence `List<LevelRecord>`.
- `persistentDataPath` on Android is app-private and cleared on uninstall, which
  is fine here. There is no cloud sync; Firebase is already in the project if
  that is ever wanted.

### Smaller items

- No tests, despite `com.unity.test-framework` being installed.

Fixed already: the `JsonUtility` round-trip in `Match3LevelData` (now a
`Clone()` on the objective and condition base classes, which also clears the
copy's event subscriptions), the unguarded `targetItem.Label` derefs in
`GetSpecificItemMatches`, and `Match3EffectManager.Awake` not destroying a
duplicate.

### Notes on things that are fine

- The Firebase API key in `google-services-desktop.json` is a client config and
  is safe to commit. It only becomes a concern if Firestore or Storage is added,
  at which point security rules are what matter.
- `Assets/GeneratedLocalRepo` is left tracked on purpose. EDM4U only regenerates
  it when the Android Resolver runs, so untracking it would break a fresh clone's
  Firebase build until someone resolves.
- The two `.exe` files under `Assets/Firebase/Editor` are Firebase's own editor
  tooling, not stray build artifacts. Do not delete them.
