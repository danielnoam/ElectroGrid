# ElectroGrid — outstanding work

Open work, in the order to do it. Finished items are in [DONE.md](DONE.md).
Last full check against the code: 2026-09-29.

---

## 1. Before a wider release

Crashlytics and the first tests are in (see DONE.md). What is left:

- [ ] **Windows builds still report no crashes to a dashboard.** Crashlytics for
      Unity only reports from Android and iOS. Exceptions now go to `crash.log`
      next to the save (see DONE.md), but that only helps when a player sends
      it in. Sentry's Unity SDK (free tier, catches native crashes on desktop
      too) is the pick, deferred for now. Do it before Steam, where PC is the
      only platform. Limit it to Windows so phones don't report twice.

- [ ] **No PlayMode tests.** The EditMode tests cover the logic. Still untested:
      the coroutine flow in `Match3GameManager`, meaning a swap, the cascade
      loop and the reshuffle running end to end. That needs a PlayMode test that
      loads the Match3 scene.

---

## 2. Features, in build order

The UI rework comes first because the combo bar and Survival both add HUD
elements. Survival depends on the combo bar (its time ability) and on score,
and the level select rework is where the Survival entry would go (it is on the main menu for now).

- [ ] **In-game UI rework.** Quit and restart are icons now (see DONE.md). Next,
      look at other match-3 games for ideas. Worth studying: how Royal Match,
      Candy Crush and Two Dots keep the goal and moves/time readable at a glance
      at the top, what they push into a pause menu instead of keeping on screen,
      and how they confirm a restart (a restart tapped by accident costs the
      whole level, so it needs a confirm or should live in the pause menu). Do
      this before the combo bar and Survival, since both add HUD elements and
      the layout should be planned once.

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
      the full bar, and a playtest of the numbers. In Survival a full bar adds
      uncapped time (`SurvivalMode`).

- [ ] **Level selection rework.** Not specified yet. Write down what's wrong
      with the current one before starting: is it the look, the number of
      levels per screen, not seeing best stats, or
      no place for a Survival entry? With 12 levels a map or scrolling path is
      probably more than needed. A tidy grid with stars/best stats plus an
      Survival button may be enough (Survival is on the main menu for now).

- [ ] **Survival mode.** First version is in: a separate Survival button on the
      main menu, unlocked from the start. It opens its own screen, laid out like level
      select: rules, best score and Play on top, your 10 best runs (score,
      time, date; `SaveData.survivalRuns`) below. One
      run is a 45s clock on a fixed
      board (`Assets/3_Data/Survival/Survival_Board`, Grid_FullSquare). Every
      number lives on `Assets/3_Data/Survival/SurvivalMode`: swap the board
      there, and tune score, time gains and Star spawns. Score is 10 per piece
      (matches and line breaks), +20 per piece beyond 3 in one match, 100 per
      Plus, 500 per Square Star, 300 per Double Star, and all of it is
      multiplied by the combo count (capped at x10). Time comes from a Plus
      (+5s), a full combo bar (+5s), Square Stars (+3s) and Double Stars (+2s),
      uncapped, and every gain shrinks with minutes played (curve, 1 at the
      start to 0.4 at 5 minutes). Square and Double Stars spawn at random on
      refills, max 2 each. Best score and runs played are saved; `survival_end`
      goes to Firebase with score and duration. Code: `Match3SurvivalRun`
      (scoring, clock, spawns, 11 EditMode tests), `SurvivalScoreObjective` and
      `SurvivalTimeLimit` (the top bar shows them like any objective).
      Still to do: playtest the numbers (watch `survival_end` once it ships),
      decide whether to show the best score on the menu, and a Steam
      leaderboard later (section 3).

### Content, not engineering

- **Twelve levels is roughly 25 minutes of play.** The level painter and 16 grid
  shapes are already there, several shapes unused by any level. Note also that
  `GetSpecificItemMatches` now works (an ordering bug meant it could never score)
  and no level uses it yet.

---

## 3. Distribution

The releases repo comes first so every installed copy keeps updating before
anything else changes. The Steam build turns the updater off, and the
leaderboard needs Survival.

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

---

## 4. Later

- [ ] **Music Vorbis quality is still 100%.** Needs listening, so later. The
      loading problems are fixed (see DONE.md > Music clips moved to Streaming).
      Dropping quality to 0.5-0.7 would roughly halve the build size and is
      normally transparent for game music, but A/B it by ear before changing it.

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
