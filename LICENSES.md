# ElectroGrid: third-party licenses

This lists everything third-party that ships in a build. It was worked out from the build's actual dependencies: the
enabled scenes, every `Resources` folder and the Addressables groups. Assets that sit in the project but don't ship
(for example the Free UI Soundpack and the TextMesh Pro example scenes) are left out. Last checked 2026-10-16.

**One font blocks a commercial release until it is licensed or replaced. See "Needs action" below.** DS-Digital was replaced by Share Tech Mono on 2026-10-16; its asset is still in the project but nothing uses it, so it doesn't ship.

## Needs action

| Asset | Used for | Problem | Options |
|---|---|---|---|
| **Abduction2002** by Jakob Fischer (Pizzadude.dk) | The logo and window titles (5 prefabs and scenes) | No authoritative licence found. Mirrors list it as "free for personal use"; the author sells commercial licences for his other fonts | Ask Jakob Fischer (pizzadude.dk) for a commercial licence, or replace it |

## Cleared

| Asset | Author | Source | License | Credit required |
|---|---|---|---|---|
| SCI-FI User Interface SFX | Sound Works 12 | [Unity Asset Store](https://assetstore.unity.com/packages/audio/sound-fx/sci-fi-user-interface-sfx-182246) (free) | Standard Unity Asset Store EULA | No (credited) |
| Force Field - Sci-Fi Collection | Alpine Audio | [Unity Asset Store](https://assetstore.unity.com/packages/audio/sound-fx/force-field-sci-fi-collection-113256) (free) | Standard Unity Asset Store EULA | No (credited) |
| Neon Pulse – Free Action Arcade Music Pack (Synthstorm) | Soma Animus | [itch.io](https://soma-animus.itch.io/arcade-music-pack-neon-pulse) | Royalty-free for commercial and personal projects | Optional, "appreciated" (credited) |
| Free Shooter Synthwave Music Pack (Shooter Synthwave 1, 3, 4, 5) | alkakrab | [itch.io](https://alkakrab.itch.io/free-shooter-synthwave-music-pack) | "100% copyright free"; the author confirmed Steam use in the comments | No (credited). The credits used to say Alpine Audio by mistake |
| Synthwave Music - Volume I (Electronic Wave, Final, Fly Above) | Alan Dalcastagne (AD Sounds) | [itch.io](https://ad-sounds.itch.io/synthwave-music-pack) | Non-exclusive licence for commercial games, no remixing or claiming as your own | "No credits needed" (credited) |
| Violet Themed UI (slider, toggle, icons, backgrounds) | Gieniel Villacote | [Unity Asset Store](https://marketplace.unity.com/packages/2d/gui/violet-themed-ui-235559) (free) | Standard Unity Asset Store EULA | No (credited) |
| Input Prompts (24 icons) | Kenney | [kenney.nl](https://kenney.nl/assets/input-prompts) | CC0 1.0 (public domain) | No (credited) |
| PrimeTween | Kyrylo Kuzyk | Unity Asset Store (free) | Standard Unity Asset Store EULA | No (credited) |
| Share Tech Mono (most UI text, in capitals) | Carrois Apostrophe | Google Fonts | SIL OFL 1.1 (`OFL.txt` next to the font) | No (credited) |
| Noto Sans Hebrew, Noto Sans Arabic | Google | Google Fonts | SIL Open Font License 1.1 (`OFL.txt` next to each font) | No (credited) |
| Roboto Bold (Main scene) | Christian Robertson | Bundled with TextMesh Pro | Apache 2.0 | No (credited) |
| Anton (tutorial cards) | Vernon Adams | Bundled with TextMesh Pro | SIL OFL 1.1 | No (credited) |
| Liberation Sans (TextMesh Pro default and fallback) | Red Hat | Bundled with TextMesh Pro | SIL OFL 1.1 | No |
| Firebase Unity SDK (Analytics, Crashlytics) | Google | firebase.google.com | Apache 2.0 | Keep its notices; credited |
| External Dependency Manager | Google | Bundled with Firebase | Apache 2.0 (`Assets/ExternalDependencyManager/Editor/LICENSE`) | Editor only, doesn't ship |
| Unity packages (URP, Input System, Localization, uGUI, Addressables, TextMesh Pro) | Unity | Package Manager | Unity Companion License | No |
| DNExtensions | Daniel Noam | github.com/danielnoam/DNExtensions | Own work | No |
| Hot Reload | The Naughty Cult | Asset Store | Editor only, doesn't ship | No (credited anyway) |

## Ships by accident

`Assets/Plugins/TextMesh Pro/Examples & Extras/Resources` is a `Resources` folder, so every example font and
material in it goes into the build even though only Roboto Bold and Anton are used. That includes **Bangers** and
**Oswald** (both OFL, fine) and **Electronic Highway Sign** (licence unclear). Moving the unused example fonts out of
`Resources`, or deleting TextMesh Pro's Examples & Extras, removes the question and makes the build a little smaller.

## Own work (confirm)

Piece, Star, Plug and obstacle sprites (`Assets/Resources/Sprites`), the game's shaders (`Assets/Resources/Shaders`),
the grid art and all text and translations are assumed to be made for ElectroGrid. Confirm that none of them came from
a pack.

## Unity splash screen

The splash screen is off (`m_ShowUnitySplashScreen: 0`). Since Unity 6, Personal projects may turn it off, so this
is fine on any plan.
