using UnityEngine;

[CreateAssetMenu(fileName = "New Grid Shape", menuName = "Grid/Grid Shape")]
public class SOGridShape : ScriptableObject
{
    [SerializeField] private Grid grid = new Grid();
    public Grid Grid => grid;
}