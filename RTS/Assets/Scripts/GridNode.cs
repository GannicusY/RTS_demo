using System.Collections;
using UnityEngine;

public struct GridNode
{
    private CoordinateGrid<GridNode> grid;

    public bool walkable;

    public int x;
    public int z;

    public int gCost;
    public int hCost;
    public int fCost;

    public int index;
    public int previousNodeIndex;

    public GridNode(CoordinateGrid<GridNode> grid, int x, int z)
    {
        this.grid = grid;
        this.x = x;
        this.z = z;
        walkable = true;
        index = 0;
        gCost = 0;
        hCost = 0;
        fCost = 0;
        previousNodeIndex = -1;
    }



    public override string ToString()
    {
        return x + "," + z;
    }
}