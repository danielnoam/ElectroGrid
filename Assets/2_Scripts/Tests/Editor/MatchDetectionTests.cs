using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class MatchDetectionTests
{
    private Match3TestBoard _board;

    [TearDown]
    public void TearDown()
    {
        _board?.Dispose();
        _board = null;
    }

    private Match3TestBoard Board(params string[] rows) => _board = new Match3TestBoard(rows);

    private static List<Vector2Int> Positions(IEnumerable<Match3Tile> tiles) =>
        tiles.Select(t => t.GridPosition).OrderBy(p => p.x).ThenBy(p => p.y).ToList();

    private static List<Vector2Int> Cells(params (int x, int y)[] cells) =>
        cells.Select(c => new Vector2Int(c.x, c.y)).OrderBy(p => p.x).ThenBy(p => p.y).ToList();

    #region FindImmediateMatches

    [Test]
    public void Immediate_HorizontalThree_IsFound()
    {
        var board = Board(
            "ABC",
            "AAA");

        var matches = board.PlayHandler.FindImmediateMatches(board.GridShape);

        Assert.That(Positions(matches), Is.EqualTo(Cells((0, 0), (1, 0), (2, 0))));
    }

    [Test]
    public void Immediate_VerticalThree_IsFound()
    {
        var board = Board(
            "BA",
            "BC",
            "BA");

        var matches = board.PlayHandler.FindImmediateMatches(board.GridShape);

        Assert.That(Positions(matches), Is.EqualTo(Cells((0, 0), (0, 1), (0, 2))));
    }

    [Test]
    public void Immediate_TwoInARow_IsNotAMatch()
    {
        var board = Board(
            "AAB",
            "BCA");

        Assert.That(board.PlayHandler.FindImmediateMatches(board.GridShape), Is.Empty);
    }

    [Test]
    public void Immediate_LShape_ReturnsEachTileOnce()
    {
        var board = Board(
            "ABC",
            "ACB",
            "AAA");

        var matches = board.PlayHandler.FindImmediateMatches(board.GridShape);

        Assert.That(matches.Count, Is.EqualTo(5));
        Assert.That(Positions(matches), Is.EqualTo(Cells((0, 0), (1, 0), (2, 0), (0, 1), (0, 2))));
    }

    [Test]
    public void Immediate_FiveInARow_ReturnsAllFive()
    {
        var board = Board("AAAAA");

        Assert.That(board.PlayHandler.FindImmediateMatches(board.GridShape).Count, Is.EqualTo(5));
    }

    [TestCase("AA.A", TestName = "Immediate_RunBrokenByEmptyTile_IsNotAMatch")]
    [TestCase("AA#A", TestName = "Immediate_RunBrokenByMissingCell_IsNotAMatch")]
    [TestCase("AA*A", TestName = "Immediate_RunBrokenByObstacle_IsNotAMatch")]
    public void Immediate_BrokenRun_IsNotAMatch(string row)
    {
        var board = Board(row);

        Assert.That(board.PlayHandler.FindImmediateMatches(board.GridShape), Is.Empty);
    }

    [Test]
    public void Immediate_SeparateMatches_AreBothFound()
    {
        var board = Board(
            "BBB",
            "CAC",
            "AAA");

        Assert.That(board.PlayHandler.FindImmediateMatches(board.GridShape).Count, Is.EqualTo(6));
    }

    #endregion

    #region FindMatchesWithTile

    [Test]
    public void WithTile_MiddleOfRun_ReturnsWholeRun()
    {
        var board = Board("BAAAAB");

        var matches = board.PlayHandler.FindMatchesWithTile(board.Tile(2, 0), board.GridShape);

        Assert.That(Positions(matches), Is.EqualTo(Cells((1, 0), (2, 0), (3, 0), (4, 0))));
    }

    [Test]
    public void WithTile_Cross_ReturnsBothLines()
    {
        var board = Board(
            "BAB",
            "AAA",
            "BAB");

        var matches = board.PlayHandler.FindMatchesWithTile(board.Tile(1, 1), board.GridShape);

        Assert.That(matches.Count, Is.EqualTo(5));
    }

    [Test]
    public void WithTile_OnlyTwoMatching_ReturnsEmpty()
    {
        var board = Board(
            "AAB",
            "BCA");

        Assert.That(board.PlayHandler.FindMatchesWithTile(board.Tile(0, 1), board.GridShape), Is.Empty);
    }

    [Test]
    public void WithTile_TileWithoutMatchable_ReturnsEmpty()
    {
        var board = Board("*AA", ".BB");

        Assert.That(board.PlayHandler.FindMatchesWithTile(board.Tile(0, 1), board.GridShape), Is.Empty);
        Assert.That(board.PlayHandler.FindMatchesWithTile(board.Tile(0, 0), board.GridShape), Is.Empty);
        Assert.That(board.PlayHandler.FindMatchesWithTile(null, board.GridShape), Is.Empty);
    }

    [Test]
    public void WithTile_OtherRunInSameRow_IsNotIncluded()
    {
        var board = Board("AAABAAA");

        var matches = board.PlayHandler.FindMatchesWithTile(board.Tile(0, 0), board.GridShape);

        Assert.That(Positions(matches), Is.EqualTo(Cells((0, 0), (1, 0), (2, 0))));
    }

    #endregion

    #region WouldCreateMatch and FindPossibleMatches

    [Test]
    public void WouldCreateMatch_FillingAGap_IsTrue()
    {
        var board = Board("A.A");

        Assert.That(board.PlayHandler.WouldCreateMatch(new Vector2Int(1, 0), board.Item('A'), board.GridShape), Is.True);
        Assert.That(board.PlayHandler.WouldCreateMatch(new Vector2Int(1, 0), board.Item('B'), board.GridShape), Is.False);
    }

    [Test]
    public void WouldCreateMatch_Vertical_IsTrue()
    {
        var board = Board(
            ".",
            "A",
            "A");

        Assert.That(board.PlayHandler.WouldCreateMatch(new Vector2Int(0, 2), board.Item('A'), board.GridShape), Is.True);
    }

    [Test]
    public void PossibleMatches_OneSwapAway_ReturnsTheSwapPair()
    {
        // Swapping (2,0) with (2,1) lines up three As on the bottom row
        var board = Board(
            "BCA",
            "AAB");

        var possible = board.PlayHandler.FindPossibleMatches(board.GridShape);

        Assert.That(possible, Does.Contain(board.Tile(2, 0)));
        Assert.That(possible, Does.Contain(board.Tile(2, 1)));
    }

    [Test]
    public void PossibleMatches_NoSwapHelps_ReturnsEmpty()
    {
        var board = Board(
            "ABC",
            "DEF",
            "ABC");

        Assert.That(board.PlayHandler.FindPossibleMatches(board.GridShape), Is.Empty);
    }

    [Test]
    public void PossibleMatches_PieceIsNotCountedAtTheCellItLeaves()
    {
        // Swapping the top A down leaves B,A,A, not three As. This used to be reported as a move,
        // so a dead board could skip its reshuffle.
        var board = Board(
            "A",
            "B",
            "A");

        Assert.That(board.PlayHandler.FindPossibleMatches(board.GridShape), Is.Empty);
        Assert.That(board.PlayHandler.WouldCreateMatch(new Vector2Int(0, 1), board.Item('A'), board.GridShape, new Vector2Int(0, 2)), Is.False);
    }

    [Test]
    public void PossibleMatches_ObstacleCannotBeSwapped()
    {
        // The only swap that would complete the row is with the obstacle
        var board = Board(
            "BCA",
            "AA*");

        Assert.That(board.PlayHandler.FindPossibleMatches(board.GridShape), Is.Empty);
    }

    #endregion
}
