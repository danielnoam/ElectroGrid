# ElectroGrid — outstanding work

Everything on `claude/game-repo-overview-yas8ak` was written in a cloud session
with no Unity install. **Nothing here has been compiled or played.** It was
checked by static inspection only: usings cross-referenced against symbol usage,
renamed symbols grepped to zero residual hits, scene GUIDs and prefab YAML keys
confirmed by hand.

Work top to bottom. The order matters — the playtests depend on the wiring, and
the wiring depends on it compiling.

---

## 1. Open the project and let it compile — done

- [x] **Does it build at all?** Yes, on 6000.6.2f1 after the DNExtensions update (sections 4 and 5). Kept for reference: if something is
      broken, the likely candidates in order are: `Match3UIManager.Awake`, which
      now calls `AddComponent<Match3TutorialPresenter>()`; the
      `RuntimeInitializeOnLoadMethod` bootstraps on `SaveManager` and
      `FirebaseManager`; and `AudioManager`, which lost `ToggleAudio()` and
      `IsMuted` entirely. A UnityEvent wired to either of those in the inspector
      would not show up in a text search.
- [x] **`Prefab_ObjectMatchable` and `Prefab_ObjectPlus`** still show their held
      colour, held scale and swap SFX. Those fields moved up into the new
      `Match3SwappableObject` base class. Unity flattens the inheritance chain
      when serialising and the YAML keys did not change, so the values should
      carry, but confirm.
- [x] **Both scenes open with no missing-prefab warnings.** The
      `UnityAnalyticsManager` and `FirebaseManager` prefab instances were removed
      by editing scene YAML directly.
- [x] **`Match3GameManager`** shows the new `maxReshuffleAttempts` field at 5.

Expected, not a bug: `mutedSprite`, `unmutedSprite` and `muteButtonImage` are
gone from `TopBarUI` and `MainMenuScreen`, so those sprite assignments drop off.

---

## 2. Editor wiring — done

**The settings window and Continue button do nothing until this is done.** Both
needed scene and prefab work that could not be done without the editor.

### Settings window

- [x] Built as `Prefab_SettingsWindow`, a copy of the information window with
      `SettingsWindowUI` in place of the tutorial list: Music and SFX sliders,
      Haptics and Screen Shake checkboxes, and a two-tap Reset Progress button.
      Placed in both scenes and wired to `MainMenuScreen`, `TopBarUI` and
      `Match3UIManager`, which now initializes it.
- [x] Build a `SettingsWindowUI` prefab the same way `Prefab_InformationWindow`
      is built: `CanvasGroup` on the root, a window `RectTransform`, a title, a
      background `Image`, a back `Button`, plus two `Slider`s (music, sfx, both
      min 0 max 1) and two `Toggle`s (haptics, screen shake). Assign every field
      on the component.
- [x] Add a reset-progress `Button` and assign both it and its label text to
      `resetProgressButton` and `resetProgressLabel`. It confirms by being
      tapped twice rather than opening a second dialog, so the label has to be
      assigned or the confirm step is invisible.
- [x] Drop one instance in the main menu scene and one in the Match3 scene.
- [x] Assign it to `settingsWindowUI` on `MainMenuScreen` and on `TopBarUI`.

### Continue button

- [x] `Prefab_ButtonContinue` added at the top of `MiddleButtons` and assigned to
      `continueButton`. The grid is now 4 rows with 30 spacing (690 of 694px), and
      the button hides itself when the save has no last played level.

### Icons

- [x] The settings button (`Prefab_ButtonMute`) now shows `White Gear 1` in both scenes. The old mute button is now the settings button. The field was renamed with
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

## 4. Upgrade Unity — done, 6000.6.2f1

Upgraded out of order (before the section 3 playtest), with vFolders and
vHierarchy removed: 6.6 made the int instance-ID APIs they depend on a compile
error. It compiles headless with no errors.

- [x] **Firebase** — every `Assets/Firebase/Plugins/*.dll.meta` logs "PluginImporter
      object at version 1, below the supported minimum (2)". Select them in the
      editor and let Unity re-save them. Then do a device build: Firebase is still
      the most upgrade-sensitive dependency here.
- [x] **URP** — check that the emission shader work on matchable pieces still
      looks right.
- [x] Replay the section 3 checklist on 6.6.

---

## 5. Update DNExtensions — done, pinned to `9fc76a6`

Installed as the `utilities`, `components` and `systems` git packages, pinned in
`Packages/manifest.json`. `Assets/Plugins/DNExtensions` is deleted. It compiles
headless, and a scripted pass over every scene and prefab found no missing
scripts or prefabs. All 16 effects in the four VFX sequences deserialise, and
all 12 pools load.

The package kept its old script GUIDs, so audio events, `SelectableAnimator`,
`SelectableTextAnimator` (now `SelectableGraphicAnimator`), `InputManager`,
`VFXManager.prefab` and the effect sequences reconnected by themselves. What
needed real work:

- **Forked files with shared GUIDs.** The game's Grid system, MobileHaptics,
  `MenuManager.cs`, `OneShotSfx`, `OneShotParticle` and four shader graphs were
  forked from DNExtensions with their GUIDs intact. Unity kept the game's copies
  and silently dropped the package's, which broke the package's own editor code.
  The game's copies now have new GUIDs, with every reference rewritten.
- **Pooling.** The old config was `Resources/ObjectPooler.prefab`; it is now
  `Resources/ObjectPoolingSettings.asset` with the same 12 pools.
  `IPooledObject` became `IPoolable` with the same three hooks.
- **VFX.** The four game sequences were rewritten into the new format.
  `SetFullscreenScale` no longer exists in the package, so it was ported into the
  game (`Assets/2_Scripts/VFX`). `SetLensDistortionDynamic` maps onto
  `SetLensDistortion`; the line-break pulse is now two back-to-back tweens.
- **Button audio.** The new `SelectableAnimator` is animation only. The select and
  submit SFX and hover-to-select moved to a new game component,
  `SelectableFeedback`, on `Prefab_Button`. Scene overrides were retargeted.
  `SelectableHaptics` now listens on the `Selectable` directly, which also fixes
  select and deselect being wired to submit.
- **Input.** `InputReaderBase` lost its virtual `Start`, and `SubscribeToAction`
  no longer unsubscribes first. `TouchInputReader` calls `Start` again every second
  through `ResubscribeToActions`, so it now unsubscribes explicitly; otherwise
  Select handlers would pile up.

Playtest these, they are the parts the scripted check cannot see:

- [x] **Transitions look the same.** Main menu start, level start, level end and
      line break. Line break is the one rebuilt by hand.
- [x] **Button sounds and haptics.** Hover and select plays the select sound; tap
      plays submit and vibrates once, not twice.
- [x] **Tapping pieces** registers once per tap, including after the game has been
      running a while.
- [x] **Pooled objects** are cleared on scene changes. Every pool is now
      `dontDestroyOnLoad: 0`, so the pooler rebuilds them per scene. With it on,
      active tiles and pieces survived the return to the main menu.
- [x] **`com.danielnoam.helpfuleditor`** installed.

Follow-ups moved to **Backlog → Up next**: `PoolableAutoReturn` timing and the
AudioLibrary move, which also covers `SOAudioEvent.PlayAtPoint` no longer
pooling.

---

## 6. In a terminal: finish the Git LFS conversion — done

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

- [x] Run it. Committed locally as `d06837d` (632 files, ~276 MB). Do **not** use
      `git add --renormalize .` as written above: with `core.autocrlf=true` and
      `-text` on Unity YAML it also stages ~1,100 `.meta`/YAML files as CRLF-only
      churn. Renormalize only the paths whose `filter` attribute is `lfs`.
- [x] `git lfs ls-files | wc -l` is 670.
- [x] `git lfs fsck` no longer reports "should have been a pointer".
- [x] **Pushed.** 605 LFS objects, 292 MB, as-is (no OGG conversion first). LFS
      is now ~600 MB of GitHub's free 1 GB. All Firebase libraries are kept, by
      choice.

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

### Up next

- [x] **Level editor cleaned up.** No behaviour change intended; compiles, and
      validation over all 12 levels gives the same result. What changed:
      - The objective and lose condition drawers were two ~160-line copies; both
        are now thin subclasses of `ManagedReferenceTypeDrawer<T>`.
      - Objective requirement counting (was in three places) is
        `Match3LevelValidation.GetRequiredCounts`. Issue drawing and grid centring
        are shared by the window and inspector via `Match3LevelGridGUI`.
      - The window defers GUI-changing actions one way (`Defer`) instead of three
        (an enum, an Action and a pending-selection flag). `Validate All` validates
        each level once instead of twice. Clear buttons share `ResetCells`.
      - Layout: sidebar actions are paired into rows. In the level pane the paint
        grid now comes straight after the Grid Shape field, followed by the two
        randomiser foldouts ("Randomize Grid Shape", then "Randomize Tile
        Objects", renamed from "Randomize"), then validation.
      Still to check by hand in the editor: the three "check it" items under
      Features.
- [x] **Pooling timing fixed upstream** (DNExtensions `78e379c`, now pinned).
      `AudioLibrary` returns sources and fades in real time, so a paused game no
      longer starves its pool. `PoolableAudioSource` and `AudioTrack` fades are
      unscaled, `PoolableParticleSystem` follows the particle system's own time
      mode, and `PoolableAutoReturn` gained a `useUnscaledTime` option. It also had
      a second bug: it counted its own `lifeTime` down to 0, so every reuse after
      the first returned on the next frame. It now keeps the configured value.
      `PoolableDecal` and `PoolableVisualEffect` still use scaled time, which is
      right for world effects.
- [x] **`VFXManager` camera fixed upstream** (DNExtensions `f9aff38`, now pinned).
      It remembers the canvas's configured render mode in `Awake` and rebinds to
      `Camera.main` whenever the canvas has no camera, checked each `LateUpdate`,
      instead of only on `activeSceneChanged`, which can fire before the new scene's
      camera exists. The game's `CameraManager.BindVFXCanvas` workaround is removed.
      **Playtest:** the fullscreen fade still sits under the menu UI after going
      menu → level → menu → level.
- [x] **SFX moved to the AudioLibrary.** The 9 `SOAudioEvent` assets became
      `SOAudioProfile` assets in `Assets/3_Data/AudioLibrary`, mapped by the same
      names in one `SFX` category routed to the SFX mixer group. Every script now
      calls `AudioLibrary.Play(id)` with an `[AudioLibraryID]` string field.
      `SelectableFeedback` was kept, as it has the interactable guard and hover
      select that `SelectableAudioPlayer` lacks. The pool is 64 sources, 24 pre-warmed.
      Removed: the per-object and per-screen AudioSources nothing plays through any
      more, the stale `audioSource` scene overrides, `OneShotSfx.cs`, and the
      `OneShotSFX` pool and prefab, which nothing spawned from but which pre-warmed 50
      objects per scene.
      **Music stays on `AudioManager`.** The package's `AudioTrack` is for layered
      stems: it starts every track at load and keeps them all playing at volume 0,
      which would mean eight streaming decoders running at once on a phone, and it
      cannot pick a random track. `AudioManager` is now music and mixer volume only;
      the sliders still work because the library routes through the same SFX group.
      **Playtest:** button sounds (including many taps with the settings window
      open), piece spawn, swap and destroy, Plus destroy, screen switch, level
      win/fail, and a big cascade.
      `AudioTrackSettings.asset` is unused and disabled.

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
- [x] **Frame rate is no longer hardcoded to 120.** `GameManager.ApplyFrameRate`
      now defaults to 60 on mobile and reads `SettingsData.highFrameRate`. See the
      frame rate item in Features for the one editor step still open.
- [x] **Music clips moved to Streaming.** ~~All eight music tracks were Decompress
      On Load.~~ Done: `loadType` is now Streaming and `loadInBackground` is on for
      all eight, which fixes the resident-PCM and blocking-decompress problems.
      **Still open:** Vorbis quality is left at `1` (100%). Dropping it to 0.5-0.7
      would roughly halve the build size and is normally transparent for game
      music, but it is a perceptual change and was not made blind — A/B it and
      decide. Original finding:
- [ ] ~~**All eight music tracks are set to Decompress On Load.**~~ 197 MB of WAV,
      every one `loadType: 0`, Vorbis at quality `1` (100%), `loadInBackground: 0`,
      and no Android platform override. Three separate problems:

      - **Memory.** Decompress On Load expands the whole clip to PCM in RAM. The
        largest is 33.9 MB. `AudioManager` crossfades between two `AudioSource`s,
        so during a transition two tracks can be resident at once, tens of MB of
        RAM for music alone. `AndroidMinSdkVersion` is 23, so the floor includes
        old low-RAM hardware.
      - **A hitch at exactly the wrong moment.** `loadInBackground` is off, so
        decompression blocks. `AudioManager.OnLevelStarted` calls
        `Play(gameplayClips.GetRandomItem())` on every level start, which can mean
        synchronously decompressing a 20-30 MB track as the level opens.
      - **Build size.** Vorbis at 100% quality is far past transparent for game
        music; 0.5 to 0.7 is normal and roughly halves it.

      For music the settings want to be **Streaming** with **Load In Background**
      on. The 57 short SFX are correct as they are — Decompress On Load is right
      for short clips, that is what gives them zero-latency playback.

- [x] **Gyroscope null dereference fixed.** `UpdateGyroRotation` now checks
      `Gyroscope.current` before reading it, so it no longer throws every frame on
      Android hardware without the sensor. The unused gyro path in
      `TouchInputReader` has since been deleted; the camera tilt feature reads
      its own sensor. Original finding:
- [ ] ~~**The gyroscope is read every frame and used by nothing, and can throw.**~~
      `TouchInputReader.Update` calls `UpdateGyroRotation` every frame, which does
      `Gyroscope.current.angularVelocity.value` on mobile. `GyroRotation` is
      consumed nowhere in the project. Worse, the guard only checks for a
      touchscreen and a mobile device, not for the sensor existing — on an Android
      phone without a gyroscope `Gyroscope.current` is null and that line throws
      every frame. Plenty of budget Android hardware has no gyro. Either delete
      the gyro code or null-check `Gyroscope.current` before dereferencing it.

- [ ] **No signing keystore is configured.** `androidUseCustomKeystore: 0`, so
      builds are signed with the debug keystore, which the Play Console will not
      accept for an upload. Needed before any release, and the keystore must be
      backed up somewhere safe — losing it means never being able to update the
      listing again.

- [ ] **No crash reporting.** The Firebase plugins are Analytics, App, Platform,
      RemoteConfig and TaskExtension — Crashlytics is not installed. With no test
      coverage and a lot of recently changed code, a crash in the wild is
      currently invisible.

### Loose ends in code

- [x] **Background hover effect no longer runs every frame.** Done: both
      `BackgroundManager` and `Match3EffectManager` now bail out when the pointer
      has not moved, and skip the transform write when the scale is unchanged.
      `BackgroundManager` also gained the `mouseInteractionEffect` switch its
      sibling already had, defaulted to `true` so the menu looks the same as
      before. Original finding:
- [ ] ~~**`BackgroundManager.UpdateTiles` writes 288 transforms every frame.**~~ The
      main menu background grid is 16x18. Every frame it walks all 288 tiles,
      does a `Vector2.Distance` and a curve evaluation each, and writes
      `transform.localScale` unconditionally — even when the value has not
      changed, which still dirties the transform. At the 120fps the game asks for,
      that is around 35,000 transform writes a second for the least important
      visual in the game.

      It is also a *mouse hover* effect on a touch game: `MousePosition` only
      moves while a finger is down, so almost all of that work produces no visual
      change at all.

      `Match3EffectManager` has the identical code but guards it behind a
      `mouseInteractionEffect` bool. `BackgroundManager` has no such guard. At
      minimum give it the same switch; better, skip the write when the scale has
      not meaningfully changed, or only recompute when the pointer actually moved.

- [ ] **The menu background pulse creates ~576 tweens every 0.45s.**
      `BackgroundManager.PulseFromCenter` runs on a `pulseInterval` of 0.45 in the
      Main scene and calls `SquashTile` on all 288 tiles. Each of those builds a
      PrimeTween `Sequence` of two tweens, so the menu sustains roughly 1,300
      tween allocations a second while just sitting there. That is very likely why
      `GameManager` raises the capacity to 1,600.

      Unlike the hover effect this is a deliberate visual, so it was left alone.
      Options if it shows up in a profile: pulse a subset rather than the whole
      grid, drive it from one shared tween evaluated per tile, or lengthen the
      interval.

> **The pooling and audio systems are being replaced by the DNExtensions update
> (section 5).** The three items below live entirely inside code that is going
> away, so fixing them now is wasted work and would only make the merge harder.
> They are kept because the *behaviours* are worth checking for in whatever
> replaces them — particularly the time scale one, which is the kind of thing a
> general purpose pooling system also gets wrong.

- [ ] ~~**Pooled one-shots return on scaled time, and this game manipulates the
      global time scale a lot.**~~ *(superseded by the DNExtensions update)* `OneShotParticle.ReturnAfter` and
      `OneShotSfx.ReturnAfter` both `yield return new WaitForSeconds(...)`, which
      advances with `Time.timeScale`. Meanwhile
      `Match3EffectManager.OnLineBreakMade` drops the global scale to 0.3 for a
      line break, and the information and settings windows tween it to 0 while
      open.

      So a line break holds every particle and sound effect in flight 3.3x longer
      than intended, and opening a window mid-effect holds them for as long as the
      window stays open, because at a time scale of zero the coroutine simply does
      not advance. The pool then has to grow to cover instances that are not
      actually doing anything.

      `WaitForSecondsRealtime` is the right call here: returning an object to a
      pool is lifecycle management, not gameplay. Note the opposite is true of the
      15 `WaitForSeconds` in `Match3PlayHandler` and `Match3GameManager` — those
      drive board animation and *should* pause with the game. Do not change those.

- [ ] ~~**`DestroyAfter` is dead in both one-shot classes.**~~ *(superseded)* Declared in
      `OneShotParticle` and `OneShotSfx`, called by neither. It would also be
      actively wrong if it were called: destroying a pooled instance leaves the
      pooler holding a reference to a destroyed object. Delete both.

- [ ] ~~**`OneShotSfx.Play` throws on a null clip.**~~ *(superseded)* It guards `!audioSource` and
      then reads `audioSource.clip.length`, so passing a null clip null-refs on
      the line after the guard. Worth a look too: `OneShotParticle` computes its
      lifetime as `main.duration + main.startLifetime.constantMax`, which returns
      0 when `startLifetime` is set to a curve mode rather than a constant — that
      would recycle the particle while it is still emitting. Check what the
      particle prefabs actually use.

- [x] **`AllowOnlyOneObjectiveOfThisType` now does something.** It was declared
      and read by nothing; the level validator uses it to warn when a level has
      two objectives of a type that only allows one.
- [ ] **`ObstaclesBroken` and `BottomObjectsReached` are write-only.**
      Incremented in `Match3LevelData` and read by nothing. Firebase logs
      `matches_made`, `moves_made` and `time_spent_seconds` but not these, and
      the level complete window shows Pieces Cleared and Moves Made but not
      these. On levels built around Double Stars and Square Stars they are the
      numbers that describe how the level went. Surface them in both places or
      delete them.
- [x] **Autorotate flags tidied.** `PortraitUpsideDown`, `LandscapeRight` and
      `LandscapeLeft` are now `0`, matching the Portrait default. No behaviour
      change; a future switch to AutoRotation will not silently allow landscape.
- [x] **Quitting mid-level logs `level_quit`.** The bottom bar's quit button calls
      `Match3GameManager.LogLevelQuit`, which sends the level name,
      `matches_made`, `moves_made`, `time_spent_seconds` and
      `objective_progress_percent` (the average across objectives). It is skipped
      once the level has already been won or lost. Restart is not logged; it is
      also an abandonment if the funnel ever needs it.

### Features

- [x] **Camera tilt.** `CameraTilt` on the CameraManager prefab sways the camera
      by up to 4% of the orthographic size. The camera is orthographic, so a real
      rotation would barely show; the sway plus `ParallaxLayer` (the menu
      background follows 60% of it) is what gives depth. Input is the mouse
      position on desktop and the device's tilt on mobile: `GravitySensor`,
      falling back to `Accelerometer`, measured against a resting angle that
      drifts toward however the phone is held, so it reacts to a change of angle
      and then settles. It moves a runtime parent (`CameraTiltRig`), so it adds to
      the shake instead of fighting it. A **Tilt** toggle sits under Screen Shake
      in settings (default on; row spacing 60 → 50 to fit), and turning it off
      disables the sensor. In levels, `Match3EffectManager` parents the pooled
      background tiles under a runtime "Background Parallax Layer" (depth 0.6,
      set by `backgroundParallaxDepth`) and hands each back to its pool holder
      before returning it.
      **Playtest:** feel on a phone (strength, settle speed), the mouse on PC,
      shake during tilt, the toggle, and clicks still landing on the right piece.
      The original notes:

      - **`angularVelocity` is the wrong signal.** It is rotation *rate* in rad/s,
        so driving a tilt from it makes the camera react to the phone being
        *moved* rather than to how it is being *held*, which feels like drift
        rather than parallax. Use `AttitudeSensor` or `GravitySensor` for
        orientation, or integrate and heavily smooth the angular velocity.
      - **Sensors are disabled by default in the Input System.** Without
        `InputSystem.EnableDevice(...)` the values read zero, which is very likely
        why nothing was noticed when this was first written.
      - **It has to compose with `ShakeCamera`.** `CameraManager` tweens the
        camera for shake, so a tilt that writes rotation or position directly will
        fight it. Apply the tilt as an offset on a parent transform, or fold it
        into the same place shake is applied.
      - **Put a toggle next to screen shake.** Camera motion tied to device
        movement makes some people motion sick, and the settings window already
        has the right home for it. Enabling the sensor also costs battery, so the
        toggle should actually disable the device rather than just zero the
        effect.

- [ ] **Grid shape randomiser — check it.** Generates board silhouettes from
      smoothed noise with optional mirroring, drops cells that have no orthogonal
      neighbour (those can never form a match), and can keep only the largest
      connected region. Two buttons: in place, or into a new `SOGridShape` asset.
      No shape is currently shared between levels, so in place is safe today, but
      the panel counts how many levels use a shape and warns when that changes.
      Changing a shape clears tile objects left on cells that are now inactive —
      they would never spawn but would still be counted by
      `CountObjectsOfType` — and can strand Square Stars, which the validator
      immediately flags.

- [ ] **Level validation and randomisation — check them.** The editor now
      validates a level and can place tile objects at random. Two rules are worth
      confirming against a real board, because both produce levels that look fine
      in the editor: a Square Star on row 0 scores the instant the level starts
      (`CheckIfReachedBottom` runs from `SetCurrentTile`), and a Square Star in a
      column whose lowest *active* cell is above row 0 can never be collected,
      since gravity only moves objects to tiles that exist and reaching the bottom
      is tested as `y <= 0`. All twelve current levels pass, this was checked.
      The randomiser filters against the same rules, and a roll is a single undo
      step so rerolling is cheap.

- [ ] **Level editor window — check the reworked version.** The level editor is
      now `ElectroGrid > Level Editor` rather than the `SOMatch3Level` inspector.
      Worth confirming: the folder field accepts a dragged folder and remembers it
      between sessions (stored in `EditorPrefs` under
      `ElectroGrid.LevelEditor.Folder`, defaulting to `Assets/3_Data/Levels`), the
      sidebar lists and highlights levels, painting and the clear buttons behave
      as they did, and undo still works on a painted cell. The inspector is now
      read-only with an *Open in Level Editor* button; its grid preview
      deliberately refuses to resize stale tile data and tells you to open the
      window instead.

- [ ] **Combo and cascade feedback.** `HandleMatchesAndRepopulate` already loops
      cascades but nothing counts them, so a four-chain feels identical to a
      single match in a game that is otherwise very loud. Rising audio pitch per
      cascade step, a combo counter, escalating shake. The loop to hook into
      already exists; this is the biggest gap in feel.
- [x] **Frame rate modes in settings.** `SettingsData.highFrameRate` (default off,
      so 60) is read by `GameManager.ApplyFrameRate`. When it is on, the game runs
      at the display's refresh rate, capped at 120. `Prefab_SettingsWindow` has a
      new **High Frame Rate Row** under Screen Shake, a copy of that row, assigned to
      `highFrameRateToggle` and `highFrameRateRow`. The whole row hides on displays
      of 60Hz or less, and the change applies immediately. Playtest: check the row
      looks right in both scenes and the window still fits; on a 60Hz monitor in
      the editor the row will be hidden, which is expected.

      Original notes:

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

- **UI setup is reasonable.** One canvas per scene, and the Match3 scene's UI is
  small enough (around 25 components, 9 raycast targets) that canvas rebuild cost
  is not worth chasing. 15 layout groups and 2 content size fitters across both
  scenes and all prefabs is unremarkable.
- **Texture import settings are in good shape.** 437 of 442 PNGs carry an
  Android platform override, and no source texture is above 200 KB. Nothing to do
  here.
- **The Unity splash screen is already disabled** (`m_ShowUnitySplashScreen: 0`).
- The Firebase API key in `google-services-desktop.json` is a client config and
  is safe to commit. It only matters if Firestore or Storage is added, at which
  point security rules are what protect you.
- `Assets/GeneratedLocalRepo` is tracked on purpose. EDM4U only regenerates it
  when the Android Resolver runs, so untracking it would break a fresh clone's
  Firebase build until someone resolves.
- The two `.exe` files under `Assets/Firebase/Editor` are Firebase's own editor
  tooling, not stray build artifacts. Do not delete them.
