using System.Collections;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;

public struct NavigationData : IComponentData
{
    public int2 startPosition;
    public int2 endPosition;
}
