# ElectroGrid rules

What every change needs before it counts as done. These hold for anyone working on the game, person or Claude.
[CLAUDE.md](CLAUDE.md) adds how to drive the editor.

## Every change

- **Finished work moves from TODO to DONE in the same change.** If what you finished was a TODO item (or part of
  one), take it out of [TODO.md](TODO.md) (or cut it down to what is left) and add an entry to [DONE.md](DONE.md)
  saying what was done and why it is that way. Nothing finished stays in TODO.
- Ideas that aren't decided yet go in [IDEAS.md](IDEAS.md), not TODO. When one is picked, it moves to TODO.
- Levels follow [LEVELS.md](LEVELS.md), and the level validator stays clean (`eg_validate`).

## Controls: every action works on every input

The game is played with touch, mouse, keyboard and gamepad. A new action isn't done until all four can do it.

- **Bindings go in the UI map of `Assets/3_Data/InputSystem_Actions`**, with a Gamepad binding and a
  Keyboard&Mouse binding. Code reads the action through `ControllerFocus.FindUiAction`; never read
  `Gamepad.current` or `Keyboard.current` buttons directly.
- **Every button that has an action gets a `ButtonPrompt`** naming it, so its key or pad button shows on a badge.
  Use Keyboard Action Name when the keyboard should use a different action (Home uses Escape in a level).
- **Board actions also go in `ControlHintsBar`**, the controls bar under the board in a level.
- **Every new screen and window gets a `FocusScope`** on its root: first selection, cancel button, and a keyboard
  cancel button if Escape should do something other than B. A level's top bar scope is marked Controls Game.
- Keep the shared keys meaning the same thing everywhere: Space and Enter (A) select, Escape and B go back, Tab and
  Menu open Settings, I and Y open Info, R and View restart, Q and E (LB and RB) jump to a row's ends. On the main
  menu C and View open Credits and Escape and B quit. Quit and Restart need a 1.5s hold on keyboard and controller
  (Hold Seconds on their `ButtonPrompt`), so they can't happen by accident. The right stick (Page Up and Page Down) scrolls lists; the left
  stick and d-pad move the selection.
- Menus read the same UI map: `ControllerFocus` gives the EventSystem's input module a copy of it, so a binding added
  there works on buttons too.

## Languages: every word in all 7

English, Spanish, Brazilian Portuguese, German, French, Hebrew and Arabic.

- **No player-facing text in code or scenes without a localization key.** Add the key to the right table (`UI` or
  `Content`) with all 7 translations in the same change, and read it through `L10n`.
- Check new screens at phone size (Game view 672x1496) in at least one right-to-left language. Hebrew and Arabic
  mirror the layout (`RtlLayoutMirror`); content whose left and right mean something gets `RtlMirrorIgnore`.
- Digits stay Latin in every language (RTL labels keep Preserve Numbers on). Numbers shown to the player are
  formatted for the language (`ToString("N0", culture)`).
- Text has to fit in every language: long German and French words, Arabic shaping. Use auto-size or room to grow.

## New mechanics and pieces

A new mechanic (piece, obstacle, power-up, objective or rule) isn't done until:

- **It has an Info card**: a tutorial asset with a sprite, a title and text in all 7 languages, listed in the Info
  window. The first level that uses it shows the card as its intro.
- It has its own feedback: a sound (`AudioLibrary` id), a haptic moment (`MobileHaptics.Play`, adding a `Haptic`
  value if none fits) and, where it helps, camera shake.
- It can be placed in the level editor, checked by the level validator, and spawned from the Match3 Debug window and
  an `eg_` command.
- It works in Survival if it shows up there, and its numbers live in the inspector, grouped under its own header.
- LEVELS.md says where it comes in on the level curve.

## Assets and licences

- Check the licence before anything third-party goes in: it must allow a commercial release. Add it to
  [LICENSES.md](LICENSES.md) and to the Credits window in the same change. Fonts need a licence that covers
  commercial games; "free for personal use" doesn't.
- Downloads come from the original source (the author, the Asset Store, Google Fonts, Kenney).

## Tools

- Every editor tool and debug feature gets an agent command (`eg_...`) in `ElectroGridAgentCommands`, with its logic
  in `Match3DebugActions`, and a Match3 Debug window button if it helps testing.

## Renaming a mechanic: sweep everything

A rename is not done until no old name is left anywhere a person or a later change will read it. When a mechanic is
renamed (Square Star became Plug, Double Star became Star), go through all of these in the same change:

- Classes, methods, properties, fields, locals, enum values and test names.
- Serialized fields: rename with `[FormerlySerializedAs("oldName")]` so assets and scenes keep their values, then
  check the values survived.
- Inspector text: `[Header]`, `[Tooltip]`, labels and section names in custom editors and windows.
- Localization keys and strings in all 7 languages, Info cards and tutorials.
- Docs: `LEVELS.md`, `TODO.md`, `DONE.md`, `README.md`.

Search case-insensitively for the old words (for example `square.?star|double.?star`) before calling it done.

## Inspector layout

Group settings by the thing they configure, not by kind of value. Everything about one obstacle (spawn chance, cap,
time gain, ...) sits under that obstacle's header, rather than all chances in one block and all time gains in
another. Name sections for what they are: obstacles are obstacles, points are points.

## Code

- No code comments; XML `<summary>` only where it explains something non-obvious, and kept short.
- Match the surrounding code. Use PrimeTween for tweens, ObjectPooler for anything spawned often (register new pools
  in `Resources/ObjectPoolingSettings.asset`), and `AudioLibrary` ids for sounds.
- Objects that come back from the pool must reset anything a tween or effect changed (scale, colour).
- Vibration goes through `MobileHaptics` (`Play(Haptic)` for game moments), which turns into controller rumble while
  a gamepad is in use. Don't call the DNExtensions haptics directly.
- Keep the SRP Batcher happy: share materials (see `EmissionMaterials`) instead of `renderer.material` per object,
  and don't put property blocks on pieces or tiles.

## Before a release build

EditMode tests pass, the level validator is clean (`eg_validate`), and the phone shape (Game view 672x1496) is
checked in Hebrew and at least one long-word language (German or French). Builds go through the build tool
(`eg_build`, then `eg_build_status`).
