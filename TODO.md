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
- **Playtest the reshuffle.** It is the change with the most sequencing risk.
  Easiest way to force it is to temporarily raise `minPossibleMatches` in the
  Match3 scene so the no-moves check trips on an ordinary board.

---

## 3. Known remaining issues

### No persistence (largest remaining gap)

There is no `PlayerPrefs` usage anywhere in the project. No level unlocking, no
stars or best scores, no record of the furthest level reached, and the mute
setting does not survive a restart. Every level is always selectable, and
`SetNextLevel` wraps modulo so level 12 returns to level 1 with nothing recorded.

For a mobile match-3 this is where retention lives, and it is the one substantial
item left.

### Smaller items

- `Match3LevelData` deep-copies objectives and lose conditions by round-tripping
  them through `JsonUtility`. It works, but it relies on Unity object references
  surviving as instance IDs within a session and is fragile.
- `GetSpecificItemMatches.GetName()` dereferences `targetItem.Label` with no null
  check, unlike `GetRequirementText()` two methods below. It would throw in the
  level-select info panel if the field were left unassigned. No shipped level
  uses that objective, so it is currently unreachable.
- `Match3EffectManager.Awake` returns without destroying a duplicate instance,
  unlike the other singletons in the project.
- No tests, despite `com.unity.test-framework` being installed.

### Notes on things that are fine

- The Firebase API key in `google-services-desktop.json` is a client config and
  is safe to commit. It only becomes a concern if Firestore or Storage is added,
  at which point security rules are what matter.
- `Assets/GeneratedLocalRepo` is left tracked on purpose. EDM4U only regenerates
  it when the Android Resolver runs, so untracking it would break a fresh clone's
  Firebase build until someone resolves.
- The two `.exe` files under `Assets/Firebase/Editor` are Firebase's own editor
  tooling, not stray build artifacts. Do not delete them.
