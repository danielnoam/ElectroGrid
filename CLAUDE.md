# ElectroGrid: notes for Claude

Unity 6 (6000.6.2f1), URP, portrait match-3. Gameplay code is in `Assets/2_Scripts/Match3`, data in `Assets/3_Data`.
Read `LEVELS.md` before touching levels, and keep `TODO.md` and `DONE.md` current: move a finished item from TODO to DONE in the same change.

## Driving the editor

- Every editor tool and debug feature also has an agent command: `unity command eg_<name> --caller plugin --skill unity-cli --project-path "D:\Projects\ElectroGrid"`. `unity list` shows them all. Prefer these over `eval` scripts and reflection.
- Commands live in `Assets/2_Scripts/Match3/Editor/ElectroGridAgentCommands.cs` (`[CliCommand]`, `[CliArg(..., Required = true)]`). Test and debug logic lives once in `Match3DebugActions`; the Match3 Debug window buttons and the commands both call it.
- **When you add or change a tool, add or update its command in the same change**, and a Match3 Debug window button if it is a test helper.
- Several Unity projects can be open at once on the same port. Check `unity status` and pass `--project-path`; never send ElectroGrid commands to another project.
- Hot Reload is installed. It starts newly added fields at 0 and can freeze the editor, so after adding fields or files do a real recompile (Refresh) before testing.
- After testing, put things back: `eg_restore` (stops play mode, reopens Match3, Game view 1080x1920, run in background off). Never open another scene over unsaved changes.

## Renaming a mechanic: sweep everything

A rename is not done until no old name is left anywhere a person or a later change will read it. When a mechanic is renamed (Square Star became Plug, Double Star became Star), go through all of these in the same change:

- Classes, methods, properties, fields, locals, enum values and test names.
- Serialized fields: rename with `[FormerlySerializedAs("oldName")]` so assets and scenes keep their values, then check the values survived.
- Inspector text: `[Header]`, `[Tooltip]`, labels and section names in custom editors and windows.
- Localization keys and strings in all 7 languages, Info cards and tutorials.
- Docs: `LEVELS.md`, `TODO.md`, `DONE.md`, `README.md`.

Search case-insensitively for the old words (for example `square.?star|double.?star`) before calling it done.

## Inspector layout

Group settings by the thing they configure, not by kind of value. Everything about one obstacle (spawn chance, cap, time gain, ...) sits under that obstacle's header, rather than all chances in one block and all time gains in another. Name sections for what they are: obstacles are obstacles, points are points.

## Code style

- No code comments; XML `<summary>` only where it explains something non-obvious, and kept short.
- Match the surrounding code. Use PrimeTween for tweens, ObjectPooler for anything spawned often (register new pools in `Resources/ObjectPoolingSettings.asset`), and `AudioLibrary` ids for sounds.
- Objects that come back from the pool must reset anything a tween or effect changed (scale, colour).

## Before a release build

EditMode tests pass, the phone shape (Game view 672x1496) and Hebrew are checked, and the level validator is clean (`eg_validate`). Builds go through the build tool (`eg_build`, then `eg_build_status`).
