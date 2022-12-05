using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class Test : MonoBehaviour
{
    public static Test Instance;

    public CoordinateGrid<GridNode> grid;

    private void Awake()
    {
        Instance = this;        
    }

    private void Start()
    {
        grid = new CoordinateGrid<GridNode>(100, 100, 1f, new Vector3(0, 0.1f, 0), displayGrid: true,
                 (CoordinateGrid<GridNode> myGrid, int x, int z) => new GridNode(myGrid, x, z));
    }


}
