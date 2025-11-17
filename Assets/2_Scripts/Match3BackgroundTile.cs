using UnityEngine;

public class Match3BackgroundTile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Color inactiveTileColor = new Color(0.1f, 0.1f, 0.1f, 0.1f);
    
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    public SpriteRenderer SpriteRenderer => spriteRenderer;
    public Color InactiveTileColor => inactiveTileColor;
}
