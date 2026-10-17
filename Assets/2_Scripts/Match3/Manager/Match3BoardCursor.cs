using DNExtensions.Utilities;
using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Plays the board with a gamepad or the keyboard: Navigate moves a frame over the board, Submit picks up the piece
/// under it, and a direction then swaps it with that neighbour. Submit again or Cancel puts it back. Only shown while a
/// controller is in use and the top <see cref="FocusScope"/> lets the game have the input. LB and RB (Q and E) jump to the
/// ends of the row.
/// </summary>
[DefaultExecutionOrder(-50)]
public class Match3BoardCursor : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Range(0.1f, 1f)] private float deadZone = 0.5f;
    [SerializeField, Min(0.05f)] private float repeatDelay = 0.3f;
    [SerializeField, Min(0.02f)] private float repeatInterval = 0.1f;
    [SerializeField, Min(0f)] private float moveDuration = 0.08f;

    [Header("Look")]
    [SerializeField] private Color cursorColor = new Color(0.85f, 0.95f, 1f, 1f);
    [SerializeField, Min(0.5f)] private float cursorScale = 1.12f;
    [SerializeField, Min(0f)] private float pulseAmount = 0.04f;
    [SerializeField, Min(0f)] private float pulseSpeed = 5f;
    [SerializeField] private int sortingOffset = 5;

    [Header("References")]
    [SerializeField] private Match3PlayHandler playHandler;
    [SerializeField] private Match3GridHandler gridHandler;
    [SerializeField] private Match3GameManager gameManager;
    [SerializeField] private Match3SelectionIndicator selectionIndicator;

    [Separator]
    [SerializeField, ReadOnly] private Vector2Int cell;
    [SerializeField, ReadOnly] private bool shown;

    private SpriteRenderer _frame;
    private InputAction _navigate;
    private InputAction _submit;
    private InputAction _cancel;
    private InputAction _edgeLeft;
    private InputAction _edgeRight;
    private Vector2Int _heldDirection;
    private float _nextRepeat;
    private bool _placed;
    private Tween _moveTween;
    private Vector3 _baseScale;
    private bool _grabbed;

    public bool Shown => shown;
    public Vector2Int Cell => cell;

    private void Update()
    {
        if (_grabbed && playHandler && !playHandler.IsHolding) _grabbed = false;
        bool show = ShouldShow();
        if (show != shown) SetShown(show);
        if (!show) return;

        _navigate ??= ControllerFocus.FindUiAction("Navigate");
        _submit ??= ControllerFocus.FindUiAction("Submit");
        _cancel ??= ControllerFocus.FindUiAction("Cancel");
        _edgeLeft ??= ControllerFocus.FindUiAction("EdgeLeft");
        _edgeRight ??= ControllerFocus.FindUiAction("EdgeRight");

        if (!_placed || !gridHandler.GetTile(cell) || !gridHandler.GetTile(cell).IsActive) PlaceAtCentre();

        var direction = ReadDirection();
        if (direction != Vector2Int.zero) Step(direction);
        if (_edgeLeft != null && _edgeLeft.WasPerformedThisFrame()) JumpToEdge(-1);
        if (_edgeRight != null && _edgeRight.WasPerformedThisFrame()) JumpToEdge(1);
        if (_submit != null && _submit.WasPerformedThisFrame()) Submit();
        if (_cancel != null && _cancel.WasPerformedThisFrame() && Back()) ControllerFocus.ConsumeCancel();

        UpdateLook();
    }

    private bool ShouldShow()
    {
        if (!ControllerFocus.ControllerActive || !playHandler || !gridHandler || !gameManager) return false;
        if (!gameManager.IsPlaying || gridHandler.Tiles == null || gridHandler.Tiles.Count == 0) return false;

        var scope = ControllerFocus.TopScope;
        return !scope || scope.ControlsGame;
    }

    private void SetShown(bool show)
    {
        shown = show;
        if (show)
        {
            if (!_frame) CreateFrame();
            selectionIndicator?.ResetHoveredTile();
            _heldDirection = Vector2Int.zero;
        }
        else if (_grabbed && playHandler && playHandler.IsHolding)
        {
            playHandler.Release();
            _grabbed = false;
        }

        if (_frame) _frame.gameObject.SetActive(show);
    }

    private void CreateFrame()
    {
        var reference = FirstTileRenderer();
        _frame = new GameObject("Board Cursor").AddComponent<SpriteRenderer>();
        if (reference)
        {
            _frame.sprite = reference.sprite;
            _frame.sharedMaterial = reference.sharedMaterial;
            _frame.sortingLayerID = reference.sortingLayerID;
            _frame.sortingOrder = reference.sortingOrder + sortingOffset;
        }

        _frame.color = cursorColor;
    }

    private SpriteRenderer FirstTileRenderer()
    {
        foreach (var tile in gridHandler.Tiles.Values)
        {
            if (tile && tile.IsActive)
            {
                var renderer = tile.GetComponentInChildren<SpriteRenderer>();
                if (renderer && renderer.sprite) return renderer;
            }
        }

        return null;
    }

    private void PlaceAtCentre()
    {
        var grid = gridHandler.Grid;
        var centre = new Vector2(grid.Width - 1, grid.Height - 1) * 0.5f;
        float best = float.MaxValue;
        foreach (var tile in gridHandler.Tiles.Values)
        {
            if (!tile || !tile.IsActive) continue;

            float distance = (tile.GridPosition - centre).sqrMagnitude;
            if (distance < best)
            {
                best = distance;
                cell = tile.GridPosition;
            }
        }

        _placed = true;
        SnapFrame();
    }

    /// <summary>A new press moves at once; holding the direction repeats after a short delay.</summary>
    private Vector2Int ReadDirection()
    {
        var value = _navigate != null ? _navigate.ReadValue<Vector2>() : Vector2.zero;
        Vector2Int direction = Vector2Int.zero;
        if (value.magnitude >= deadZone)
        {
            direction = Mathf.Abs(value.x) >= Mathf.Abs(value.y)
                ? (value.x > 0 ? Vector2Int.right : Vector2Int.left)
                : (value.y > 0 ? Vector2Int.up : Vector2Int.down);
        }

        if (direction == Vector2Int.zero)
        {
            _heldDirection = Vector2Int.zero;
            return Vector2Int.zero;
        }

        float now = Time.unscaledTime;
        if (direction != _heldDirection)
        {
            _heldDirection = direction;
            _nextRepeat = now + repeatDelay;
            return direction;
        }

        if (playHandler.IsHolding || now < _nextRepeat) return Vector2Int.zero;

        _nextRepeat = now + repeatInterval;
        return direction;
    }

    /// <summary>What a direction does: swaps the held piece that way, or moves the cursor.</summary>
    public void Step(Vector2Int direction)
    {
        if (playHandler.IsHolding) playHandler.SwapHeld(direction);
        else Move(direction);
    }

    /// <summary>Jumps to the last board cell of the row in a direction: -1 is the left end, 1 the right end.</summary>
    public void JumpToEdge(int side)
    {
        if (playHandler.IsHolding || side == 0) return;

        var grid = gridHandler.Grid;
        int x = side < 0 ? 0 : grid.Width - 1;
        for (; x >= 0 && x < grid.Width; x -= side)
        {
            var tile = gridHandler.GetTile(new Vector2Int(x, cell.y));
            if (!tile || !tile.IsActive) continue;
            if (x == cell.x) return;

            cell = new Vector2Int(x, cell.y);
            MobileHaptics.Play(Haptic.Tick, 0.7f);
            MoveFrame();
            return;
        }
    }

    /// <summary>Puts a held piece back. False when nothing was held, so Cancel goes on to the screen.</summary>
    public bool Back()
    {
        if (!playHandler.IsHolding) return false;

        playHandler.Release();
        return true;
    }

    /// <summary>Moves to the next board cell in a direction, stepping over holes in the board's shape.</summary>
    private void Move(Vector2Int direction)
    {
        var grid = gridHandler.Grid;
        var next = cell + direction;
        while (next.x >= 0 && next.y >= 0 && next.x < grid.Width && next.y < grid.Height)
        {
            var tile = gridHandler.GetTile(next);
            if (tile && tile.IsActive)
            {
                cell = next;
                MobileHaptics.Play(Haptic.Tick, 0.5f);
                MoveFrame();
                return;
            }

            next += direction;
        }
    }

    public void MoveTo(Vector2Int target)
    {
        var tile = gridHandler.GetTile(target);
        if (!tile || !tile.IsActive) return;

        cell = target;
        _placed = true;
        SnapFrame();
    }

    public void Submit()
    {
        if (playHandler.IsHolding)
        {
            playHandler.Release();
            return;
        }

        if (!playHandler.Grab(gridHandler.GetTile(cell))) return;

        _grabbed = true;
        MobileHaptics.Play(Haptic.Tick);
    }

    private void MoveFrame()
    {
        if (!_frame) return;

        var target = FramePosition();
        _moveTween.Stop();
        if (moveDuration <= 0f) _frame.transform.position = target;
        else _moveTween = Tween.Position(_frame.transform, target, moveDuration, Ease.OutQuad);
    }

    private void SnapFrame()
    {
        if (!_frame) return;

        _moveTween.Stop();
        _frame.transform.position = FramePosition();
    }

    private Vector3 FramePosition()
    {
        var tile = gridHandler.GetTile(cell);
        if (!tile) return _frame.transform.position;

        _baseScale = tile.transform.lossyScale * cursorScale;
        var position = tile.transform.position;
        position.z = tile.transform.position.z - 0.01f;
        return position;
    }

    private void UpdateLook()
    {
        if (!_frame) return;

        if (_baseScale == Vector3.zero) FramePosition();
        float pulse = playHandler.IsHolding ? 1.06f : 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;
        _frame.transform.localScale = _baseScale * pulse;

        var color = cursorColor;
        if (!playHandler.AcceptsInput) color.a *= 0.35f;
        _frame.color = color;
    }

    private void OnDisable()
    {
        _moveTween.Stop();
        if (_frame) _frame.gameObject.SetActive(false);
        shown = false;
    }

    private void OnDestroy()
    {
        if (_frame) Destroy(_frame.gameObject);
    }
}
