# ElectroGrid — how levels work

What a level is made of, how the board behaves, and the rules we follow when
making new ones. Read this before adding or changing a level.

## What a level is

A level is an `SOMatch3Level` asset in `Assets/3_Data/Levels`, named
`Level_N`. It holds:

- **Grid shape**: an `SOGridShape` in `Assets/2_Scripts/Grid`, named
  `Grid_<Shape>` (Grid_Heart, Grid_Bolt). It sets the width, height and which
  cells are on. One shape can be used by more than one level.
- **Match objects**: the pieces that can spawn, with a chance each. Levels 1
  to 50 use the same four shapes (triangle, pentagon, circle, hexagon) at 25
  each. From 51 some levels add the Diamond, the Trapezoid or both, split
  evenly. Every extra kind makes matches rarer, so give those levels more moves.
- **Objectives**: what the player has to do to win. All of them must be done.
- **Lose conditions**: a move limit or a time limit, not both. Running out loses.
- **Tile objects**: which cells start with a Star, a Plug, a virus or a Surge
  instead of a piece. Painted in the level editor.

A level is only in the game if it is in the `match3Levels` list on the
GameManager prefab (`Assets/4_Prefabs/Managers/GameManager.prefab`). That
list is the play order: players see "Level 3" for the third entry whatever the
asset is called, and finishing a level unlocks the next one in the list.
The level editor window (Match3 Level Editor) edits the list, paints tiles,
validates, and can play a level directly.

## How the board behaves

- **Matches** are 3 or more of the same piece in a row or column.
- **Line break**: a match of 4 or more in a straight line clears its whole row
  or column. It also breaks Stars in that line.
- **Gravity** is straight down per column. Pieces fall through switched-off
  cells, so a column with a gap still refills (Hourglass and Hollow Square
  rely on this). New pieces fill whatever is empty after the fall.
- **Viruses, fuses and conveyors**

- Give viruses room to be reached: a virus walled in by Stars spreads
  with nothing you can do about it.
- Fuse Moves should leave time to set up a match, 7 or more for a first
  meeting. Put fuses where a match can reach them in a move or two.
- Keep Stars off conveyor rows unless the point is to split the row.

**Stars** (obstacles) don't move, can't be swapped, and break when a
  match happens next to them or a line break passes through them. One hit
  each. Pieces above a Star can't fall past it, so the cells under it
  only refill from new spawns.
- **Plugs** fall like pieces and are collected when they reach the
  bottom row of their column. The catchers under the board open when a level
  has Plugs.
- **Glitches** are a layer painted on cells (Glitch paint mode). A match on
  the cell or a line break through it clears it. The cell flickers cyan until
  then.
- **Viruses** sit still and break like Stars, but after every move in
  which no virus broke, one spreads onto a neighbouring piece. Fused pieces,
  Plus and Stars are safe from it.
- **Fuses** are painted on cells and land on whatever piece fills that cell.
  The number drops by one every move; matching the piece defuses it, and
  reaching 0 loses the level. The count is the level's Fuse Moves setting.
  Painting a fuse on a Star or Plug puts it on the Star: it is
  defused when the Star breaks or the Plug drops off the bottom.
- **Conveyor rows** (Conveyor Rows list, row 0 is the bottom) move every
  piece one cell along after each move and wrap at the end. Stars and
  viruses stay put and the row moves past them. Small arrows mark the row.
- **Surges** come from a straight match of 5 or more, or can be placed in the
  level editor (each one holds a chosen piece, or a random one). Swapping a
  Surge with any neighbour zaps every piece of the kind it holds. A line break
  or fuse blast that hits a Surge sets it off too, and so does another Surge
  holding the same piece.
- **Plus** pieces spawn now and then (5% per new piece). Using one gives
  extra moves or time.
- **Combo**: off unless the level's **Combo Bar** box is ticked (Survival
  always has it, and every Reach Score level should too). Matches in quick succession build the
  combo and fill the combo bar. A full bar adds 5 seconds to a time limit or
  2 moves to a move limit, but never above the level's starting amount. In
  Survival a full bar adds nothing; the combo only multiplies points.
- **Score**: every level keeps a score, using the Level Score settings on
  `Match3GameManager` (10 per piece, more for longer matches, bonuses for
  Plus and Stars, multiplied by the combo when the level has one). Only the
  Reach Score objective shows it.
- The board is always generated with at least 3 possible moves, and it is
  reshuffled if the player runs out of moves.

## Objectives

| Objective | Asks for | Notes |
|---|---|---|
| Get Matches | N pieces cleared | Counts every piece, including line breaks. |
| Destroy Obstacles | N Stars broken | N can't be more than the Stars placed. |
| Reach Bottom | N Plugs collected | N can't be more than the Plugs placed. |
| Reach Score | N points | Needs a lose condition and the Combo Bar box ticked (the validator warns about both). Stars (300) and Plugs (500) add a lot, so count them in. First used by Level 21. |
| Get Specific Item Matches | N of one piece | Not used: it plays badly, so levels use Get Matches instead. |
| Clear Glitches | N glitches cleared | N can't be more than the glitches painted. First used by Level 18. |
| Clear Viruses | Every virus gone | Counts down as you break them and up as they spread. Needs at least one virus. First used by Level 19. |
| Defuse | N fuses matched | Any fuse that runs out loses the level, goal or not. First used by Level 24. |

Only one of each objective type per level. Two objectives fit the top bar;
every level from 6 on uses two.

## Rules for new levels

**Grids**

- At most 11 cells wide and 10 tall. The camera fits the board to the screen,
  so bigger boards make every cell smaller on a phone.
- Every part of the board should be at least 3 cells wide or tall so matches
  can happen there. A single cell can only ever match one way.
- Set the grid origin's Y to -1 for boards 7 rows tall or fewer and -1.5 for
  taller ones, like the existing grids.
- A new level should get a new grid or a clear twist on an old one. The shape
  is half of what makes a level feel new.

**Plugs**

- Never on the bottom row: they would score the moment the level starts.
- Only in columns whose lowest switched-on cell is on row 0, or they can
  never be collected. The validator checks both.
- A Star directly below a Plug is allowed and worth using now
  and then: the player has to break it before the Plug can fall.
  Levels 10, 12, 31, 40 and 50 do this.

**Stars**

- Leave at least 9 cells for pieces (the validator warns below that), and in
  practice far more. Most levels keep Stars to about a sixth of the
  board.
- Spread them so matches can reach each one. A Star surrounded by other
  Stars can only be broken by a line break.

**Difficulty**

- Each new level should be a little harder than the one before it, or teach
  something new. Introduce one new idea at a time: Level 6 brings Stars,
  Level 8 brings Plugs, Level 10 is the first to mix them, then
  glitches at 18, viruses at 19, fuses at 24 and conveyors at 26.
- Moves so far run between 9 and 28. Use 14 to 18 for levels with two
  objectives. Fewer cells or more stars means more moves.
- One limit per level: moves or time, never both. Use a time limit now and
  then instead of moves. Keep time limits at 45 seconds or less.
- Playtest every new level on a phone before it ships. Losing on the last move
  a few times is about right; losing with half the goal left is too hard.

**Before it ships**

- No errors in the level editor's validator.
- Load it in Play mode at the phone shape (672x1496 in the Game view) and in
  Hebrew to check the board and the top bar fit.
- Add it to the end of `match3Levels` unless it is meant to go earlier. Moving
  a level changes every later level's number and which levels players have
  unlocked.

## Current levels

| # | Grid | Objectives | Limit |
|---|---|---|---|
| 1 | FullSquareSmall 6x6 | 16 pieces | none |
| 2 | Heart 8x8 | 19 pieces | 10 moves |
| 3 | HollowSquare 8x8 | 24 pieces | 9 moves |
| 4 | Plus 9x9 | 29 pieces | 45s |
| 5 | H 8x8 | 46 pieces | 13 moves |
| 6 | Circle 8x8 | 16 pieces, 4 Stars | 13 moves |
| 7 | Smile 10x10 | 16 pieces, 14 Stars | 9 moves |
| 8 | FullBlock 8x6 | 21 pieces, 2 Plugs | 16 moves |
| 9 | NoCornerShortSquare 9x7 | 26 pieces, 4 Plugs | 18 moves |
| 10 | TriangleSquashed 11x6 | 1 Plugs, 9 Stars | 16 moves |
| 11 | Circle2 9x9 | 30 pieces, 9 Stars | 45s |
| 12 | ChestPlate 9x8 | 8 Stars, 1 Plugs | 14 moves |
| 13 | Hourglass 9x9 | 24 pieces, 3 Plugs | 16 moves |
| 14 | Bolt 9x10 | 26 pieces, 8 Stars | 16 moves |
| 15 | Crown 9x8 | 6 Stars, 3 Plugs | 18 moves |
| 16 | Diamond 9x9 | 27 pieces, 4 Stars | 45s |
| 17 | Arrow 9x10 | 24 pieces, 3 Plugs | 18 moves |
| 18 | Donut 9x9 | 18 glitches, 8 Stars | 16 moves |
| 19 | Steps 9x9 | all viruses, 3 Plugs | 18 moves |
| 20 | Battery 8x10 | 8 Stars, 2 Plugs | 20 moves |
| 21 | Cross 9x9 | 900 points | 45s |
| 22 | Castle 9x9 | 7 Stars, 2 Plugs | 18 moves |
| 23 | Bowtie 11x7 | 32 pieces | 45s |
| 24 | Ladder 9x10 | 28 pieces, 3 fuses (9 moves each) | 18 moves |
| 25 | Chip 10x10 | 4000 points, 12 Stars | 22 moves |
| 26 | Star 9x9 | 30 matches, conveyors on rows 6 | 18 moves |
| 27 | V 11x7 | 30 pieces, all viruses | 18 moves |
| 28 | House 9x9 | 18 glitches, 2 Plugs | 18 moves |
| 29 | Tree 9x10 | 1150 points | 45s |
| 30 | Moon 9x9 | 3 Stars, 3 fuses (9 moves each) | 17 moves |
| 31 | Eight 7x9 | 5 Stars (one under a Plug), 2 Plugs, conveyors on rows 7 and 5 | 18 moves |
| 32 | T 9x8 | 1600 points, 2 Plugs | 20 moves |
| 33 | Arch 11x8 | 14 glitches | 45s |
| 34 | Hexagon 9x9 | 30 matches, all viruses | 20 moves |
| 35 | Shield 9x9 | 3 fuses (8 moves each, one on a Plug), 2 Plugs | 18 moves |
| 36 | Dumbbell 11x6 | 23 pieces, 5 Stars | 45s |
| 37 | Snake 9x10 | 1150 points | 45s |
| 38 | Pyramid 11x6 | 2 Plugs, 5 Stars, conveyors on rows 2 | 18 moves |
| 39 | Butterfly 11x9 | 13 glitches, 19 matches | 45s |
| 40 | Fortress 10x10 | 11 Stars (one under a Plug), 2 Plugs | 22 moves |
| 41 | Boat 11x6 | 2000 points, 2 Plugs | 20 moves |
| 42 | Gem 9x9 | 23 pieces, all viruses | 45s |
| 43 | Comb 11x8 | 30 pieces, 3 Plugs, conveyors on rows 6 and 5 | 20 moves |
| 44 | Frame 10x10 | 5000 points, 12 Stars | 22 moves |
| 45 | Trophy 9x10 | 3 fuses (8 moves each), 25 matches | 20 moves |
| 46 | Clover 9x9 | 28 glitches, 5 Stars | 45s |
| 47 | Window 10x10 | 4000 points, 4 Stars, conveyors on rows 8 and 1 | 22 moves |
| 48 | Bridge 11x8 | 2 Plugs, all viruses | 20 moves |
| 49 | Skull 9x10 | 4500 points, 3 fuses (8 moves each) | 24 moves |
| 50 | Circuit 11x10 | 10 Stars (one under a Plug), 3 Plugs, conveyors on rows 7 and 5 | 26 moves |
| 51 | Square7 7x7 | 22 pieces | 12 moves |
| 52 | Tall 6x9 | 24 pieces, + Diamond | 15 moves |
| 53 | Wide 9x6 | 26 pieces, + Diamond | 40s |
| 54 | Ring 9x9 | 22 pieces, 4 Stars, + Diamond | 17 moves |
| 55 | Pill 9x7 | 28 pieces, + Trapezoid | 16 moves |
| 56 | Envelope 9x8 | 11 glitches, 24 pieces | 16 moves |
| 57 | Cup 9x9 | 36 pieces, 2 Surges | 14 moves |
| 58 | Rocket 7x10 | 2 Plugs, 22 pieces, + Diamond | 18 moves |
| 59 | Petri 9x9 | all viruses, 26 pieces | 18 moves |
| 60 | Gate 10x10 | 4 Stars, 2 Plugs, 2 Surges, + Trapezoid | 24 moves |
| 61 | Lens 8x8 | 32 pieces | 40s |
| 62 | Belt 10x7 | 30 pieces, 4 Stars, conveyors on rows 3, + Diamond | 18 moves |
| 63 | Dynamite 9x9 | 3 fuses (9 moves each), 26 pieces | 18 moves |
| 64 | Bars 11x8 | 1400 points | 45s |
| 65 | Garden 9x9 | 18 glitches, 2 Plugs | 20 moves |
| 66 | Lab 9x8 | all viruses, 28 pieces, 1 Surge, + Diamond | 18 moves |
| 67 | Field 10x10 | 30 pieces, + Diamond, Trapezoid | 20 moves |
| 68 | Checker 9x9 | 8 Stars, 22 pieces, + Trapezoid | 45s |
| 69 | Mill 9x9 | 2 Plugs, 30 pieces, conveyors on rows 4 and 2 | 18 moves |
| 70 | Reactor 11x10 | 3 fuses (9 moves each), 4 Stars, 2 Surges, + Diamond | 24 moves |
| 71 | Square8 8x8 | 38 pieces | 45s |
| 72 | Hook 9x8 | 4 Stars, 2 Plugs | 20 moves |
| 73 | Lattice 9x9 | 16 glitches | 45s |
| 74 | Fireworks 9x9 | 2200 points, 4 Surges | 18 moves |
| 75 | Cannon 10x8 | 2 fuses (9 moves each), 2 Plugs, + Diamond | 20 moves |
| 76 | Outbreak 10x9 | all viruses, 24 pieces | 45s |
| 77 | Highway 11x7 | 36 pieces, conveyors on rows 1 and 3 and 5, + Trapezoid | 18 moves |
| 78 | Anchor 10x10 | 4 Stars, 26 pieces | 20 moves |
| 79 | Prism 9x9 | 18 glitches, 24 pieces, 1 Surge, + Diamond | 18 moves |
| 80 | Mainframe 11x10 | all viruses, 2 Plugs, conveyors on rows 4 | 26 moves |
| 81 | Square9 9x9 | 40 pieces | 45s |
| 82 | Clock 9x9 | 4 fuses (10 moves each), 24 pieces | 18 moves |
| 83 | Zigzag 10x8 | 1300 points | 45s |
| 84 | Temple 9x9 | 6 Stars, 28 pieces, 1 Surge, + Trapezoid | 20 moves |
| 85 | Elevator 7x10 | 3 Plugs, 30 pieces, + Diamond | 22 moves |
| 86 | Infection 9x9 | 16 glitches, all viruses | 22 moves |
| 87 | Assembly 10x8 | 3 fuses (9 moves each), 28 pieces, conveyors on rows 4 and 2 | 20 moves |
| 88 | Arena 10x9 | 28 pieces, 4 Stars, + Diamond, Trapezoid | 24 moves |
| 89 | Volcano 11x9 | 2 Plugs, 30 pieces, 2 Surges, + Trapezoid | 20 moves |
| 90 | Core 11x10 | 16 glitches, 3 fuses (10 moves each), + Diamond | 26 moves |
| 91 | Slab 7x8 | 30 pieces | 14 moves |
| 92 | Twins 10x7 | all viruses, 26 pieces, + Diamond | 20 moves |
| 93 | Vault 10x10 | 4500 points, 12 Stars | 24 moves |
| 94 | Staircase 9x9 | 8 glitches, 26 pieces, + Trapezoid | 45s |
| 95 | Power 9x10 | 3000 points, 5 Surges, + Diamond | 18 moves |
| 96 | Depot 11x8 | 2 Plugs, 4 Stars, conveyors on rows 4 and 2 | 22 moves |
| 97 | Quarantine 9x9 | all viruses, 4 fuses (10 moves each) | 22 moves |
| 98 | Spectrum 11x8 | 2000 points, + Trapezoid | 45s |
| 99 | Labyrinth 11x10 | 24 glitches, 6 Stars, 2 Surges, + Diamond | 26 moves |
| 100 | Finale 11x10 | 3 Plugs, 3 fuses (10 moves each), 1 Surge, + Trapezoid | 28 moves |
