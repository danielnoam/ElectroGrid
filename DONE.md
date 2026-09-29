# ElectroGrid — done

Finished work moved out of [TODO.md](TODO.md), oldest first. The notes are
kept because they explain why things are the way they are.

## Compile, wiring, upgrade, DNExtensions, LFS

### 1. Open the project and let it compile — done

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

### 2. Editor wiring — done

**The settings window and Continue button do nothing until this is done.** Both
needed scene and prefab work that could not be done without the editor.

#### Settings window

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

#### Continue button

- [x] `Prefab_ButtonContinue` added at the top of `MiddleButtons` and assigned to
      `continueButton`. The grid is now 4 rows with 30 spacing (690 of 694px), and
      the button hides itself when the save has no last played level.

#### Icons

- [x] The settings button (`Prefab_ButtonMute`) now shows `White Gear 1` in both scenes. The old mute button is now the settings button. The field was renamed with
      `[FormerlySerializedAs("muteButton")]`, so the scene reference and the top
      bar's layout animation carry over untouched — only the icon needs swapping
      to something settings shaped, in both scenes.

### 4. Upgrade Unity — done, 6000.6.2f1

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

### 5. Update DNExtensions — done, pinned to `9fc76a6`

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

### 6. In a terminal: finish the Git LFS conversion — done

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

## Up next (from the old backlog)

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
      - Sidebar (reworked again): one drag-to-reorder list that is the play
        order (number, validation dot, name), right-click for Ping, Duplicate,
        Remove From Play Order and Delete (to trash, confirmed). Levels in the
        folder but not in the play order sit under "Not In Game" with Add and
        Add All. Toolbar: New, Duplicate, Validate All. Refresh is automatic on
        project changes, and the folder field sits at the bottom as "New Levels
        Folder".
      - Layout: in the level pane the paint
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

## Ships broken on modern phones

- [x] **Safe area handled.** `SafeAreaMargin` keeps edge-anchored UI clear of
      cutouts and the home indicator, but only moves an element when the unsafe
      area reaches it: distance from the edge = max(designed margin, inset + 16
      − content clearance). Unity's built-in uGUI `SafeArea` would have added the
      full inset on top of the existing margins, dropping the Match3 top bar
      about 150 units on an iPhone for nothing. It is on the Match3 top bar, the
      menu's bottom corner buttons (clearance 105), and the Credits and Level
      Select back bars. `BottomBarUI` resolves its shown position through the
      same method, since it animates its own position. The windows were checked
      and already clear the home indicator.
      **Playtest in the Device Simulator:** an iPhone with a Dynamic Island, a
      notched or punch-hole Android, and a phone without a cutout (nothing
      should move).
- [x] **Android target API pinned to 36** (Android 16) by `AndroidSdkPin`, an
      `[InitializeOnLoad]` editor script that reapplies it on every script
      reload, so switching back to Automatic does not stick. Change
      `AndroidSdkPin.TargetSdk` to move it. The installed SDK has 34, 36 and 37;
      bump it when Google Play's August deadline moves. Min SDK is 26 (Android 8),
      raised from 23 by Unity 6.6 when it upgraded the project settings.
- [x] **`Assets/link.xml` keeps `Assembly-CSharp` from IL2CPP stripping**, so the
      `[SerializeReference]` objectives, lose conditions and grid converters, which
      only ever come into being through the deserialiser, survive any stripping
      level. It preserves the whole assembly rather than a list, so a new
      objective type cannot be forgotten. The project sets no stripping level, so
      Unity's default applies; this keeps game code safe whatever it is raised to.
      **Still confirm on device:** the first release APK loads a level with its
      objectives and lose conditions showing.
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
- ~~**All eight music tracks are set to Decompress On Load.**~~ 197 MB of WAV,
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
- ~~**The gyroscope is read every frame and used by nothing, and can throw.**~~
      `TouchInputReader.Update` calls `UpdateGyroRotation` every frame, which does
      `Gyroscope.current.angularVelocity.value` on mobile. `GyroRotation` is
      consumed nowhere in the project. Worse, the guard only checks for a
      touchscreen and a mobile device, not for the sensor existing — on an Android
      phone without a gyroscope `Gyroscope.current` is null and that line throws
      every frame. Plenty of budget Android hardware has no gyro. Either delete
      the gyro code or null-check `Gyroscope.current` before dereferencing it.

- [x] **Signing keystore set up.** `electrogrid.keystore` (alias `electrogrid`)
      was made with Unity's Keystore Manager and lives in the Google Drive build
      folder, so it is backed up. The build window's `AndroidSigning` applies it for
      the build only; `androidUseCustomKeystore: 0` in ProjectSettings is on
      purpose. The v1.0.0 draft APK was signed with it. Original finding:
- ~~**No signing keystore is configured.**~~ `androidUseCustomKeystore: 0`, so
      builds are signed with the debug keystore, which the Play Console will not
      accept for an upload. Needed before any release, and the keystore must be
      backed up somewhere safe — losing it means never being able to update the
      listing again.

## Loose ends in code

- [x] **Background hover effect no longer runs every frame.** Done: both
      `BackgroundManager` and `Match3EffectManager` now bail out when the pointer
      has not moved, and skip the transform write when the scale is unchanged.
      `BackgroundManager` also gained the `mouseInteractionEffect` switch its
      sibling already had, defaulted to `true` so the menu looks the same as
      before. Original finding:
- ~~**`BackgroundManager.UpdateTiles` writes 288 transforms every frame.**~~ The
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

- [x] **Menu background pulse is one tween per pulse.** It used to build a
      two-tween sequence on each of the 288 tiles every 0.45s (~1,300 tweens a
      second). Now `BackgroundManager` caches each tile's ring delay once, and
      a single `Tween.Custom` per pulse calls `Match3BackgroundTile.EvaluateSquash`
      with the same curve (linear down over 30%, `OutSine` back up over 70%).
      Tiles at rest are skipped. The look is meant to be identical, so compare it
      by eye. `GameManager` still raises the tween capacity to 1,600, since the
      Match3 line break still squashes background tiles one tween each; lower it
      only after profiling.

> **The pooling and audio systems are being replaced by the DNExtensions update
> (section 5).** The three items below live entirely inside code that is going
> away, so fixing them now is wasted work and would only make the merge harder.
> They are kept because the *behaviours* are worth checking for in whatever
> replaces them — particularly the time scale one, which is the kind of thing a
> general purpose pooling system also gets wrong.
>
> **Update:** only `OneShotSfx` was actually replaced. See TODO.md > Later >
> `OneShotParticle` for what is still live.

- ~~**Pooled one-shots return on scaled time, and this game manipulates the
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

- ~~**`DestroyAfter` is dead in both one-shot classes.**~~ *(superseded)* Declared in
      `OneShotParticle` and `OneShotSfx`, called by neither. It would also be
      actively wrong if it were called: destroying a pooled instance leaves the
      pooler holding a reference to a destroyed object. Delete both.

- ~~**`OneShotSfx.Play` throws on a null clip.**~~ *(superseded)* It guards `!audioSource` and
      then reads `audioSource.clip.length`, so passing a null clip null-refs on
      the line after the guard. Worth a look too: `OneShotParticle` computes its
      lifetime as `main.duration + main.startLifetime.constantMax`, which returns
      0 when `startLifetime` is set to a curve mode rather than a constant — that
      would recycle the particle while it is still emitting. Check what the
      particle prefabs actually use.

- [x] **`AllowOnlyOneObjectiveOfThisType` now does something.** It was declared
      and read by nothing; the level validator uses it to warn when a level has
      two objectives of a type that only allows one.
- [x] ~~**`ObstaclesBroken` and `BottomObjectsReached` are write-only.**~~
      *Won't do: not a problem as they are.*
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

## Features

- [x] **High Frame Rate row missing on 120Hz phones, fixed.** The check read the
      display's *current* refresh rate, and phones drop to 60Hz while the game
      asks for 60, so the row hid itself. It now uses the highest rate in
      `Screen.resolutions`. **Confirm on the phone** that the row appears and
      the game runs smoother with it on.

- [x] **Build window (`ElectroGrid > Build`).** Builds the enabled targets one
      after another from Unity 6 Build Profiles: Android APK and Windows, zipped.
      Output goes to `Builds/<version>/`, which git ignores. The active platform
      builds first and is restored after. Version bump buttons; the Android
      version code is derived from the version (1.2.3 → 10203). Checks before
      building: level validation, and for GitHub `gh`
      installed and signed in, the tag not already released, and HEAD pushed.
      Uploads: GitHub Release through `gh` (draft by default, notes from commit
      subjects since the last tag) and Copy To Folder. **Replace Existing Release**
      re-uploads a version that already exists, draft or published: it replaces
      the files, moves the tag to the current commit, and keeps its draft or
      published state. Players already on that version are not offered it.
      Machine-specific paths (build folder, copy folder, keystore path and alias)
      live in EditorPrefs via `BuildMachineSettings` and `AndroidSigning`, never in
      git; the project only holds the default build folder `Builds`. Create the
      keystore with Unity's Keystore Manager and point the build window at it. The same pipeline runs
      headless with `-executeMethod ElectroGridBuild.BuildFromCommandLine`
      (add `-noUpload` to only build). Config: `Assets/Settings/Build/BuildConfig.asset`.
      **First use:** install `gh` (`winget install --id GitHub.cli`), run
      `gh auth login`, then click the two Create Profile buttons in the window.
      **Has run end to end:** the v1.0.0 draft on GitHub has both the APK and
      the Windows zip, and a copy sits in the Google Drive build folder.
      Android signing: release APKs must be signed with the release keystore at
      the path set in the window (default `%USERPROFILE%\.android-keystores\electrogrid.keystore`,
      alias `electrogrid`), outside the repo and backed up. The password is held
      for the session, saved DPAPI-encrypted for this Windows user when Remember
      is on, or read from `ELECTROGRID_KEYSTORE_PASSWORD` for headless builds.
      It is applied for the build only, then ProjectSettings is restored. The build folder can be any
      folder (Browse). First release is v1.0.0 (Android code 10000).
      Later: AAB for Google Play, WebGL (needs Firebase compiled out),
      itch.io through butler, Firebase App Distribution.

- [x] **In-game updater.** `GameUpdater` (bootstrapped like `SaveManager`)
      checks `api.github.com/repos/danielnoam/ElectroGrid/releases/latest` once per
      launch. It needs the repo public, which it is. It compares the tag with
      `Application.version` and picks this platform's asset (`.apk` or
      `-Windows.zip`, as the build window names them). Drafts and prereleases are
      never offered. It downloads to `persistentDataPath/Updates` and checks the
      size and GitHub's `sha256:` asset digest; old downloads are cleared on
      launch. A failed check is silent.
      `UpdateInstaller`: Android hands the APK to the system installer through
      `FileProvider` (`ElectroGridUpdater.androidlib` adds the provider and
      `REQUEST_INSTALL_PACKAGES`), opening "install unknown apps" the first time.
      Windows extracts the zip, and a generated `apply-update.cmd` waits for the
      game to exit, robocopies over the install folder and relaunches. A
      read-only install folder (Program Files) falls back to the release page.
      `Prefab_UpdateWindow` (copied from the settings window: same animation,
      pauses the game) sits on the main menu. It offers the update 1.5s after
      the menu opens, once per launch, with release notes, progress and
      Later / Update → Install or Restart, and Open Page as the fallback.
      **Playtest:** publish a release newer than an installed build and run the
      whole flow on a phone (including the permission prompt) and on Windows.
      **Before any Google Play build:** remove the androidlib, since Play
      forbids `REQUEST_INSTALL_PACKAGES` for self-updating.
      Later: pull the animation shared by the settings, information and update
      windows into one base class.

- [x] **Camera tilt.** `CameraManager` sways the camera
      by up to 4% of the orthographic size. The camera is orthographic, so a real
      rotation would barely show; the sway plus `ParallaxLayer` (the menu
      background follows 60% of it) is what gives depth. Input is the mouse
      position on desktop and the device's tilt on mobile: `GravitySensor`,
      falling back to `Accelerometer`, measured against a resting angle that
      drifts toward however the phone is held, so it reacts to a change of angle
      and then settles. The shake now shakes offsets (`Tween.ShakeCustom`) rather
      than the transform, and `CameraManager` writes resting position + tilt +
      shake each LateUpdate, so the two add up with no extra objects. A **Tilt** toggle sits under Screen Shake
      in settings (default on; row spacing 60 → 50 to fit), and turning it off
      disables the sensor. In levels there is deliberately no parallax: the
      board cells and background tiles form one continuous grid, so they sway
      together with the camera. A background layer sliding separately pulled the
      grid apart and was removed.
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

- [x] **Continue shows New Game for a first-time player.** When the save has no
      last played level, `MainMenuScreen` relabels the Continue button **New
      Game** and points it at the first level, instead of hiding it. Once a level
      has been started it reads Continue again, and Reset Progress turns it back
      into New Game. The label is the button's TMP child, set from code, so no
      scene change was needed.

- [x] **Automatic tutorials have their own look.** The card that pops up the
      first time a mechanic appears still uses the information window, but
      through `InformationWindowUI.ShowIntroduction`: the title reads **New!**,
      only that card is shown, and the back button reads **Got it**. The info
      button's list restores the original title and Back label. Both strings are
      fields on the component (`introductionTitle`, `introductionButtonLabel`),
      as is its lighter backdrop (`introductionBackgroundAlpha`, 0.5 against the
      list's 0.94). The window shrinks to fit its cards, never past its authored
      1380, and the button keeps the same gap below it, so one card gets a small
      window and the full list looks as before. The list scrolls back to the top
      when it is filled.
- [x] **Tapping the backdrop closes the information and settings windows,** the
      same as the back button (settings still saves and disarms Reset Progress).
      `PointerClickRelay` is added to the Background image at runtime, so no
      prefab changed. Only taps outside the window, its title and its button
      close it: parts of both windows are not raycast targets, so taps inside
      fell through to the backdrop until that check was added. The update window does not do this.

- [x] **Quit and restart are icon buttons.** Two new variants built like the
      settings button (`Prefab_ButtonMute`, text removed, icon on top), from the
      same Violet Theme UI pack: `Prefab_ButtonHome` (White Home 2) for quit and
      `Prefab_ButtonRestart` (White Return 2) for restart. In the Match3 scene they
      replace the 240x120 text buttons at the same anchors, sized 100x100 like the
      settings and info buttons, and `BottomBarUI` points at them. The swap was
      scripted headless; check the bottom bar in the editor. Restart still has no
      confirm (see the UI rework item in TODO.md).

- [x] **All in-level buttons sit on the bottom bar.** Quit and restart on the
      left, info and settings on the right. The bar's `Buttons` row now splits
      into a `Left` and a `Right` group (each a HorizontalLayoutGroup, spacing
      30, aligned to its edge), so it holds up at any width. Info and settings
      moved from the top bar along with their click handlers (`TopBarUI` →
      `BottomBarUI`), and the top bar lost the pop-out animation it gave them;
      they now slide in and out with the bottom bar. Done by a headless script;
      check the layout in the editor.

- [x] **Main menu info and settings match the level HUD.** Both sit bottom right
      like in the Match3 bottom bar: settings outermost (45 from the right),
      info to its left (175), both 60 up and 100x100. Settings used to be bottom
      left. The `BottomButtons` SafeAreaMargin clearance went 105 → 25 so the pair
      moves off a notch or home indicator the same way the level's bottom bar does.

- [x] **Credits is a window, opened from a Tag icon in the menu's bottom left.**
      `CreditsWindowUI` is the settings window's show/hide and backdrop-close
      without the pause (nothing to pause on the menu). The scene's
      `CreditsWindow` is an unpacked copy of `Prefab_SettingsWindow` holding the
      old credits content (the two `Prefab_CreditGroup` columns, now 606 tall so
      they clear the byline, and "A Game By Daniel Noam"). `Prefab_ButtonCredits`
      (White Tag) sits at 45/60 from the bottom left, mirroring settings. The
      Credits text button left the middle list (now 3 rows), and `CreditsScreen`
      plus `MenuManager.ShowCredits` are deleted. Tidied after: both columns stack from the top so
      the headers line up (24 extra below each header), each entry is the name
      with its author smaller and dimmer underneath (size 34, wraps at 380),
      vFolders and vHierarchy are gone, and a flexible spacer keeps the byline
      at the bottom of the window. The icon is a placeholder
      until a better one turns up.
- [x] **Prefabs sorted into folders.** `4_Prefabs/Managers`, `UI/Buttons`,
      `UI/Windows`, `UI/Elements`, `Grid` and `Particles`, moved through the
      AssetDatabase so GUIDs and every reference held (both scenes and the pool
      settings checked). Renamed on the way: `Prefab_ButtonMute` →
      `Prefab_ButtonSettings`, `Prefab_ObjectSquareStart` →
      `Prefab_ObjectSquareStar`.

- [x] **All windows are variants of one base.** `Prefab_WindowBase` (in
      `UI/Windows`) holds what every window shares: the backdrop, the window
      frame and title, and the buttons row with a Back button. Information,
      Settings, Update and the new `Prefab_CreditsWindow` are variants that add
      their own middle (scroll list, or panel and content), buttons and script;
      Update removes the base's Back for its Later/Update pair. Built and swapped
      in the open editor: scene instances were replaced by hierarchy, so their
      overrides and all 1,556 references into them held, and the credits window
      in Main is now a prefab instance. The buttons row sits 50 higher (-800),
      set once in the base.
