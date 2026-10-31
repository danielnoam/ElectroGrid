# ElectroGrid

A match-3 puzzle game for mobile, made in Unity 6 (6000.2) with URP.

This repository is the version of ElectroGrid up to February 2026: the grid system, match-3 gameplay, level data and a
level editor, objectives and lose conditions, tutorials, menus with level selection, audio and mobile haptics.
Development continues privately.

## Notes

- The Firebase SDK and its config files are not included, so `FirebaseManager` won't compile until the
  [Firebase Unity SDK](https://firebase.google.com/docs/unity/setup) (Analytics, Remote Config) is imported.
- Code is in `Assets/2_Scripts`; the match-3 game is in `Assets/2_Scripts/Match3` and the `Match3` scene.
