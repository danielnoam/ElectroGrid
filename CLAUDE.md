# ElectroGrid: notes for Claude

Unity 6 (6000.6.2f1), URP, portrait match-3. Gameplay code is in `Assets/2_Scripts/Match3`, data in `Assets/3_Data`.
**Follow [RULES.md](RULES.md)**: what every change needs (all controls, all 7 languages, Info cards for new mechanics, TODO to DONE, licences). Read `LEVELS.md` before touching levels.

## Driving the editor

- Every editor tool and debug feature also has an agent command: `unity command eg_<name> --caller plugin --skill unity-cli --project-path "D:\Projects\ElectroGrid"`. `unity list` shows them all. Prefer these over `eval` scripts and reflection.
- Commands live in `Assets/2_Scripts/Match3/Editor/ElectroGridAgentCommands.cs` (`[CliCommand]`, `[CliArg(..., Required = true)]`). Test and debug logic lives once in `Match3DebugActions`; the Match3 Debug window buttons and the commands both call it.
- **When you add or change a tool, add or update its command in the same change**, and a Match3 Debug window button if it is a test helper.
- Several Unity projects can be open at once on the same port. Check `unity status` and pass `--project-path`; never send ElectroGrid commands to another project.
- Hot Reload is installed. It starts newly added fields at 0 and can freeze the editor, so after adding fields or files do a real recompile (Refresh) before testing.
- After testing, put things back: `eg_restore` (stops play mode, reopens Match3, Game view 1080x1920, run in background off). Never open another scene over unsaved changes.
- Dynamic TextMesh Pro font assets change on disk whenever text renders. Never `git checkout` them while the editor is open: it puts the atlas and the glyph tables out of step and letters vanish (a "B" missing from RUMBLE, Arabic going blank). To tidy them, call `ClearFontAssetData` on the dynamic fonts from the editor instead.
