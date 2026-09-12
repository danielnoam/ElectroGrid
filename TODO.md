# ElectroGrid — outstanding work

Everything on `claude/game-repo-overview-yas8ak` was written in a cloud session
with no Unity install. **Nothing here has been compiled or played.** It was
checked by static inspection only: usings cross-referenced against symbol usage,
renamed symbols grepped to zero residual hits, scene GUIDs and prefab YAML keys
confirmed by hand.

Work top to bottom. The order matters — the playtests depend on the wiring, and
the wiring depends on it compiling.

---

## 1. Open the project and let it compile

- [ ] **Does it build at all?** Nine commits, none verified. If something is
      broken, the likely candidates in order are: `Match3UIManager.Awake`, which
      now calls `AddComponent<Match3TutorialPresenter>()`; the
      `RuntimeInitializeOnLoadMethod` bootstraps on `SaveManager` and
      `FirebaseManager`; and `AudioManager`, which lost `ToggleAudio()` and
      `IsMuted` entirely. A UnityEvent wired to either of those in the inspector
      would not show up in a text search.
- [ ] **`Prefab_ObjectMatchable` and `Prefab_ObjectPlus`** still show their held
      colour, held scale and swap SFX. Those fields moved up into the new
      `Match3SwappableObject` base class. Unity flattens the inheritance chain
      when serialising and the YAML keys did not change, so the values should
      carry, but confirm.
- [ ] **Both scenes open with no missing-prefab warnings.** The
      `UnityAnalyticsManager` and `FirebaseManager` prefab instances were removed
      by editing scene YAML directly.
- [ ] **`Match3GameManager`** shows the new `maxReshuffleAttempts` field at 5.

Expected, not a bug: `mutedSprite`, `unmutedSprite` and `muteButtonImage` are
gone from `TopBarUI` and `MainMenuScreen`, so those sprite assignments drop off.

---

## 2. Editor wiring

**The settings window and Continue button do nothing until this is done.** Both
needed scene and prefab work that could not be done without the editor.

### Settings window

- [ ] Build a `SettingsWindowUI` prefab the same way `Prefab_InformationWindow`
      is built: `CanvasGroup` on the root, a window `RectTransform`, a title, a
      background `Image`, a back `Button`, plus two `Slider`s (music, sfx, both
      min 0 max 1) and two `Toggle`s (haptics, screen shake). Assign every field
      on the component.
- [ ] Add a reset-progress `Button` and assign both it and its label text to
      `resetProgressButton` and `resetProgressLabel`. It confirms by being
      tapped twice rather than opening a second dialog, so the label has to be
      assigned or the confirm step is invisible.
- [ ] Drop one instance in the main menu scene and one in the Match3 scene.
- [ ] Assign it to `settingsWindowUI` on `MainMenuScreen` and on `TopBarUI`.

### Continue button

- [ ] Add a button to the main menu and assign it to `continueButton` on
      `MainMenuScreen`. It hides itself when the save has no last played level,
      so it will not appear until a level has been started once.

### Icons

- [ ] The old mute button is now the settings button. The field was renamed with
      `[FormerlySerializedAs("muteButton")]`, so the scene reference and the top
      bar's layout animation carry over untouched — only the icon needs swapping
      to something settings shaped, in both scenes.

---

## 3. Playtest

- [ ] **Reshuffle.** The riskiest change on the branch. Force it by temporarily
      raising `minPossibleMatches` in the Match3 scene so the no-moves check
      trips on an ordinary board. The board should redeal and hand back control;
      it must never lock up.
- [ ] **Tutorials.** Each card shows once, the first time its mechanic appears:
      `BasicMatching` on the first level played, `DoubleStars` and `SquareStars`
      when a level contains those tiles, `Plus` when a helper spawns, `LineBreak`
      after the first line break. The info button should still list all five.
- [ ] **Save and unlock.** `save.json` appears in `Application.persistentDataPath`
      after finishing a level. Locked buttons show `lockedLevelLabel` and are not
      clickable. The last level shows **Finish**, not a dead **Next Level**.
- [ ] **Settings.** Sliders move volume smoothly across their whole travel, both
      toggles take effect, and all four values survive a restart.
- [ ] **Reset progress.** First tap arms it, second within 3s wipes progress, and
      it disarms itself on timeout or on closing the window. Levels relock, the
      Continue button disappears, tutorials re-arm, and the volume and toggle
      settings are deliberately kept.
- [ ] **Best stats.** After completing a level, selecting it in level select
      shows Best moves, time and pieces cleared.
- [ ] **Continue.** Appears only after a level has been started, and loads
      straight into the right level.

`SaveManager` has a **Delete Save** context-menu item to reset between tests. It
re-arms the tutorials too.

---

## 4. In a terminal: finish the Git LFS conversion

`.gitattributes` was broken until this branch — every pattern was missing its
glob (`.png` instead of `*.png`), so it only matched a file literally named
`.png`. Two full-path Firebase entries had no glob to omit and did work, which is
why `git lfs ls-files` shows exactly two files today.

The patterns are fixed, so anything touched from here on goes to LFS
automatically. What is outstanding is converting the binaries already tracked as
plain blobs. `git lfs fsck` reports them as
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

Expect **642 files, ~297 MB**: png, mp3, wav, dll, ttf, psd and the native
libraries. No text assets are affected — verified that no `.cs`, `.unity`,
`.asset`, `.prefab` or `.meta` is caught by the patterns.

- [ ] Run it.
- [ ] `git lfs ls-files | wc -l` should be far more than 2.
- [ ] `git lfs fsck` should stop reporting "should have been a pointer".

**Quota:** LFS holds 319 MB today, which is just the two Firebase libraries
(124 MB + 194 MB). This adds ~297 MB, so roughly 616 MB against GitHub's free
allowance of about 1 GB storage and 1 GB/month bandwidth. Bandwidth is the one to
watch, it is spent on every clone and CI run.

**This does not shrink the repo.** Historical blobs stay reachable from earlier
commits, so `.git` remains ~450 MB. It only stops the tree and `.gitattributes`
from disagreeing, so a binary changed from now on is stored once in LFS instead
of as a new blob per edit.

---

## Reference

### The optional full history rewrite

Only this actually shrinks the repository, and it was deliberately declined:

```bash
git lfs migrate import --everything
```

It rewrites all commits, so every SHA on `main` changes, both branches need a
force push, and every clone must be recreated. Doing it on a side branch does not
help — the migrated branch shares no commits with `main`, cannot be merged back,
and the old blobs stay reachable as long as `main` points at them. It would also
push ~450 MB more into LFS, which likely needs a paid data pack.

What is in the history:

| type | size | note |
|---|---|---|
| `*.wav` | 447 MB | 197 MB of it is live, referenced music |
| `*.asset` | 174 MB | Unity YAML, correctly not LFS material |
| `*.a` | 70 MB | Firebase iOS (tvOS has since been removed) |
| `*.unity` | 38 MB | scenes, text, correctly not LFS |
| `*.o` | 22 MB | historical only, no longer in HEAD |

### Save file shape

`Application.persistentDataPath/save.json`, written atomically via a temp file
and rename, because an app kill part way through a direct write truncates it and
the OS can kill a mobile app at any point. A missing or unparseable file falls
back to defaults rather than throwing during startup. Records are keyed by asset
name, not index, so reordering levels never scrambles them.

```csharp
public class SaveData
{
    public int version = 1;
    public SettingsData settings;
    public string lastPlayedLevel;
    public int highestLevelUnlocked;      // level index, 0 means only the first
    public List<LevelRecord> levels;
    public List<string> seenTutorials;    // by asset name
}

public class SettingsData
{
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
    public bool hapticsEnabled = true;
    public bool screenShakeEnabled = true;
}

public class LevelRecord
{
    public string levelName;
    public bool completed;
    public int bestMoves;
    public float bestTime;
    public int bestPiecesCleared;
}
```

A level unlocks by completing the previous one. No stars, so `SOMatch3Level`
needed no new fields and no level asset was retuned.

---

## Known gaps, not yet done

- **No combo or cascade feedback.** `HandleMatchesAndRepopulate` already loops
  cascades but nothing counts them, so a four-chain feels identical to a single
  match in a game that is otherwise very loud.
- **Twelve levels is roughly 25 minutes of play.** The level painter and 16 grid
  shapes are already there, several shapes unused by any level. This is content
  work, not engineering, and it is the real limit on retention.
- **No tests**, despite `com.unity.test-framework` being installed. Match
  detection, objective progress and `SaveManager` are all testable without a
  scene, and the reshuffle has no safety net.

### Notes on things that are fine

- The Firebase API key in `google-services-desktop.json` is a client config and
  is safe to commit. It only matters if Firestore or Storage is added, at which
  point security rules are what protect you.
- `Assets/GeneratedLocalRepo` is tracked on purpose. EDM4U only regenerates it
  when the Android Resolver runs, so untracking it would break a fresh clone's
  Firebase build until someone resolves.
- The two `.exe` files under `Assets/Firebase/Editor` are Firebase's own editor
  tooling, not stray build artifacts. Do not delete them.
