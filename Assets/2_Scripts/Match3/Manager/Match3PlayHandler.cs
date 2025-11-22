using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DNExtensions;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class Match3PlayHandler : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Duration taken to populate grid")]
    [SerializeField, Min(0)] private float populationDuration = 1f;
    [Tooltip("Duration taken to swap two objects")]
    [SerializeField, Min(0)] private float objectSwapDuration = 0.3f;
    [Tooltip("Delay between popping matched objects")]
    [SerializeField, Min(0)] private float delayBetweenMatchPops = 0.05f;
    [Tooltip("Delay between moving objects downwards")]
    [SerializeField, Min(0)] private float delayBetweenObjectMovement = 0.15f;
    
    [Header("References")]
    [SerializeField] private Match3GameManager gameManager;
    [SerializeField] private Match3GridHandler gridHandler;
    [SerializeField] private Match3SelectionIndicator selectionIndicator;


    [Separator]
    [SerializeField, ReadOnly] private bool canInteract;
    [SerializeField, ReadOnly] private bool isPaused;
    [SerializeField, ReadOnly] private Match3Tile selectedMatch3Tile;
    [SerializeField, ReadOnly] private Match3Object heldMatch3Object;
    private Camera _camera;
    private TouchInputReader _inputReader;

    public bool CanInteract
    {
        get => canInteract;
        set => canInteract = value;
    }

    private void Awake()
    {
        _camera = Camera.main;
    }

    private void Start()
    {
        if (!_inputReader)
        {
            _inputReader = TouchInputReader.Instance;
        }
        
        if (_inputReader)
        {
            _inputReader.OnSelect -= OnSelect;
            _inputReader.OnSwipe -= OnSwipe;
            _inputReader.OnSelect += OnSelect;
            _inputReader.OnSwipe += OnSwipe;
        }

        if (GameManager.Instance)
        {
            GameManager.Instance.PauseToggled -= OnPauseToggled;
            GameManager.Instance.PauseToggled += OnPauseToggled;
        }
    }

    private void OnEnable()
    {
        if (!_inputReader)
        {
            _inputReader = TouchInputReader.Instance;
        }
        
        if (_inputReader)
        {
            _inputReader.OnSelect -= OnSelect;
            _inputReader.OnSwipe -= OnSwipe;
            _inputReader.OnSelect += OnSelect;
            _inputReader.OnSwipe += OnSwipe;
        }
        
        if (GameManager.Instance)
        {
            GameManager.Instance.PauseToggled -= OnPauseToggled;
            GameManager.Instance.PauseToggled += OnPauseToggled;
        }
    }
    
    private void OnDisable()
    {
        _inputReader.OnSelect -= OnSelect;
        _inputReader.OnSwipe -= OnSwipe;
        
        if (GameManager.Instance)
        {
            GameManager.Instance.PauseToggled -= OnPauseToggled;
        }
    }

    private void OnPauseToggled(bool paused)
    {
        isPaused = paused;
    }


    #region Selection & Swapping
    
    private void OnSwipe(Vector2 direction)
    {
        if (!canInteract || !heldMatch3Object || isPaused) return;

        Vector2Int gridDirection;

        float absX = Mathf.Abs(direction.x);
        float absY = Mathf.Abs(direction.y);

        if (absX > absY)
        {
            if (absY > absX * _inputReader.SwipeDeadzone)
            {
                ReleaseObject(true);
                return;
            }
            gridDirection = direction.x > 0 ? Vector2Int.right : Vector2Int.left;
        }
        else
        {
            if (absX > absY * _inputReader.SwipeDeadzone)
            {
                ReleaseObject(true);
                return;
            }
            gridDirection = direction.y > 0 ? Vector2Int.up : Vector2Int.down;
        }

        var newTilePos = selectedMatch3Tile.GridPosition + gridDirection;
        var newTile = gridHandler.GetTile(newTilePos);

        TrySwapHeldObject(newTile);
    }

    private void OnSelect(InputAction.CallbackContext callbackContext)
    {
        if (!canInteract || isPaused) return;

        if (!heldMatch3Object && callbackContext.started)
        {
            Vector3 worldPos = _camera.ScreenToWorldPoint(_inputReader.MousePosition);
            worldPos.z = 0;
    
            var gridPos = gridHandler.GridShape.Grid.GetCell(worldPos);
            var tile = gridHandler.GetTile(gridPos);
        
            SelectObjectInTile(tile);
        }            
        else if (heldMatch3Object && callbackContext.canceled)
        {
            ReleaseObject(true);
        }
    }

    private void SelectObjectInTile(Match3Tile match3Tile)
    {
        if (!gridHandler.CanSelectTile(match3Tile)) return;
        
        selectedMatch3Tile = match3Tile;
        heldMatch3Object = selectedMatch3Tile.CurrentMatch3Object;
        
        if (heldMatch3Object && heldMatch3Object.IsSwappable)
        {
            heldMatch3Object.SetHeld(true);
        }
        
        selectionIndicator?.EnableIndicator(selectedMatch3Tile);
    }
    
    private void ReleaseObject(bool animateReturn)
    {
        selectedMatch3Tile?.SetSelected(false);
        selectedMatch3Tile = null;
        
        if (heldMatch3Object && heldMatch3Object.IsSwappable)
        {
            heldMatch3Object.SetHeld(false);
        }
        
        heldMatch3Object = null;
        selectionIndicator?.DisableIndicator(animateReturn);
    }

    private void TrySwapHeldObject(Match3Tile match3Tile)
    {
        if (!gridHandler.CanSelectTile(match3Tile))
        {
            ReleaseObject(true);
            return;
        } 

        if (match3Tile == selectedMatch3Tile)
        {
            ReleaseObject(false);
            return;
        }

        if (gridHandler.Grid.AreCellsNeighbors(match3Tile.GridPosition, selectedMatch3Tile.GridPosition))
        {
            gameManager.StartCoroutine(gameManager.RunGameLogic(match3Tile.GridPosition, selectedMatch3Tile.GridPosition));
        }
        else
        {
            ReleaseObject(true);
        }
    }

    public IEnumerator SwapObjects(Vector2Int posA, Vector2Int posB)
    {
        var tileA = gridHandler.GetTile(posA);
        var tileB = gridHandler.GetTile(posB);
        
        if (!gridHandler.IsValidTile(tileA) || !gridHandler.IsValidTile(tileB)) 
        {
            yield break;
        }
        
        var itemA = tileA.CurrentMatch3Object;
        var itemB = tileB.CurrentMatch3Object;
        
        tileA.SetCurrentItem(itemB);
        tileB.SetCurrentItem(itemA);
        
        itemA.SetCurrentTile(tileB);
        itemB.SetCurrentTile(tileA);

        ReleaseObject(false);
        yield return new WaitForSeconds(objectSwapDuration);
    }
    

    #endregion

    
    #region Match Detection

    public List<Match3Tile> FindImmediateMatches(SOGridShape gridShape)
    {
        HashSet<Match3Tile> matches = new HashSet<Match3Tile>();

        for (var y = 0; y < gridShape.Grid.Height; y++)
        {
            for (var x = 0; x < gridShape.Grid.Width - 2; x++)
            {
                var tile1 = gridHandler.GetTile(new Vector2Int(x, y));
                var tile2 = gridHandler.GetTile(new Vector2Int(x + 1, y));
                var tile3 = gridHandler.GetTile(new Vector2Int(x + 2, y));

                if (!gridHandler.CanSelectTile(tile1) || !gridHandler.CanSelectTile(tile2) || !gridHandler.CanSelectTile(tile3)) continue;

                var matchable1 = tile1.CurrentMatch3Object as Match3MatchableObject;
                var matchable2 = tile2.CurrentMatch3Object as Match3MatchableObject;
                var matchable3 = tile3.CurrentMatch3Object as Match3MatchableObject;

                if (matchable1 && matchable2 && matchable3 &&
                    matchable1.ItemData == matchable2.ItemData && 
                    matchable2.ItemData == matchable3.ItemData)
                {
                    matches.Add(tile1);
                    matches.Add(tile2);
                    matches.Add(tile3);
                }
            }
        }

        for (var x = 0; x < gridShape.Grid.Width; x++)
        {
            for (var y = 0; y < gridShape.Grid.Height - 2; y++)
            {
                var tile1 = gridHandler.GetTile(new Vector2Int(x, y));
                var tile2 = gridHandler.GetTile(new Vector2Int(x, y + 1));
                var tile3 = gridHandler.GetTile(new Vector2Int(x, y + 2));
                
                if (!gridHandler.CanSelectTile(tile1) || !gridHandler.CanSelectTile(tile2) || !gridHandler.CanSelectTile(tile3)) continue;
                
                var matchable1 = tile1.CurrentMatch3Object as Match3MatchableObject;
                var matchable2 = tile2.CurrentMatch3Object as Match3MatchableObject;
                var matchable3 = tile3.CurrentMatch3Object as Match3MatchableObject;

                if (matchable1 && matchable2 && matchable3 &&
                    matchable1.ItemData == matchable2.ItemData && 
                    matchable2.ItemData == matchable3.ItemData)
                {
                    matches.Add(tile1);
                    matches.Add(tile2);
                    matches.Add(tile3);
                }
            }
        }
        
        return new List<Match3Tile>(matches);
    }

    public List<Match3Tile> FindMatchesWithTile(Match3Tile match3Tile, SOGridShape gridShape)
    {
        if (!match3Tile || !match3Tile.HasObject || !(match3Tile.CurrentMatch3Object is Match3MatchableObject matchable)) 
            return new List<Match3Tile>();
        
        HashSet<Match3Tile> matches = new HashSet<Match3Tile>();
        Vector2Int pos = match3Tile.GridPosition;
        SOItemData itemData = matchable.ItemData;
        
        List<Match3Tile> horizontalMatches = new List<Match3Tile> { match3Tile };
        for (int x = pos.x - 1; x >= 0; x--)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(x, pos.y));
            if (!gridHandler.IsValidTile(checkTile) || !checkTile.HasObject || 
                !(checkTile.CurrentMatch3Object is Match3MatchableObject checkMatchable) ||
                checkMatchable.ItemData != itemData)
                break;
            horizontalMatches.Add(checkTile);
        }
        for (int x = pos.x + 1; x < gridShape.Grid.Width; x++)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(x, pos.y));
            if (!gridHandler.IsValidTile(checkTile) || !checkTile.HasObject || 
                !(checkTile.CurrentMatch3Object is Match3MatchableObject checkMatchable) ||
                checkMatchable.ItemData != itemData)
                break;
            horizontalMatches.Add(checkTile);
        }
        
        if (horizontalMatches.Count >= gameManager.MinMatchCount)
        {
            foreach (var match in horizontalMatches)
                matches.Add(match);
        }
        
        List<Match3Tile> verticalMatches = new List<Match3Tile> { match3Tile };
        for (int y = pos.y - 1; y >= 0; y--)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(pos.x, y));
            if (!gridHandler.IsValidTile(checkTile) || !checkTile.HasObject || 
                !(checkTile.CurrentMatch3Object is Match3MatchableObject checkMatchable) ||
                checkMatchable.ItemData != itemData)
                break;
            verticalMatches.Add(checkTile);
        }
        for (int y = pos.y + 1; y < gridShape.Grid.Height; y++)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(pos.x, y));
            if (!gridHandler.IsValidTile(checkTile) || !checkTile.HasObject || 
                !(checkTile.CurrentMatch3Object is Match3MatchableObject checkMatchable) ||
                checkMatchable.ItemData != itemData)
                break;
            verticalMatches.Add(checkTile);
        }
        
        if (verticalMatches.Count >= gameManager.MinMatchCount)
        {
            foreach (var match in verticalMatches)
                matches.Add(match);
        }
        
        return new List<Match3Tile>(matches);
    }

    public List<Match3Tile> FindPossibleMatches(SOGridShape gridShape)
    {
        HashSet<Match3Tile> possibleMatchTiles = new HashSet<Match3Tile>();
        HashSet<(Vector2Int, Vector2Int)> checkedPairs = new HashSet<(Vector2Int, Vector2Int)>();

        foreach (var tile in gridHandler.Tiles.Values)
        {
            if (!gridHandler.IsValidTile(tile) || !tile.HasObject || !(tile.CurrentMatch3Object is Match3MatchableObject)) continue;
    
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
    
            foreach (var direction in directions)
            {
                var neighborPos = tile.GridPosition + direction;
                var neighborTile = gridHandler.GetTile(neighborPos);
        
                if (!gridHandler.IsValidTile(neighborTile) || !neighborTile.HasObject || !(neighborTile.CurrentMatch3Object is Match3MatchableObject)) continue;
        
                var pair = tile.GridPosition.x < neighborPos.x || (tile.GridPosition.x == neighborPos.x && tile.GridPosition.y < neighborPos.y)
                    ? (tile.GridPosition, neighborPos)
                    : (neighborPos, tile.GridPosition);
            
                if (!checkedPairs.Add(pair)) continue;

                var currentMatchable = tile.CurrentMatch3Object as Match3MatchableObject;
                var neighborMatchable = neighborTile.CurrentMatch3Object as Match3MatchableObject;
        
                if (WouldCreateMatch(neighborPos, currentMatchable.ItemData, gridShape) || 
                    WouldCreateMatch(tile.GridPosition, neighborMatchable.ItemData, gridShape))
                {
                    possibleMatchTiles.Add(tile);
                    possibleMatchTiles.Add(neighborTile);
                }
            }
        }

        return new List<Match3Tile>(possibleMatchTiles);
    }

    public bool WouldCreateMatch(Vector2Int position, SOItemData itemData, SOGridShape gridShape)
    {
        int horizontalCount = 1;
        for (int x = position.x - 1; x >= 0; x--)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(x, position.y));
            if (!gridHandler.IsValidTile(checkTile) || !checkTile.HasObject || 
                !(checkTile.CurrentMatch3Object is Match3MatchableObject checkMatchable) ||
                checkMatchable.ItemData != itemData)
                break;
            horizontalCount++;
        }
        for (int x = position.x + 1; x < gridShape.Grid.Width; x++)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(x, position.y));
            if (!gridHandler.IsValidTile(checkTile) || !checkTile.HasObject || 
                !(checkTile.CurrentMatch3Object is Match3MatchableObject checkMatchable) ||
                checkMatchable.ItemData != itemData)
                break;
            horizontalCount++;
        }
        
        if (horizontalCount >= gameManager.MinMatchCount) return true;
        
        int verticalCount = 1;
        for (int y = position.y - 1; y >= 0; y--)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(position.x, y));
            if (!gridHandler.IsValidTile(checkTile) || !checkTile.HasObject || 
                !(checkTile.CurrentMatch3Object is Match3MatchableObject checkMatchable) ||
                checkMatchable.ItemData != itemData)
                break;
            verticalCount++;
        }
        for (int y = position.y + 1; y < gridShape.Grid.Height; y++)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(position.x, y));
            if (!gridHandler.IsValidTile(checkTile) || !checkTile.HasObject || 
                !(checkTile.CurrentMatch3Object is Match3MatchableObject checkMatchable) ||
                checkMatchable.ItemData != itemData)
                break;
            verticalCount++;
        }
        
        return verticalCount >= gameManager.MinMatchCount;
    }

    public bool WouldCreateMatchDuringPopulation(Vector2Int position, SOItemData itemData, 
        Dictionary<Match3Tile, SOItemData> tilesToPopulate, SOGridShape gridShape)
    {
        int horizontalCount = 1;
        for (int x = position.x - 1; x >= 0; x--)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(x, position.y));
            if (!gridHandler.IsValidTile(checkTile)) break;
            
            SOItemData checkItemData = null;
            if (checkTile.HasObject && checkTile.CurrentMatch3Object is Match3MatchableObject matchable)
                checkItemData = matchable.ItemData;
            else if (tilesToPopulate.TryGetValue(checkTile, out var value))
                checkItemData = value;
            
            if (checkItemData == null || checkItemData != itemData)
                break;
                
            horizontalCount++;
        }
        for (int x = position.x + 1; x < gridShape.Grid.Width; x++)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(x, position.y));
            if (!gridHandler.IsValidTile(checkTile)) break;
            
            SOItemData checkItemData = null;
            if (checkTile.HasObject && checkTile.CurrentMatch3Object is Match3MatchableObject matchable)
                checkItemData = matchable.ItemData;
            else if (tilesToPopulate.TryGetValue(checkTile, out var value))
                checkItemData = value;
            
            if (checkItemData == null || checkItemData != itemData)
                break;
                
            horizontalCount++;
        }
        
        if (horizontalCount >= gameManager.MinMatchCount) return true;
        
        int verticalCount = 1;
        for (int y = position.y - 1; y >= 0; y--)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(position.x, y));
            if (!gridHandler.IsValidTile(checkTile)) break;
            
            SOItemData checkItemData = null;
            if (checkTile.HasObject && checkTile.CurrentMatch3Object is Match3MatchableObject matchable)
                checkItemData = matchable.ItemData;
            else if (tilesToPopulate.TryGetValue(checkTile, out var value))
                checkItemData = value;
            
            if (checkItemData == null || checkItemData != itemData)
                break;
                
            verticalCount++;
        }
        for (int y = position.y + 1; y < gridShape.Grid.Height; y++)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(position.x, y));
            if (!gridHandler.IsValidTile(checkTile)) break;
            
            SOItemData checkItemData = null;
            if (checkTile.HasObject && checkTile.CurrentMatch3Object is Match3MatchableObject matchable)
                checkItemData = matchable.ItemData;
            else if (tilesToPopulate.TryGetValue(checkTile, out var value))
                checkItemData = value;
            
            if (checkItemData == null || checkItemData != itemData)
                break;
                
            verticalCount++;
        }
        
        return verticalCount >= gameManager.MinMatchCount;
    }

    #endregion

    
    #region Match Handling

    
    public IEnumerator HandleMatchesAndRepopulate(SOMatch3Level level, SOGridShape gridShape, int minPossibleMatches)
    {
        while (true)
        {
            var immediateMatches = FindImmediateMatches(gridShape);
        
            if (immediateMatches.Count == 0)
            {
                break;
            }
        
            gameManager.NotifyMatchesWhereMade(immediateMatches);
            
            yield return HandleMatches(immediateMatches);
            yield return MoveObjectsDown(gridShape);
            yield return PopulateGrid(level, gridShape, minPossibleMatches, false);
        }
    }

    public IEnumerator HandleMatches(List<Match3Tile> tilesWithMatches)
    {
        foreach (var tile in tilesWithMatches)
        {
            if (tile && tile.CurrentMatch3Object is Match3MatchableObject matchable)
            {
                matchable.MatchFound();
                yield return new WaitForSeconds(delayBetweenMatchPops);
            }
        }


        // Check if the match amount is more than the minimum amount for a match
        if (tilesWithMatches.Count >= gameManager.MinMatchForLineBreak)
        {
            // Check if the matches form a significant vertical line (multiple tiles share the same x)
            var xGroups = tilesWithMatches.GroupBy(tile => tile.GridPosition.x);
            var enumerable = xGroups.ToList();
            bool isVertical = enumerable.Any(group => group.Count() >= gameManager.MinMatchForLineBreak);
            
            // Check if the matches form a significant horizontal line (multiple tiles share the same y)
            var yGroups = tilesWithMatches.GroupBy(tile => tile.GridPosition.y);
            var groups = yGroups.ToList();
            bool isHorizontal = groups.Any(group => group.Count() >= gameManager.MinMatchForLineBreak);
            
            if (isHorizontal || isVertical)
            {
                yield return new WaitForSeconds(0.3f);
                CameraManager.Instance?.ShakeCamera(3, 0.75f);
            }
            
            // If vertical, destroy all columns that have more than MinMatchForSpecial matches
            if (isVertical)
            {
                var columnsToDestroy = enumerable.Where(group => group.Count() >= gameManager.MinMatchForLineBreak).Select(group => group.Key);
                foreach (var column in columnsToDestroy)
                {
                    for (var y = 0; y < gridHandler.Grid.Height; y++)
                    {
                        var tile = gridHandler.GetTile(new Vector2Int(column, y));
                        if (!tile || !tile.CurrentMatch3Object) continue;
                        
                        if (tile.CurrentMatch3Object.IsAffectedBySpecialMatches)
                        {
                            tile.CurrentMatch3Object.DestroyWithAnimation();
                            tile.PunchTile(1.5f);
                            tile.SetCurrentItem(null);
                        } 
                    }
                }
            }

            // If horizontal, destroy all rows that have more than MinMatchForSpecial matches
            if (isHorizontal)
            {
                var rowsToDestroy = groups.Where(group => group.Count() >= gameManager.MinMatchForLineBreak).Select(group => group.Key);
                foreach (var row in rowsToDestroy)
                {
                    for (var x = 0; x < gridHandler.Grid.Width; x++)
                    {
                        var tile = gridHandler.GetTile(new Vector2Int(x, row));
                        if (!tile || !tile.CurrentMatch3Object) continue;
                        
                        if (tile.CurrentMatch3Object.IsAffectedBySpecialMatches)
                        {
                            tile.CurrentMatch3Object.DestroyWithAnimation();
                            tile.PunchTile(1.5f);
                            tile.SetCurrentItem(null);
                        } 
                    }
                }
            }

            if (isHorizontal || isVertical)
            {
                yield return new WaitForSeconds(0.3f);
            }
        }
    }


    public IEnumerator MoveObjectsDown(SOGridShape gridShape)
    {
        bool objectsMoved;
        
        do
        {
            objectsMoved = false;
            List<(Match3Object obj, Match3Tile fromTile, Match3Tile toTile)> movesThisWave = new List<(Match3Object, Match3Tile, Match3Tile)>();
            HashSet<Match3Tile> tilesAlreadyMoving = new HashSet<Match3Tile>();
            HashSet<Match3Tile> tilesAlreadyReceiving = new HashSet<Match3Tile>();
            
            for (var y = 0; y < gridShape.Grid.Height; y++)
            {
                for (var x = 0; x < gridShape.Grid.Width; x++)
                {
                    var tile = gridHandler.GetTile(new Vector2Int(x, y));
                    
                    if (!tile || tile.HasObject || !tile.IsActive || tilesAlreadyReceiving.Contains(tile))
                        continue;
                    
                    for (var i = y + 1; i < gridShape.Grid.Height; i++)
                    {
                        var aboveTile = gridHandler.GetTile(new Vector2Int(x, i));
                        
                        if (aboveTile && aboveTile.HasObject)
                        {
                            if (!aboveTile.CurrentMatch3Object.IsMovable)
                            {
                                break;
                            }
                            
                            if (!tilesAlreadyMoving.Contains(aboveTile))
                            {
                                movesThisWave.Add((aboveTile.CurrentMatch3Object, aboveTile, tile));
                                tilesAlreadyMoving.Add(aboveTile);
                                tilesAlreadyReceiving.Add(tile);
                                objectsMoved = true;
                                break;
                            }
                        }
                    }
                }
            }
            
            foreach (var move in movesThisWave)
            {
                move.fromTile.SetCurrentItem(null);
                move.toTile.SetCurrentItem(move.obj);
                move.obj.SetCurrentTile(move.toTile);
            }
            
            if (objectsMoved)
            {
                yield return new WaitForSeconds(delayBetweenObjectMovement);
            }
            
        } while (objectsMoved);
    }
    
    public IEnumerator ClearObjects()
    {
        var tiles = gridHandler.Tiles.Values.Where(t => t.HasObject).ToList();
        
        foreach (var tileObjectMatch in tiles)
        {
            CameraManager.Instance?.ShakeCamera(1, 0.25f);
            tileObjectMatch.CurrentMatch3Object.DestroyWithAnimation();
            yield return new WaitForSeconds(populationDuration / tiles.Count);
        }
    }


    #endregion
    
    
    #region Population

    public Dictionary<Match3Tile, SOItemData> GenerateGridLayout(SOMatch3Level level, SOGridShape gridShape, int minPossibleMatches)
    {
        if (!level)
        {
            Debug.LogError("Level is not set, Cannot generate grid layout");
            return null;
        }

        int totalActiveTiles = gridHandler.GetActiveTileCount();
        
        if (totalActiveTiles == 0)
        {
            return null;
        }

        int maxX = gridHandler.Grid.Width - 1;
        int maxY = gridHandler.Grid.Height - 1;
        
        var tilesToPopulate = new Dictionary<Match3Tile, SOItemData>();
        for (int y = 0; y <= maxY; y++)
        {
            for (int x = 0; x <= maxX; x++)
            {
                var tile = gridHandler.GetTile(new Vector2Int(x, y));
                if (tile && !tile.HasObject)
                {
                    tilesToPopulate.Add(tile, null);
                }
            }
        }
        
        if (tilesToPopulate.Count == 0)
        {
            return tilesToPopulate;
        }
        
        List<(Vector2Int posA, Vector2Int posB)> guaranteedMatchPairs = CreateGuaranteedMatchPositions(minPossibleMatches);
        
        foreach (var tileItemPair in tilesToPopulate.ToList())
        {
            SOItemData forcedItem = GetForcedItemForGuaranteedMatch(
                tileItemPair.Key.GridPosition, guaranteedMatchPairs, tilesToPopulate, level);
            
            if (forcedItem)
            {
                tilesToPopulate[tileItemPair.Key] = forcedItem;
            }
            else
            {
                SOItemData selectedItem;
                int attempts = 0;

                do
                {
                    selectedItem = level.MatchObjects.GetRandomItem();
                    attempts++;
            
                    if (attempts >= gameManager.MaxAttemptsToRecheckMatches)
                    {
                        Debug.Log("Could not find non-matching item");
                        break;
                    }
                } 
                while (WouldCreateMatchDuringPopulation(tileItemPair.Key.GridPosition, selectedItem, tilesToPopulate, gridShape));

                tilesToPopulate[tileItemPair.Key] = selectedItem;
            }
        }

        return tilesToPopulate;
    }

    public (bool isValid, int immediateMatches, int possibleMatches) ValidateGridLayout(Dictionary<Match3Tile, SOItemData> layout, SOGridShape gridShape, int minPossibleMatches, bool checkImmediateMatches = true)
    {
        int immediateMatchCount = 0;
        if (checkImmediateMatches)
        {
            immediateMatchCount = FindImmediateMatchesInLayout(layout, gridShape);
        }

        int possibleMatchCount = FindPossibleMatchesInLayout(layout, gridShape);

        bool isValid = (!checkImmediateMatches || immediateMatchCount == 0) && possibleMatchCount >= minPossibleMatches;
    
        return (isValid, immediateMatchCount, possibleMatchCount);
    }

    public IEnumerator SpawnGridLayout(Dictionary<Match3Tile, SOItemData> layout, bool isInitialSetup)
    {
        ReleaseObject(false);

        int totalActiveTiles = layout.Count;
    
        if (totalActiveTiles == 0)
        {
            yield break;
        }

        if (isInitialSetup)
        {
            foreach (var tileObjectMatch in layout)
            {
                CameraManager.Instance?.ShakeCamera(1, 0.25f);
                MobileHaptics.Vibrate(50);
                gridHandler.CreateMatchableObject(tileObjectMatch.Value, tileObjectMatch.Key);
                yield return new WaitForSeconds(populationDuration / totalActiveTiles);
            }
        }
        else
        {

            var spawnedHelperAlready = false;
            
            var tilesByRow = layout
                .GroupBy(kvp => kvp.Key.GridPosition.y)
                .OrderBy(g => g.Key);
    
            int totalRows = tilesByRow.Count();
    
            foreach (var row in tilesByRow)
            {
                foreach (var tileObjectMatch in row)
                {
                    if (ShouldSpawnHelperObject() && !spawnedHelperAlready)
                    {
                        gridHandler.CreateHelperObject(tileObjectMatch.Key);
                        spawnedHelperAlready = true;
                    }
                    else
                    {
                        gridHandler.CreateMatchableObject(tileObjectMatch.Value, tileObjectMatch.Key);
                    }
                }
        
                yield return new WaitForSeconds(populationDuration / totalRows);
            }
        }
    }
    

    public IEnumerator PopulateGrid(SOMatch3Level level, SOGridShape gridShape, int minPossibleMatches, bool isInitialSetup)
    {
        var layout = GenerateGridLayout(level, gridShape, minPossibleMatches);
        if (layout == null || layout.Count == 0)
        {
            canInteract = false;
            yield break;
        }

        yield return SpawnGridLayout(layout, isInitialSetup);
    }

    private List<(Vector2Int posA, Vector2Int posB)> CreateGuaranteedMatchPositions(int count)
    {
        List<(Vector2Int, Vector2Int)> pairs = new List<(Vector2Int, Vector2Int)>();
        List<Vector2Int> usedPositions = new List<Vector2Int>();
        
        int attempts = 0;
        while (pairs.Count < count && attempts < gameManager.MaxGuaranteedMatchAttempts)
        {
            attempts++;
            
            var randomTile = gridHandler.GetRandomValidTile();
            if (!randomTile) break;
            
            if (usedPositions.Contains(randomTile.GridPosition)) continue;
            
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var dir in directions.OrderBy(x => Random.value))
            {
                var neighborPos = randomTile.GridPosition + dir;
                var neighborTile = gridHandler.GetTile(neighborPos);
                
                if (!gridHandler.IsValidTile(neighborTile) || usedPositions.Contains(neighborPos)) continue;
                
                pairs.Add((randomTile.GridPosition, neighborPos));
                usedPositions.Add(randomTile.GridPosition);
                usedPositions.Add(neighborPos);
                break;
            }
        }
        
        return pairs;
    }

    private SOItemData GetForcedItemForGuaranteedMatch(Vector2Int position, 
        List<(Vector2Int posA, Vector2Int posB)> guaranteedPairs, 
        Dictionary<Match3Tile, SOItemData> tilesToPopulate, 
        SOMatch3Level level)
    {
        foreach (var pair in guaranteedPairs)
        {
            if (pair.posA == position)
            {
                var neighborPos = pair.posB;
                Vector2Int direction = neighborPos - position;
                Vector2Int thirdPos = neighborPos + direction;
                var thirdTile = gridHandler.GetTile(thirdPos);
                
                if (gridHandler.IsValidTile(thirdTile))
                {
                    var itemData = level.MatchObjects.GetRandomItem();
                    
                    if (tilesToPopulate.ContainsKey(thirdTile))
                    {
                        tilesToPopulate[thirdTile] = itemData;
                    }
                    
                    return itemData;
                }
            }
            else if (pair.posB == position)
            {
                var tileA = gridHandler.GetTile(pair.posA);
                if (tilesToPopulate.ContainsKey(tileA) && tilesToPopulate[tileA])
                {
                    SOItemData differentItem;
                    do
                    {
                        differentItem = level.MatchObjects.GetRandomItem();
                    } while (differentItem == tilesToPopulate[tileA] && level.MatchObjects.Count > 1);
                    
                    return differentItem;
                }
            }
        }
        
        return null;
    }
    
    private int FindImmediateMatchesInLayout(Dictionary<Match3Tile, SOItemData> layout, SOGridShape gridShape)
    {
        HashSet<Match3Tile> matches = new HashSet<Match3Tile>();

        for (var y = 0; y < gridShape.Grid.Height; y++)
        {
            for (var x = 0; x < gridShape.Grid.Width - 2; x++)
            {
                var tile1 = gridHandler.GetTile(new Vector2Int(x, y));
                var tile2 = gridHandler.GetTile(new Vector2Int(x + 1, y));
                var tile3 = gridHandler.GetTile(new Vector2Int(x + 2, y));

                if (!tile1 || !tile2 || !tile3) continue;
                if (!layout.TryGetValue(tile1, out var item1)) continue;
                if (!layout.TryGetValue(tile2, out var item2)) continue;
                if (!layout.TryGetValue(tile3, out var item3)) continue;

                if (item1 == item2 && item2 == item3)
                {
                    matches.Add(tile1);
                    matches.Add(tile2);
                    matches.Add(tile3);
                }
            }
        }

        for (var x = 0; x < gridShape.Grid.Width; x++)
        {
            for (var y = 0; y < gridShape.Grid.Height - 2; y++)
            {
                var tile1 = gridHandler.GetTile(new Vector2Int(x, y));
                var tile2 = gridHandler.GetTile(new Vector2Int(x, y + 1));
                var tile3 = gridHandler.GetTile(new Vector2Int(x, y + 2));
                
                if (!tile1 || !tile2 || !tile3) continue;
                if (!layout.TryGetValue(tile1, out var item1)) continue;
                if (!layout.TryGetValue(tile2, out var item2)) continue;
                if (!layout.TryGetValue(tile3, out var item3)) continue;
                
                if (item1 == item2 && item2 == item3)
                {
                    matches.Add(tile1);
                    matches.Add(tile2);
                    matches.Add(tile3);
                }
            }
        }
        
        return matches.Count;
    }

    private int FindPossibleMatchesInLayout(Dictionary<Match3Tile, SOItemData> layout, SOGridShape gridShape)
    {
        HashSet<Match3Tile> possibleMatchTiles = new HashSet<Match3Tile>();
        HashSet<(Vector2Int, Vector2Int)> checkedPairs = new HashSet<(Vector2Int, Vector2Int)>();

        foreach (var tileItemPair in layout)
        {
            var tile = tileItemPair.Key;
            if (!gridHandler.IsValidTile(tile)) continue;

            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (var direction in directions)
            {
                var neighborPos = tile.GridPosition + direction;
                var neighborTile = gridHandler.GetTile(neighborPos);
        
                if (!gridHandler.IsValidTile(neighborTile)) continue;
                if (!layout.ContainsKey(neighborTile)) continue;
        
                var pair = tile.GridPosition.x < neighborPos.x || (tile.GridPosition.x == neighborPos.x && tile.GridPosition.y < neighborPos.y)
                    ? (tile.GridPosition, neighborPos)
                    : (neighborPos, tile.GridPosition);
            
                if (!checkedPairs.Add(pair)) continue;

                var currentItemData = layout[tile];
                var neighborItemData = layout[neighborTile];
        
                if (WouldCreateMatchInLayout(neighborPos, currentItemData, layout, gridShape) || 
                    WouldCreateMatchInLayout(tile.GridPosition, neighborItemData, layout, gridShape))
                {
                    possibleMatchTiles.Add(tile);
                    possibleMatchTiles.Add(neighborTile);
                }
            }
        }

        return possibleMatchTiles.Count;
    }

    private bool WouldCreateMatchInLayout(Vector2Int position, SOItemData itemData, 
        Dictionary<Match3Tile, SOItemData> layout, SOGridShape gridShape)
    {
        int horizontalCount = 1;
        for (int x = position.x - 1; x >= 0; x--)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(x, position.y));
            if (!gridHandler.IsValidTile(checkTile)) break;
            if (!layout.TryGetValue(checkTile, out var checkItemData)) break;
            if (checkItemData != itemData) break;
            horizontalCount++;
        }
        for (int x = position.x + 1; x < gridShape.Grid.Width; x++)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(x, position.y));
            if (!gridHandler.IsValidTile(checkTile)) break;
            if (!layout.TryGetValue(checkTile, out var checkItemData)) break;
            if (checkItemData != itemData) break;
            horizontalCount++;
        }
        
        if (horizontalCount >= gameManager.MinMatchCount) return true;
        
        int verticalCount = 1;
        for (int y = position.y - 1; y >= 0; y--)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(position.x, y));
            if (!gridHandler.IsValidTile(checkTile)) break;
            if (!layout.TryGetValue(checkTile, out var checkItemData)) break;
            if (checkItemData != itemData) break;
            verticalCount++;
        }
        for (int y = position.y + 1; y < gridShape.Grid.Height; y++)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(position.x, y));
            if (!gridHandler.IsValidTile(checkTile)) break;
            if (!layout.TryGetValue(checkTile, out var checkItemData)) break;
            if (checkItemData != itemData) break;
            verticalCount++;
        }
        
        return verticalCount >= gameManager.MinMatchCount;
    }
    
    private bool ShouldSpawnHelperObject()
    {
        var loseConditions = gameManager.CurrentLevelData.HasLoseConditions() && gameManager.CurrentLevelData.IsAnyLoseConditionBellowHalf();
        var chanceCheck = gameManager.ChanceToSpawnHelper > 0 && Random.Range(0, 100) < gameManager.ChanceToSpawnHelper;
        
        return loseConditions && chanceCheck;
    }
        
    #endregion


}