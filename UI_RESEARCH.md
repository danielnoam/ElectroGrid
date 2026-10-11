# ElectroGrid UI research: main menu, level select, in-game

What good mobile puzzle games do, where ElectroGrid stands today, and what to change. Sources are at the bottom. Where a point comes from playing the genre (Royal Match, Candy Crush, Two Dots, Block Blast, Helix Jump) rather than from a written source, it says so.

## The rules that apply everywhere

1. **One main action per screen.** Every screen should have one obvious thing to press, bigger and brighter than everything else. Secondary actions go to icons or a menu. (GDevelop mobile UX guide; Two Dots analysis.)
2. **Put that action where the thumb is.** The lower-middle of a portrait phone is the easy reach zone. Top corners are a stretch on big phones, which makes them a good place for things you *don't* want tapped by accident (pause, settings). (Hoober's thumb zone, via Fluid UI and Parachute Design.)
3. **Touch targets at least 44pt (iOS) / 48dp (Android)**, with space between them. ElectroGrid's 100px corner icons at the 1080 reference are fine; the risk is buttons placed close together, like the old bottom bar. (Deque, BrowserStack.)
4. **Contrast and hierarchy beat decoration.** The 2024 Royal Match vs Candy Crush study (331 participants) found colour contrast, harmony and clear hierarchy drove the experience. Royal Match's own lesson #1 is "keep your colours simple": vibrant, high-contrast pieces. ElectroGrid's neon-on-black already has this; protect it by keeping the UI greyscale so the pieces stay the most colourful thing on screen.
5. **Readable type over stylised type.** The Candy Crush UX review calls out its stylised font as hurting clarity. DS-DIGI and abduction are fine for titles and numbers, but long text (rules, tutorials) should stay in the plain font, as it is now.
6. **Every tap answers back**, and big moments answer bigger: press scale, click sound, haptic. Screen shake 0.1–0.3s, scaled to the event, and kept rare so it means something. Stacking shake + flash + sound on every tap becomes noise. (Itch.io juice guide, Wayline "Juice Overload".)
7. **Get the player into play fast.** Hyper-casual and puzzle hits get you playing within seconds of launch (Stack: tap anywhere; Helix Jump: Play / Shop / Settings only). Every popup between launch and the board costs players; Two Dots reviewers complain specifically about popups before and after levels.
8. **Safe areas.** Keep all text and buttons inside the safe area (notch, gesture bar). ElectroGrid already has `SafeAreaMargin` on the bottom bars; the top bar needs the same check on notched phones.

## Main menu

**What the best ones do**
- One huge Play button in the lower-middle, usually showing the level it will start ("Level 13"). Royal Match's home has two big buttons under the main view, Level and Tasks (Baijing analysis). Candy Crush and Royal Match send you straight to the next level from that button; level browsing is secondary.
- Everything else is small icons around the edges: settings, info, credits, shop.
- Little to no reading. The Two Dots opening screen is basically the name and "play".

**ElectroGrid today:** title, then four equal buttons stacked in the centre (Continue, Level Select, Survival, Quit), with credits, info and settings as corner icons.

**Recommendations**
1. **Make Continue the hero.** Bigger, brighter, lower, and labelled with the level: "Level 7" (or "New Game" on a fresh save). Level Select and Survival become two smaller buttons under it, side by side.
2. **Show the Survival best on its button** ("Survival · Best 1,980") so the mode advertises itself. This also settles the open TODO question.
3. **Hide Quit on mobile.** Android players leave with the back gesture; a Quit button on a phone is a desktop habit. Keep it on Windows only.
4. Keep the title, but let the board-style background breathe; no extra panels.

## Level selection

**What the best ones do**
- **Grids** suit touch and make old levels easy to revisit; **maps** suit games with a theme or branching paths but make browsing back harder (Candy Crush is the usual example). Chapters of small grids are the common hybrid (Angry Birds). With 12 levels, a grid is right.
- Each level button shows its state at a glance: completed (tick or stars), current (highlighted, maybe pulsing), locked (dimmed or padlock).
- Stars or a best result give a reason to replay.
- Tapping a level shows its goal and a big Play. Royal Match and Candy Crush do this as a popup before the level; ElectroGrid does it in a panel on the same screen, which is fine and faster.
- Save high contrast for things you can tap; everything else stays quiet (Geodessey devlog).

**ElectroGrid today:** the info panel and Start on top, a scrolling grid of plain numbered buttons below, Back bottom-left. Locked levels show "?". Nothing on the buttons says which levels are done or how well.

**Recommendations**
1. **Put state on the buttons**: a small tick or 1–3 stars on completed levels, a highlight on the next unplayed level, a padlock icon on locked ones (instead of "?").
2. **Stars.** Three stars per level from moves (or time) left at the end, saved in the level record. This is the single biggest replay driver in the genre, and the data is already there (best moves and time are saved).
3. **Preselect the next level** when the screen opens, so Start works with one tap instead of two.
4. Keep the board preview and goal text; they are good and most competitors don't show the board shape.
5. If levels grow past ~20, split into chapters of 12 with a page swipe, rather than one long scroll.

## In-game

**What the best ones do (from playing them; the HUD layouts aren't written up anywhere I found)**
- **Top: goals and moves.** Royal Match and Candy Crush keep the goals and the moves/time counter together at the top, close to each other, with the moves number big. Two Dots splits the screen into three rows: top is "how to beat the level" (moves and goals), middle is the board, bottom holds less important things like score and power-ups.
- **Goals as icons with counts**, not words. A goal that is done gets a tick.
- **Pause/settings in a corner**, out of the way. Quit and restart live inside it, so a stray tap can't throw away a level. Restart usually asks first.
- **Warnings when it's close:** few moves left makes the counter pulse or turn red.
- **Feedback near the action:** score popups at the matched pieces, bigger for bigger combos; combo text for chains.
- **Start of level:** a short goal banner ("Collect 21 pieces") that the player taps away or that dismisses itself, so the goal is clear before the first swap.
- **End of level:** win screen with stars and one big Next; lose screen with one big Try Again. Out-of-moves offers (extra moves) exist in free-to-play games but need an economy; skip.

**ElectroGrid today:** a two-row top box (goals on one row, moves/time on the other) with the level name under it and the combo bar beside the name; four icons along the bottom (home, restart, info, settings) with nothing between you and quitting.

**Recommendations, in order**
1. **Pause menu.** One pause button in the top corner. It opens a panel with Resume, Restart, How to Play, Settings, Main Menu. The bottom bar goes away, giving the board more room.
2. **Restart confirm** inside it ("Tap again to restart", the Reset Progress pattern).
3. **One-row top bar:** goals on the left as piece icons with counts, moves or time on the right as one big number; Survival shows the score on the left. Level name as a small label above; the combo bar full width under the row.
4. **Goal done → tick** and the goal dims.
5. **Low warning:** moves ≤ 5 or time ≤ 10s pulses the number red.
6. **Score popups** ("+120") at the matched pieces, scaled by combo, short-lived, never covering the counters.
7. **Goal banner at level start** for one or two seconds, using the same goal icons, so the first move is informed.
8. **Results screen:** stars for the win (once stars exist), one big primary button (Next / Try Again), Main Menu as the small one.

## What to build, in order

| # | Change | Screen | Why first |
|---|---|---|---|
| 1 | Pause menu + restart confirm | In-game | Fixes accidental quits and restarts, frees the bottom of the screen |
| 2 | One-row top bar with goal icons, low-move warning | In-game | Readability during play |
| 3 | Continue as the hero button, Survival best on its button | Main menu | One tap to play |
| 4 | Stars per level (saved), shown on results and level buttons | Results, level select | The genre's main replay reason |
| 5 | Level button states and preselecting the next level | Level select | One-tap start, progress at a glance |
| 6 | Score popups, goal banner | In-game | Polish |

## Sources

- [The effects of visual design principles on game experience: Royal Match and Candy Crush Saga (IYTE thesis, 2024)](https://gcris.iyte.edu.tr/entities/publication/261378b1-1b04-494d-b789-8e24264b5bc1)
- [5 Simple UX Lessons From Royal Match (This Week in LiveOps)](https://thisweekinliveops.substack.com/p/5-simple-ux-lessons-from-royal-match)
- [Royal Match Dominates Match-3: What Can All Designers Learn? (Funovus)](https://www.funovus.com/blogs/royal-match-dominates-match-3-what-can-all-designers-learn/)
- [Royal Match analysis (Baijing, Chinese)](https://www.baijing.cn/article/34400)
- [Does Candy Crush have a good UX? (UX Collective)](https://uxdesign.cc/does-candy-crush-have-a-good-ux-3c1a865d24e)
- [Two Dots iPhone App, good design (Pratt IxD)](https://ixd.prattsi.org/2015/02/two-dots-iphone-app-good-design/)
- [Visual design of games: Two Dots (Mechanics of Magic)](https://mechanicsofmagic.com/2023/04/21/short-exercise-visual-design-of-games-cheese-or-font-and-two-dots/)
- [Two Dots review (Pocket Gamer)](https://www.pocketgamer.com/twodots/twodots/)
- [Two Dots player feedback report (Kimola)](https://kimola.com/reports/unlock-player-insights-with-two-dots-feedback-analysis-report-app-store-my-151207)
- [Designing a level select interface (Geodessey devlog)](https://feitizogames.itch.io/geodessey/devlog/56371/designing-a-level-select-interface)
- [Level select grids vs maps (Game Dev Stack Exchange answer)](https://backiee.wasmer.app/http_gamedev_stackexchange_com/a/61837)
- [Admiring the game design in hyper-casuals (GameDev.net)](https://www.gamedev.net/articles/game-design/game-design-and-theory/admiring-the-game-design-in-hyper-casuals-r5146/)
- [UI/UX design in hypercasual games (Pixune)](https://pixune.com/blog/hypercasual-games-ui-ux-design-guide/)
- [5 mobile UX mistakes that cost you players (GDevelop)](https://gdevelop.io/blog/mobile-ux-mistakes)
- [The thumb zone (Fluid UI)](https://blog.fluidui.com/the-thumb-zone/)
- [Thumb zone UX (Parachute Design)](https://parachutedesign.ca/blog/thumb-zone-ux/)
- [Touch target size (Deque)](https://dequeuniversity.com/rules/attest-ios/1.0/touch-target-size)
- [Touch target size (BrowserStack)](https://browserstack.com/docs/app-accessibility/rule-repository/rules-list/touch-target/touch-target-size)
- [Making a game feel juicy with simple effects (itch.io)](https://itch.io/blog/1059831/making-a-game-feel-juicy-with-simple-effects)
- [Juice overload: when sensory feedback hurts gameplay (Wayline)](https://www.wayline.io/blog/juice-overload-sensory-feedback-hurts-gameplay)
- [UI and UX in games: menus, HUDs and feedback (Outlook Respawn)](https://respawn.outlookindia.com/gaming/gaming-guides/ui-and-ux-in-games-building-menus-huds-and-feedback-systems)
- [UI and UX design (Roblox creator docs)](https://create.roblox.com/docs/production/game-design/ui-ux-design)
