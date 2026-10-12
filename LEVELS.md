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
- **Lose conditions**: moves, time, or both. Running out of either loses.
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
- **Double Stars** (obstacles) don't move, can't be swapped, and break when a
  match happens next to them or a line break passes through them. One hit
  each. Pieces above a Double Star can't fall past it, so the cells under it
  only refill from new spawns.
- **Square Stars** fall like pieces and are collected when they reach the
  bottom row of their column. The catchers under the board open when a level
  has Square Stars.
- **Plus** pieces spawn now and then (5% per new piece). Using one gives
  extra moves or time.
- **Combo**: matches in quick succession fill the combo bar. A full bar adds
  5 seconds to a time limit or 2 moves to a move limit, but never above the
  level's starting amount.
- The board is always generated with at least 3 possible moves, and it is
  reshuffled if the player runs out of moves.

## Objectives

| Objective | Asks for | Notes |
|---|---|---|
| Get Matches | N pieces cleared | Counts every piece, including line breaks. |
| Destroy Obstacles | N Double Stars broken | N can't be more than the Double Stars placed. |
| Reach Bottom | N Square Stars collected | N can't be more than the Square Stars placed. |
| Get Specific Item Matches | N of one piece | Exists in code, not used by any level yet. |

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
- Avoid a Double Star directly below one unless the point is to break it
  first. The validator warns about this.

**Double Stars**

- Leave at least 9 cells for pieces (the validator warns below that), and in
  practice far more. Most levels keep Double Stars to about a sixth of the
  board.
- Spread them so matches can reach each one. A Double Star surrounded by other
  Double Stars can only be broken by a line break.

**Difficulty**

- Each new level should be a little harder than the one before it, or teach
  something new. Introduce one new idea at a time: Level 6 brings Double Stars,
  Level 8 brings Square Stars, Level 10 is the first to mix them.
- Moves so far run between 9 and 18. Use 14 to 18 for levels with two
  objectives. Fewer cells or more stars means more moves.
- Mix in a time limit now and then (Levels 4 and 11 use 45 seconds on top of
  moves).
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
| 4 | Plus 9x9 | 29 pieces | 10 moves, 45s |
| 5 | H 8x8 | 46 pieces | 13 moves |
| 6 | Circle 8x8 | 16 pieces, 4 Double Stars | 13 moves |
| 7 | Smile 10x10 | 16 pieces, 14 Double Stars | 9 moves |
| 8 | FullBlock 8x6 | 21 pieces, 2 Square Stars | 16 moves |
| 9 | NoCornerShortSquare 9x7 | 26 pieces, 4 Square Stars | 18 moves |
| 10 | TriangleSquashed 11x6 | 1 Square Star, 9 Double Stars | 16 moves |
| 11 | Circle2 9x9 | 30 pieces, 9 Double Stars | 16 moves, 45s |
| 12 | ChestPlate 9x8 | 8 Double Stars, 1 Square Star | 14 moves |
| 13 | Hourglass 9x9 | 24 pieces, 3 Square Stars | 16 moves |
| 14 | Bolt 9x10 | 26 pieces, 8 Double Stars | 16 moves |
| 15 | Crown 9x8 | 6 Double Stars, 3 Square Stars | 18 moves |
| 16 | Diamond 9x9 | 30 pieces, 4 Double Stars | 15 moves, 50s |
| 17 | Arrow 9x10 | 24 pieces, 3 Square Stars | 18 moves |
| 18 | Donut 9x9 | 24 pieces, 8 Double Stars | 16 moves |
| 19 | Steps 9x9 | 4 Double Stars, 3 Square Stars | 18 moves |
| 20 | Battery 8x10 | 8 Double Stars, 2 Square Stars | 20 moves |
