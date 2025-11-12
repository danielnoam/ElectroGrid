using System;
using UnityEngine;

public class TestGr : MonoBehaviour
{

    [SerializeField] private Grid grid;

    private void OnDrawGizmos()
    {
        grid.DrawGrid();
    }
}
