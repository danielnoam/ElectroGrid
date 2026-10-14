# ElectroGrid — how levels work

What a level is made of, how the board behaves, and the rules we follow when
making new ones. Read this before adding or changing a level.

## What a level is

A level is an `SOMatch3Level` asset in `Assets/3_Data/Levels`, named
`Level_N`. It holds:

- **Grid shape**: an `SOGridShape` in `Assets/2_Scripts/Grid`, named
  `Grid_<Shape>` (Grid_Heart, Grid_Bolt). It sets the width, height and which
  cells are on. One shape can be used by more than one level.
- **Match objects**: the pieces that can spawn, with a chance each. Every level
  so far uses the same four shapes (triangle, pentagon, circle, hexagon) at
  25 each.
- **Objectives**: what the player has to do to win. All of them must be done.
- **Lose conditions**: a move limit or a time limit, not both. Running out loses.
- **Tile objects**: which cells start with a Double Star or a Square Star
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
  or column. It also breaks Double Stars in that line.
- **Gravity** is straight down per column. Pieces fall through switched-off
  cells, so a column with a gap still refills (Hourglass and Hollow Square
  rely on this). New pieces fill whatever is empty after the fall.
- **Viruses, fuses and conveyors**

- Give viruses room to be reached: a virus walled in by Double Stars spreads
  with nothing you can do about it.
- Fuse Moves should leave time to set up a match, 7 or more for a first
  meeting. Put fuses where a match can reach them in a move or two.
- Keep Double Stars off conveyor rows unless the point is to split the row.

**Double Stars** (obstacles) don't move, can't be swapped, and break when a
  match happens next to them or a line break passes through them. One hit
  each. Pieces above a Double Star can't fall past it, so the cells under it
  only refill from new spawns.
- **Square Stars** fall like pieces and are collected when they reach the
  bottom row of their column. The catchers under the board open when a level
  has Square Stars.
- **Glitches** are a layer painted on cells (Glitch paint mode). A match on
  the cell or a line break through it clears it. The cell flickers cyan until
  then.
- **Viruses** sit still and break like Double Stars, but after every move in
  which no virus broke, one spreads onto a neighbouring piece. Fused pieces,
  Plus and Stars are safe from it.
- **Fuses** are painted on cells and land on whatever piece fills that cell.
  The number drops by one every move; matching the piece defuses it, and
  reaching 0 loses the level. The count is the level's Fuse Moves setting.
- **Conveyor rows** (Conveyor Rows list, row 0 is the bottom) move every
  piece one cell along after each move and wrap at the end. Double Stars and
  viruses stay put and the row moves past them. Small arrows mark the row.
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
| Destroy Obstacles | N Double Stars broken | N can't be more than the Double Stars placed. |
| Reach Bottom | N Square Stars collected | N can't be more than the Square Stars placed. |
| Reach Score | N points | Needs a lose condition and the Combo Bar box ticked (the validator warns about both). Double Stars (300) and Square Stars (500) add a lot, so count them in. First used by Level 21. |
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

**Square Stars**

- Never on the bottom row: they would score the moment the level starts.
- Only in columns whose lowest switched-on cell is on row 0, or they can
  never be collected. The validator checks both.
- A Double Star directly below a Square Star is allowed and worth using now
  and then: the player has to break it before the Square Star can fall.
  Levels 10, 12, 31, 40 and 50 do this.

**Double Stars**

- Leave at least 9 cells for pieces (the validator warns below that), and in
  practice far more. Most levels keep Double Stars to about a sixth of the
  board.
- Spread them so matches can reach each one. A Double Star surrounded by other
  Double Stars can only be broken by a line break.

**Difficulty**

- Each new level should be a little harder than the one before it, or teach
  something new. Introduce one new idea at a time: Level 6 brings Double Stars,
  Level 8 brings Square Stars, Level 10 is the first to mix them, then
  glitches at 18, viruses at 19, fuses at 24 and conveyors at 26.
- Moves so far run between 9 and 18. Use 14 to 18 for levels with two
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
| 6 | Circle 8x8 | 16 pieces, 4 Double Stars | 13 moves |
| 7 | Smile 10x10 | 16 pieces, 14 Double Stars | 9 moves |
| 8 | FullBlock 8x6 | 21 pieces, 2 Square Stars | 16 moves |
| 9 | NoCornerShortSquare 9x7 | 26 pieces, 4 Square Stars | 18 moves |
| 10 | TriangleSquashed 11x6 | 1 Square Stars, 9 Double Stars | 16 moves |
| 11 | Circle2 9x9 | 30 pieces, 9 Double Stars | 45s |
| 12 | ChestPlate 9x8 | 8 Double Stars, 1 Square Stars | 14 moves |
| 13 | Hourglass 9x9 | 24 pieces, 3 Square Stars | 16 moves |
| 14 | Bolt 9x10 | 26 pieces, 8 Double Stars | 16 moves |
| 15 | Crown 9x8 | 6 Double Stars, 3 Square Stars | 18 moves |
| 16 | Diamond 9x9 | 27 pieces, 4 Double Stars | 45s |
| 17 | Arrow 9x10 | 24 pieces, 3 Square Stars | 18 moves |
| 18 | Donut 9x9 | 18 glitches, 8 Double Stars | 16 moves |
| 19 | Steps 9x9 | all viruses, 3 Square Stars | 18 moves |
| 20 | Battery 8x10 | 8 Double Stars, 2 Square Stars | 20 moves |
| 21 | Cross 9x9 | 900 points | 45s |
| 22 | Castle 9x9 | 7 Double Stars, 2 Square Stars | 18 moves |
| 23 | Bowtie 11x7 | 32 pieces | 45s |
| 24 | Ladder 9x10 | 28 pieces, 3 fuses (9 moves each) | 18 moves |
| 25 | Chip 10x10 | 4000 points, 12 Double Stars | 22 moves |
| 26 | Star 9x9 | 30 matches, conveyors on rows 6 | 18 moves |
| 27 | V 11x7 | 30 pieces, all viruses | 18 moves |
| 28 | House 9x9 | 18 glitches, 2 Square Stars | 18 moves |
| 29 | Tree 9x10 | 1150 points | 45s |
| 30 | Moon 9x9 | 3 Double Stars, 3 fuses (9 moves each) | 17 moves |
| 31 | Eight 7x9 | 5 Double Stars (one under a Square Star), 2 Square Stars, conveyors on rows 7 and 5 | 18 moves |
| 32 | T 9x8 | 1600 points, 2 Square Stars | 20 moves |
| 33 | Arch 11x8 | 14 glitches | 45s |
| 34 | Hexagon 9x9 | 30 matches, all viruses | 20 moves |
| 35 | Shield 9x9 | 3 fuses (8 moves each), 2 Square Stars | 18 moves |
| 36 | Dumbbell 11x6 | 23 pieces, 5 Double Stars | 45s |
| 37 | Snake 9x10 | 1150 points | 45s |
| 38 | Pyramid 11x6 | 2 Square Stars, 5 Double Stars, conveyors on rows 2 | 18 moves |
| 39 | Butterfly 11x9 | 13 glitches, 19 matches | 45s |
| 40 | Fortress 10x10 | 11 Double Stars (one under a Square Star), 2 Square Stars | 22 moves |
| 41 | Boat 11x6 | 2000 points, 2 Square Stars | 20 moves |
| 42 | Gem 9x9 | 23 pieces, all viruses | 45s |
| 43 | Comb 11x8 | 30 pieces, 3 Square Stars, conveyors on rows 6 and 5 | 20 moves |
| 44 | Frame 10x10 | 5000 points, 12 Double Stars | 22 moves |
| 45 | Trophy 9x10 | 3 fuses (8 moves each), 25 matches | 20 moves |
| 46 | Clover 9x9 | 28 glitches, 5 Double Stars | 45s |
| 47 | Window 10x10 | 4000 points, 4 Double Stars, conveyors on rows 8 and 1 | 22 moves |
| 48 | Bridge 11x8 | 2 Square Stars, all viruses | 20 moves |
| 49 | Skull 9x10 | 4500 points, 3 fuses (8 moves each) | 24 moves |
| 50 | Circuit 11x10 | 10 Double Stars (one under a Square Star), 3 Square Stars, conveyors on rows 7 and 5 | 26 moves |
