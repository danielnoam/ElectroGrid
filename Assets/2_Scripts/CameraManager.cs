
using DNExtensions.Utilities;
using DNExtensions.Utilities.Button;
using DNExtensions.Systems.VFXManager;
using PrimeTween;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }
    
    [Header("Match3")]
    [SerializeField] private float gridPadding = 0.5f; 
    
    [Header("References")]
    [SerializeField] private Camera cam;
    
    
    [Separator]
    [SerializeField, ReadOnly] private Vector2Int gridSize = new Vector2Int(10,10);
    private Match3GameManager _match3GameManager;



    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        SubscribeToMatch3Manager();
        BindVFXCanvas();
    }

    /// <summary>
    /// VFXManager persists across scenes and keeps the previous scene's camera, which is destroyed on load.
    /// A Screen Space - Camera canvas with no camera renders as an overlay, putting the fullscreen fade
    /// above the scene's own UI, so every scene's camera claims the canvas when it starts.
    /// </summary>
    private void BindVFXCanvas()
    {
        if (!cam || !VFXManager.Instance || !VFXManager.Instance.FullScreenImage) return;

        var canvas = VFXManager.Instance.FullScreenImage.canvas.rootCanvas;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
    }

    private void OnEnable()
    {
        SubscribeToMatch3Manager();
    }

    private void SubscribeToMatch3Manager()
    {
        if (!_match3GameManager) _match3GameManager = Match3GameManager.Instance;
        if (_match3GameManager)
        {
            _match3GameManager.LevelStarted -= OnLevelStarted;
            _match3GameManager.LevelStarted += OnLevelStarted;
        }
    }

    private void OnDisable()
    {
        if (_match3GameManager)
        {
            _match3GameManager.LevelStarted -= OnLevelStarted;
        }

    }




    private void OnLevelStarted(Match3LevelData level)
    {
        if (level == null) return;
        var gridShape = level.Level.GridShape;
        var size = new Vector2Int(gridShape.Grid.Width, gridShape.Grid.Height);
        UpdateGridSize(size);
    }

    private void UpdateGridSize(Vector2Int size)
    {
        gridSize = size;
        FitCameraToGrid();
    }

    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void FitCameraToGrid()
    {
        if (!cam) return;
        
        float screenAspect = (float)Screen.width / Screen.height;
        float gridAspect = gridSize.x / gridSize.y;

        if (screenAspect >= gridAspect)
        {
            cam.orthographicSize = (gridSize.y / 2f) + gridPadding;
        }
        else
        {
            float targetHeight =  gridSize.x / screenAspect;
            cam.orthographicSize = (targetHeight / 2f) + gridPadding;
        }
    }
    
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    public void ShakeCamera(float strength = 1f,float duration = 0.5f,float frequency = 10f)
    {
        if (SaveManager.Instance && !SaveManager.Instance.ScreenShakeEnabled) return;
        if (FirebaseManager.Instance) strength *= FirebaseManager.Instance.ScreenShakeIntensityMultiplier;
        var sequence = Sequence.Create();
        sequence.Group(Tween.ShakeCamera(cam, strength, duration, frequency));
    }
}
