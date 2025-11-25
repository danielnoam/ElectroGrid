using System;
using Core.Attributes;
using UnityEngine;

public class TestGrid : MonoBehaviour
{
    
    [SerializeField] private Grid testGrid;


    private void OnDrawGizmos()
    {
        testGrid?.DrawGrid();
    }
}
