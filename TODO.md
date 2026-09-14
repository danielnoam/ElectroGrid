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

## 4. Upgrade Unity

**Only after section 3 passes.** An editor upgrade triggers its own recompile;
doing it on top of commits that have never been compiled means untangling two
unrelated sources of breakage at once. Get to a known-good state, commit it,
then upgrade.

Currently on **6000.2.9f1**.

- [ ] Upgrade to the latest Unity 6 release.
- [ ] Back up or branch first. A Unity upgrade rewrites asset metadata and is not
      cleanly reversible.
- [ ] Watch these, they are the ones most likely to complain:
      - **Firebase** — the SDK is 12.6.0 with native libraries per platform and is
        the single most upgrade-sensitive dependency here. Check the Firebase
        Unity release notes support the editor version *before* upgrading.
      - **URP 17.2.0** — a major URP bump can change the look of the emission
        shader work on matchable pieces.
      - **PrimeTween** — installed from a local tarball
        (`Assets/Plugins/PrimeTween/internal/`), so it will not update itself with
        the registry packages. Everything in the game animates through it.
      - Input System 1.14.2, Cinemachine 3.1.5, Timeline 1.8.9.
- [ ] Recompile, reopen both scenes, replay the section 3 checklist.

---

## 5. Update DNExtensions

Do this after the Unity upgrade has settled, for the same reason — one
toolchain change at a time.

- [ ] Install the latest DNExtensions.

Nothing in this project references a specific DNExtensions version, so there is
nothing recorded here to diff against. The game code uses `SceneField`,
`ChanceList`, `RangedFloat`, `SOAudioEvent`, `ObjectPooler`, `MenuScreen`,
`SelectableAnimator`, `InputReaderBase`, `VFXManager` and the attribute set
(`ReadOnly`, `Separator`, `Button`, `MinMaxRange`, `Preview`), so those are the
surfaces to check after updating.

---

## 6. In a terminal: finish the Git LFS conversion

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

## Backlog

### Ships broken on modern phones — do these first

- [ ] **No safe-area handling.** Nothing in the project references
      `Screen.safeArea`. This is a portrait game with a top bar and a bottom bar,
      so on any notched or punch-hole Android phone, or an iPhone with a Dynamic
      Island, both bars sit partly under the cutout and the home indicator. The
      fix is a small component that insets a `RectTransform` by
      `Screen.safeArea`, applied to the top and bottom bar containers in both
      scenes.
- [ ] **`AndroidTargetSdkVersion` is 0**, meaning "Automatic (highest
      installed)". The API level a build targets then depends on whichever SDK
      happens to be installed on the machine doing the build, so it can change
      silently between machines or after an SDK update. Google Play also enforces
      a minimum target API for new uploads. Pin it to a specific level.
- [ ] **Verify a device build still loads levels, before trusting IL2CPP
      stripping.** Android builds with IL2CPP, there is no `link.xml` and no
      `[Preserve]` anywhere, and the `[SerializeReference]` subclasses
      (`GetMatches`, `MoveLimit`, `TimeLimit`, `DestroyObstaclesObjective`,
      `ReachBottomObjective`, `GetSpecificItemMatches`) are never constructed in
      runtime code — they only ever come into being through Unity's deserialiser.
      Types reachable only through serialised data are the classic thing managed
      stripping removes. If it happens, every level loads with null objectives and
      conditions and becomes unwinnable, and it will only show up on device, never
      in the editor. Cheap insurance is a `link.xml` preserving the assembly, or
      `[Preserve]` on those six classes.
- [ ] **Frame rate is hardcoded to 120 on Android** (`GameManager.cs:47`, inside
      the `RuntimePlatform.Android` check). Superseded by the settings item in
      Features below — the value should come from the player's choice rather than
      being baked in. For a match-3, 120 is a lot of battery and heat for very
      little benefit, and thermal throttling makes it inconsistent anyway.
- [ ] **No crash reporting.** The Firebase plugins are Analytics, App, Platform,
      RemoteConfig and TaskExtension — Crashlytics is not installed. With no test
      coverage and a lot of recently changed code, a crash in the wild is
      currently invisible.

### Loose ends in code

- [ ] **`AllowOnlyOneObjectiveOfThisType` is dead.** Declared abstract on
      `Match3Objective`, overridden `=> true` by all four subclasses, and read by
      nothing. Nothing stops a level being authored with two `GetMatches`
      objectives, which would show two competing "Pieces:" rows in the top bar.
      Either enforce it in `SOMatch3LevelEditor`, which already draws the
      objectives list, or delete the property.
- [ ] **`ObstaclesBroken` and `BottomObjectsReached` are write-only.**
      Incremented in `Match3LevelData` and read by nothing. Firebase logs
      `matches_made`, `moves_made` and `time_spent_seconds` but not these, and
      the level complete window shows Pieces Cleared and Moves Made but not
      these. On levels built around Double Stars and Square Stars they are the
      numbers that describe how the level went. Surface them in both places or
      delete them.
- [ ] **Tidy the inert autorotate flags.** `allowedAutorotateToPortrait`,
      `PortraitUpsideDown`, `LandscapeRight` and `LandscapeLeft` are all `1` while
      `defaultScreenOrientation` is `0` (Portrait), so they do nothing. Setting the
      three non-portrait ones to `0` changes no behaviour but stops the settings
      contradicting each other, and means a future switch to AutoRotation does not
      silently allow landscape. Cosmetic, not urgent.
- [ ] **Quitting mid-level fires no analytics event.** The bottom bar's quit
      button returns to the menu silently, so abandonment does not appear in the
      funnel — only starts, completions and failures do. A `level_quit` event
      carrying the level name and progress so far would close that.

### Features

- [ ] **Combo and cascade feedback.** `HandleMatchesAndRepopulate` already loops
      cascades but nothing counts them, so a four-chain feels identical to a
      single match in a game that is otherwise very loud. Rising audio pitch per
      cascade step, a combo counter, escalating shake. The loop to hook into
      already exists; this is the biggest gap in feel.
- [ ] **Frame rate modes in settings, 60 and 120.** `GameManager.Awake` currently
      forces `Application.targetFrameRate = 120` on Android. That becomes a stored
      preference instead: add a field to `SettingsData`, a control to
      `SettingsWindowUI` alongside the existing sliders and toggles, and have
      `GameManager` read the saved value rather than hardcoding it.

      Two things worth getting right:

      - **Hide or disable 120 on a display that cannot do it.** Most phones are
        still 60Hz, and `Application.targetFrameRate` above the panel's refresh
        rate does nothing. `Screen.currentResolution.refreshRateRatio` gives the
        real ceiling, so the option should only be offered when it means
        something. Offering a setting that visibly does nothing is worse than not
        offering it.
      - **Default to 60, not 120.** Battery life on a puzzle game people play in
        long sessions matters more than frame rate, and a device that thermally
        throttles delivers an inconsistent 120 which feels worse than a steady 60.
        Let players opt into 120.

      A third mode worth considering is a "match display" option that just uses the
      panel's refresh rate, which avoids the whole question on high refresh
      hardware.

- [ ] **Localisation.** Not started. Scope, so it can be costed honestly:

      **Volume is small.** Roughly 25 player-facing strings in code, plus 5
      tutorial cards (title and body), 12 level names and 4 item labels in
      ScriptableObjects, plus whatever sits in scene and prefab TMP components
      (menu buttons, window titles). Translation cost is genuinely low.

      **The work is in the plumbing, not the words.** The strings are currently
      built by string interpolation inside `Match3Objective` and
      `Match3LoseCondition` — `$"Collect {requiredAmount} Pieces"`,
      `$"Destroy {requiredAmount} Double Stars"`. Those become table lookups with
      arguments. Watch for word order: languages do not agree that the number
      comes first, so the translator needs the placeholder, not a concatenation.

      **Suggested route:** the official `com.unity.localization` package, which
      handles TMP, ScriptableObject fields and a locale selector. Add it during
      or after the Unity upgrade (section 4) rather than before, so the package
      resolves against the final editor version.

      **Font coverage is the trap.** TMP renders from a pre-baked atlas. Latin
      languages with accents (Spanish, Portuguese, German, French) may already
      be covered; Cyrillic (Russian), Greek, or any CJK will need the atlas
      rebuilt with those ranges or they render as blank boxes. Check the font
      asset before promising a language. Leave right-to-left (Arabic, Hebrew)
      out of a first pass, it needs TMP's RTL handling and mirrored layouts.

      **A reasonable first set** is English as the base plus Spanish,
      Portuguese (Brazil), German and French — all Latin script, all large
      mobile markets, no atlas surprises.

      **One thing to decide:** level names are currently authored strings
      ("Level 1"). If they stay numeric they need no translation at all, which
      is the cheaper answer.

- [ ] **No tests**, despite `com.unity.test-framework` being installed. Match
      detection, objective progress and `SaveManager` are all testable without a
      scene, and the reshuffle has no safety net.

### Content, not engineering

- **Twelve levels is roughly 25 minutes of play.** The level painter and 16 grid
  shapes are already there, several shapes unused by any level. Note also that
  `GetSpecificItemMatches` now works (an ordering bug meant it could never score)
  and no level uses it yet.

### Notes on things that are fine

- **The game is already locked to portrait.** `defaultScreenOrientation: 0` is
  `UIOrientation.Portrait`. The four `allowedAutorotateTo*` flags are all `1` but
  they are inert, they only apply when the default orientation is `4`
  (AutoRotation). Setting them to `0` would make the settings read consistently
  but would change nothing.

- The Firebase API key in `google-services-desktop.json` is a client config and
  is safe to commit. It only matters if Firestore or Storage is added, at which
  point security rules are what protect you.
- `Assets/GeneratedLocalRepo` is tracked on purpose. EDM4U only regenerates it
  when the Android Resolver runs, so untracking it would break a fresh clone's
  Firebase build until someone resolves.
- The two `.exe` files under `Assets/Firebase/Editor` are Firebase's own editor
  tooling, not stray build artifacts. Do not delete them.
