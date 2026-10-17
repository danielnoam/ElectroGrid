# ElectroGrid — outstanding work

Open work, in the order to do it: finish the game, polish and compliance,
performance, the Android release, then Windows and Steam last. Finished items
are in [DONE.md](DONE.md).
Last full check against the code: 2026-09-29.

---

## 1. Finish the game

Feel fixes first, then the last gameplay additions, then the level curve once
the mechanics stop changing (retuning 100 levels twice is the waste to avoid).
Surge is in. Decide whether Spark is in for launch before the level curve
rework, since it changes how every level plays.

- [ ] **Combo bar.** `HandleMatchesAndRepopulate` already loops cascades but
      nothing counts them, so a four-chain feels identical to a single match in
      a game that is otherwise very loud. The idea: every match fills a combo bar
      on the right side of the screen. The combo resets if the next match doesn't
      come in time, and the window gets shorter as the combo grows. A full bar
      triggers an ability (in Survival: extra time). The feel pass comes along
      with it: pitch rising per step, a counter, shake getting stronger.
      **Groundwork is in:** `Match3Combo` (plain C#, 9 EditMode tests) is owned
      by `Match3GameManager` as `Combo`. A swap that matches is a step (count +1,
      fill +0.2), each cascade wave it sets off adds fill +0.05 but no step, and
      the countdown pauses while the board resolves, restarting from a full
      window once the player can move. The window starts at 4s and shrinks by
      0.9 per step down to 1.5s. Events: `StepAdded`, `FillChanged`, `Filled`
      (the bar empties, the combo keeps going) and `Reset`. All numbers live in
      the Combo section of `Match3GameManager`'s inspector, which also shows the
      live count, fill and time left.
      **Bar, feel and ability are in too** (`ComboBarUI` under the top bar):
      the counter shows from x2 on the left of the level title, the bar and a
      draining timer line sit on its right (mirrored for Hebrew and Arabic).
      Each step plays ButtonSelect pitched up 0.08 per step and shakes a little
      harder; a full bar plays PlusDestroy, shakes, and adds 5s to a time limit
      or 2 moves to a move limit (`Match3LevelData.OnComboFilled`). All of it
      is tunable in the inspector. Still to do: real sounds for the step and
      the full bar (numbers playtested 2026-10-11). In Survival a full bar adds
      uncapped time (`SurvivalMode`).

- [ ] **Plus: rainbow look, and maybe a wildcard rule.**
      - **Rainbow effect** on the Plus so it's clear it can be moved: it cycles
        through the colours of the matchable pieces in play (the level's piece
        colours, not a fixed rainbow).
      - **Idea to decide: Plus as a wildcard.** Today a Plus is cleared by a match
        next to it (or a line break). Instead it would count as any piece: swap it
        into a line and it completes the match for whatever colour is there, and it
        has to be used that way. Changes the Info card text in all 7 languages, the
        bot, and level balance (levels that relied on Plus clearing by adjacency),
        so decide before the level curve rework.

- [ ] **New-mechanic visuals, what is left.** Shaders for glitches (`Glitch.shader`),
      fuse rings (`FuseRing.shader`) and conveyor belts (`ConveyorBelt.shader`),
      a real virus sprite with a glow mask, spread and glitch-clear particles are
      in. Still to do: sounds for a glitch clearing, a virus spreading and a fuse
      blowing, and for the win sweep and the lose grey-out.

- [ ] **Survival mode.** First version is in: a separate Survival button on the
      main menu, unlocked from the start. It opens its own screen, laid out like level
      select: rules, best score and Play on top, your 10 best runs (score,
      time, date; `SaveData.survivalRuns`) below. One
      run is a 45s clock on a fixed
      board (`Assets/3_Data/Survival/Survival_Board`, Grid_FullSquare). Every
      number lives on `Assets/3_Data/Survival/SurvivalMode`: swap the board
      there, and tune score, time gains and Star spawns. Score is 10 per piece
      (matches and line breaks), +20 per piece beyond 3 in one match, 100 per
      Plus, 500 per Plug, 300 per Star, and all of it is
      multiplied by the combo count (capped at x10). Time comes from a Plus
      (+5s), a full combo bar (+5s), Plugs (+3s) and Stars (+2s),
      uncapped, and every gain shrinks with minutes played (curve, 1 at the
      start to 0.4 at 5 minutes). Square and Stars spawn at random on
      refills, max 2 each. Best score and runs played are saved; `survival_end`
      goes to Firebase with score and duration. Code: `Match3SurvivalRun`
      (scoring, clock, spawns, 11 EditMode tests), `SurvivalScoreObjective` and
      `SurvivalTimeLimit` (the top bar shows them like any objective).
      Unlocks are now score **Milestones** on `SurvivalMode`: each entry is a
      score and what it unlocks (a piece, viruses, fuses, Plugs or Double
      Stars). Set now: Stars at 0, Diamond 25,000, viruses 40,000, Trapezoid
      60,000, fuses 75,000 (a blown fuse ends the run). Thresholds need a
      playtest on the phone. Numbers playtested 2026-10-11. The milestone
      banner is in (see DONE.md). Still to do: a Steam leaderboard later
      (section 5). The best score already shows
      on the main menu Survival button.

- [ ] **Spark power-up.** An L or T match leaves a Spark that clears a 3x3
      area when matched or swapped. Surge is done (see DONE.md); the Drone
      (2x2) was dropped because Plus already fills the helper role.

- [ ] **Surge, what is left.** A lightning arc from the Surge to each piece it
      zaps, a glow on its core, its own sounds (made, zapping, chain), and a
      combo when two Surges are swapped with each other (today only one goes
      off unless they hold the same piece). Add a tutorial level for it in the curve
      rework.

- [ ] **Rework the level curve, after the 100 levels.** Difficulty is all
      over the place. Re-order and retune all 100 into a steady ramp: rate
      each level (limit against goal, mechanics in play, board size and shape),
      then sort by that. Simpler grid shapes go at the start.
      **How a new obstacle comes in** (the pattern most match-3 games use:
      introduce, isolate, reinforce, combine):
      1. **Before it:** 3 to 4 levels with only known obstacles and different
         objectives, so the player is comfortable.
      2. **Introduce:** 1 or 2 easy levels with just the new obstacle (for
         example only Stars), with its tutorial card.
      3. **Reinforce:** 2 or 3 levels mixing it with the objectives the player
         knows.
      4. **Combine:** then mix it with earlier obstacles, and repeat the cycle
         for the next one. That is a new obstacle about every 8 to 10 levels.
      Keep a saw-tooth inside the ramp: an easier breather level after a hard
      one, and a harder "boss" level at the end of each cycle. Playtest on the
      phone and update LEVELS.md to match.

### Content, not engineering

- **100 levels are in** (see LEVELS.md). `GetSpecificItemMatches` is left
  unused on purpose: it played badly, so levels use Get Matches instead.

---

## 2. Polish and compliance

Everything a store page and a paying audience can hold against the game.

- [ ] **New game icon.** Replace the current app icon (Android adaptive icon,
      Windows exe icon, and later the Steam and itch.io capsules) with one that
      fits the neon ElectroGrid look and reads at small sizes.

- [ ] **Find new fonts for both the UI text and the titles.** Share Tech Mono
      replaced DS-Digital for licence reasons, but it isn't the look we want:
      find a better free (or bought) font for the UI text. Abduction2002 (logo,
      window titles, settings labels) has no commercial licence, so it needs a
      replacement too, one that keeps the logo's striped neon feel. Options tried
      so far are in [IDEAS.md](IDEAS.md); every candidate must cover the accents
      of Spanish, Portuguese, German and French (see [LICENSES.md](LICENSES.md)).
      Also move the unused TextMesh Pro example fonts out of their `Resources`
      folder so they stop shipping, and confirm the sprites and shaders are own
      work.

---

## 3. Performance pass

- [ ] **Music Vorbis quality is still 100%.** Needs listening, so later. The
      loading problems are fixed (see DONE.md > Music clips moved to Streaming).
      Dropping quality to 0.5-0.7 would roughly halve the build size and is
      normally transparent for game music, but A/B it by ear before changing it.

- [ ] **Performance, what is left** (measured in the editor on 2026-10-16, see
      DONE.md for what was done). A level draws about 300 sprites: 128 background
      grid tiles, 70 board tiles and 71 pieces. With the SRP Batcher that is only
      about 30 SetPass calls, so the draw count is cheap, and only a phone Profiler
      run can say whether it matters. In order:
      - **Profile on a phone** (a low-end Android): frame time in a busy level, a
        line break and the win sweep, and GC allocations per frame.
      - **The background grid** is the biggest group (128 sprites in a level, more
        on the menu). If the phone shows draw cost, draw it as one tiled sprite or
        a shader, keeping its pulse effects.
      - **Sprite atlases**: no gain in the editor. A UI atlas was tried and moved
        the draw count by 1 to 3, so it was dropped. Pieces can't be packed as they
        are: their emission masks are separate textures that line up with the
        sprite's own UVs, and packing moves those UVs. Packing them needs the masks
        turned into sprite secondary textures first.
      - **Property blocks** are used on fuse rings, glitch overlays and conveyors.
        In URP they take a renderer out of the SRP Batcher, which is fine for the
        handful there are, but don't add them to pieces or tiles.

---

## 4. Android release

The releases repo comes first so every installed copy keeps updating before
anything else changes.

- [ ] **Separate public repo for releases, so this repo can go private.**
      Makes sense, but the order matters or every installed copy stops updating:
      1. Create the public repo (e.g. `danielnoam/ElectroGrid-Releases`), with
         just a README and maybe a changelog.
      2. Change `ReleaseRepository.Name` to it. The updater, its Open Page
         fallback and every `gh release` call in the build window already read
         that one value and pass it as `--repo` (see DONE.md).
      3. Make the build window's upload work across repos. The tag no longer lives in the repo where the commit is,
         so the "tag already released" check, the "HEAD pushed" check and the
         release notes from commit subjects need to handle that. **Replace
         Existing Release** moving the tag to the current commit no longer works
         the same way, because the commit doesn't exist in the releases repo.
         Tag the source repo yourself, and in the releases repo point the tag at
         its own default branch.
      4. **Ship one release to both repos** built with the new URL, and wait
         until players have actually updated.
      5. Only then make this repo private. Installed builds older than step 4
         still point here and will quietly stop seeing updates, since a failed
         check is silent. That's acceptable for a small player base, but it's
         permanent for those installs.
      Also check: the README links and anything else that points players at
      this repo's releases page (the updater's Open Page fallback).

- [ ] **Google Play release checklist.** Research and check before the first
      Play upload; confirm each rule against the current Play Console docs:
      - **Developer account.** One-time fee and identity verification. New
        personal accounts have had to run a closed test with a minimum number
        of testers for 14 days before production access; check the current
        numbers.
      - **Build.** AAB, not APK, signed with Play App Signing (back up the
        upload keystore). Target API level at the current Play minimum,
        64-bit (ARM64) only, and 16 KB memory page support for native
        libraries (Unity and Firebase versions must support it). Bump the
        version code every upload. Upload IL2CPP symbols so Crashlytics and
        Play vitals show readable stacks.
      - **Turn off the GitHub updater in Play builds.** Play policy forbids
        apps updating themselves outside Play. Same scripting define idea as
        the Steam build.
      - **Policy forms.** Data safety form (Firebase Analytics and Crashlytics
        collect data), a privacy policy URL, the content rating questionnaire,
        target audience (stay out of the Families program unless aimed at
        children) and the "contains ads" answer. Check whether EU players need
        a consent prompt for Analytics.
      - **Store listing.** 512x512 icon, 1024x500 feature graphic, phone
        screenshots, short and full description, localized for every language
        the game has.
      - **Device testing.** A low-end phone, a tall phone and a tablet; notches
        and safe areas; the system back button and back gesture (back or pause,
        never a silent quit); pausing and resuming mid-level (calls,
        notifications, app switching) keeps the save and audio right; first
        launch with no network.

---

## 5. Windows and Steam release

Last. The Steam build turns the updater off, and the leaderboard needs
Survival. The in-game level editor sits here too: it is big, and Workshop is
what makes it worth building.

- [ ] **Windows builds still report no crashes to a dashboard.** Crashlytics for
      Unity only reports from Android and iOS. Exceptions now go to `crash.log`
      next to the save (see DONE.md), but that only helps when a player sends
      it in. Sentry's Unity SDK (free tier, catches native crashes on desktop
      too) is the pick, deferred for now. Do it before Steam, where PC is the
      only platform. Limit it to Windows so phones don't report twice.

- [ ] **Free-to-play Steam release — what it takes.** Research, not started.
      Known requirements and the costs people tend to miss:
      - **Steam Direct fee: $100 per app**, paid even for a free game, and
        (as far as known) only paid back after $1,000 gross revenue, which a free
        game with no purchases never reaches. So budget it as a cost. Tax and
        bank info in Steamworks are required even for a free app.
      - **Timeline.** The store page has to be public as "Coming Soon" for at
        least two weeks before launch, and both the store page and the build go
        through Valve review (a few business days each). There has also been a
        waiting period between paying the fee and being able to release; check
        the current rule in the Steamworks docs.
      - **Store assets.** Several capsule images in exact sizes, at least 5
        screenshots, and ideally a trailer. It's real work.
      - **Portrait on desktop is the big design question.** The game is locked
        portrait at 540x960 with a fixed-size window (`resizableWindow: 0`).
        On a 16:9 monitor that's a narrow strip, and on Steam Deck (1280x800,
        landscape only) it would be a small column in the middle. Options:
        letterbox portrait with decorative art on the sides, or a landscape
        layout for the HUD. Decide this before the store screenshots, since they
        show it. Decide too whether to go for Steam Deck compatibility (controller
        input on a touch-only UI is work too).
      - **Turn off the updater in Steam builds.** Steam handles updates, and
        `apply-update.cmd` robocopying over the Steam install folder would
        leave files Steam doesn't know about. Use a scripting define for a Steam
        build profile.
      - **Steamworks integration.** Steamworks.NET or Facepunch.Steamworks
        for achievements, leaderboards and overlay. `steam_appid.txt` for local
        testing, SteamPipe (`steamcmd` + depot scripts) for uploads. The build
        window could gain a Steam upload step the same way it has GitHub.
      - **Privacy.** Firebase Analytics on PC collects data from players, so
        the store page should link a privacy policy.
      - **Free-to-play on Steam usually means some monetisation.** If there's
        truly nothing to buy, it's just a "Free" game. Fine, but say so, since
        it changes nothing about the $100 and it affects whether trading cards
        or DLC ever make sense.

- [ ] **PC and large-screen support.** The game is built for a portrait phone
      (540x960, fixed-size window). Rework it so it plays well on a PC monitor
      and other large screens:
      - **Display settings** in the Settings window: windowed, fullscreen and
        borderless; resolution (window size); v-sync; and a frame rate cap.
        Save them with the rest of the settings and apply them on start.
      - **Layout.** Let the window resize and keep the board and HUD scaled
        and centred at any aspect ratio, with something filling the sides on
        wide screens instead of black bars. Ties into the portrait-on-desktop
        question in the Steam item below.
      - **Input.** Make mouse play feel right (hover states, no touch-only
        gestures) and add basic keyboard shortcuts (Esc for back or pause).

- [ ] **Make the build tool generic, with itch.io and Steam uploads.** The
      ElectroGrid build window (`Assets/2_Scripts/Build/Editor`) should work in
      any Unity project, not just this one:
      - **Generic.** Take out the ElectroGrid-specific names, paths and repo, so
        it can be its own package (it could live with the DNExtensions packages)
        and every project reads its own config.
      - **More upload targets.** Next to GitHub releases, upload to itch.io
        with `butler push` (one channel per platform) and to Steam with
        SteamPipe (`steamcmd` + depot scripts), each turned on per build.
      - **Settings tab.** Move the per-machine and rarely changed settings
        (output and copy folders, keystore path and passwords, upload logins
        and tokens) out of the main view into a Settings tab, so the main view
        is just what changes per build.

- [ ] **Steam leaderboard for Survival.** Depends on Survival (score must exist)
      and on the Steam integration above. `FindOrCreateLeaderboard` + upload
      the score with `KeepBest`, show top 10 and the player's own rank next to
      them on the Survival results screen. Things to know:
      - **Android players are left out.** Steam leaderboards only work in the
        Steam build. If mobile players should compete too, that's Google Play
        Games or a backend (Firebase is already there), and then the two sources
        should share one board, which is a bigger job. Decide whether a
        Steam-only board is enough.
      - **Cheating.** The score is computed on the client, so anyone can upload
        any number. Steam has no server check for that. For a small free game
        that's usually accepted; at least clamp obviously impossible scores and
        upload the run duration as extra detail so outliers can be spotted.
      - **Scoring changes invalidate the board.** Once there's a leaderboard,
        rebalancing the score resets fairness. Use a new board name per scoring
        version (`survival_v1`).

- [ ] **In-game level editor, with Steam Workshop later.** Let players build
      levels inside the game: pick or paint a grid shape, place Stars
      and Plugs, choose objectives and limits, then play-test and save.
      The editor-only tools already do most of this (`Match3LevelEditorWindow`,
      `Match3LevelValidation`), so the work is a runtime UI for it and a level
      format that isn't a ScriptableObject (JSON with the grid cells, tile
      objects, objectives and limits). Run the same validation rules as
      LEVELS.md before a level can be saved or shared. Workshop comes after
      the Steam build: upload and subscribe through Steamworks UGC, load
      subscribed levels from their install folders, and keep player levels
      out of the main level list and progress.

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

### Notes on things that are fine

- **The game is already locked to portrait.** `defaultScreenOrientation: 0` is
  `UIOrientation.Portrait`. Only `allowedAutorotateToPortrait` is `1` now
  (see DONE.md); the flags only apply when the default
  orientation is `4` (AutoRotation) anyway.

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
