using Unity.Entities;
using Unity.Jobs;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Burst;
using static UnityEngine.EventSystems.EventTrigger;
using UnityEditor.Experimental.GraphView;

[UpdateAfter(typeof(UnitMoveOrderSystem))]
public partial class NavigationSystem : ComponentSystem
{
    private const int StraightMoveCost = 10;
    private const int DiagonalMoveCost = 14;

    protected override void OnUpdate()
    {
        Entities.ForEach((Entity entity, ref NavigationData navigationData, DynamicBuffer<Path> pathBuffer) =>
        {
            FindPathJob findPathJob = new FindPathJob
            {
                startPosition = navigationData.startPosition,
                endPosition = navigationData.endPosition,
                pathBuffer = pathBuffer,
                entity = entity,
                pathFollowComponentDataFromEntity = GetComponentDataFromEntity<PathFollow>()
            };
            findPathJob.Run();

            PostUpdateCommands.RemoveComponent<NavigationData>(entity);
        });        
    }

    

    [BurstCompile]
    private struct FindPathJob : IJob
    {
        private int2 gridSize;        

        public int2 startPosition;
        public int2 endPosition;

        public Entity entity;
        public ComponentDataFromEntity<PathFollow> pathFollowComponentDataFromEntity;
        public DynamicBuffer<Path> pathBuffer;

        public void Execute()
        {
            gridSize = new int2(100, 100);

            NativeArray<GridNode>  gridNodeArray = new NativeArray<GridNode>(gridSize.x * gridSize.y, Allocator.Temp);

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int z = 0; z < gridSize.y; z++)
                {
                    GridNode gridNode = new GridNode();
                    gridNode.x = x;
                    gridNode.z = z;
                    gridNode.index = CalculateIndex(x, z, gridSize.x);

                    gridNode.gCost = int.MaxValue;
                    gridNode.hCost = CalculateHCost(new int2(x, z), endPosition);
                    gridNode.fCost = CalculateFCost(gridNode.gCost, gridNode.fCost);

                    gridNode.walkable = true;

                    gridNode.previousNodeIndex = -1;
                    gridNodeArray[gridNode.index] = gridNode;
                }
            }

            NativeArray<int2> neighbourOffsetArray = new NativeArray<int2>(8, Allocator.Temp);
            neighbourOffsetArray[0] = new int2(-1, 0); // Left
            neighbourOffsetArray[1] = new int2(+1, 0); // Right
            neighbourOffsetArray[2] = new int2(0, +1); // Up
            neighbourOffsetArray[3] = new int2(0, -1); // Down
            neighbourOffsetArray[4] = new int2(-1, -1); // Left Down
            neighbourOffsetArray[5] = new int2(-1, +1); // Left Up
            neighbourOffsetArray[6] = new int2(+1, -1); // Right Down
            neighbourOffsetArray[7] = new int2(+1, +1); // Right Up

            int endNodeIndex = CalculateIndex(endPosition.x, endPosition.y, gridSize.x);                       

            GridNode startNode = gridNodeArray[CalculateIndex(startPosition.x, startPosition.y, gridSize.x)];
            startNode.gCost = 0;
            startNode.fCost = CalculateFCost(startNode.gCost, startNode.hCost);
            gridNodeArray[startNode.index] = startNode;
                        
            NativeList<int> openList = new NativeList<int>(Allocator.Temp);
            NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

            openList.Add(startNode.index);

            while (openList.Length > 0)
            {
                int currentNodeIndex = GetLowestCostFNodeIndex(openList, gridNodeArray);
                GridNode currentNode = gridNodeArray[currentNodeIndex];

                if (currentNodeIndex == endNodeIndex)
                {
                    // Reached our destination!
                    break;
                }

                // Remove current node from Open List
                for (int i = 0; i < openList.Length; i++)
                {
                    if (openList[i] == currentNodeIndex)
                    {
                        openList.RemoveAtSwapBack(i);
                        break;
                    }
                }

                closedList.Add(currentNodeIndex);

                for (int i = 0; i < neighbourOffsetArray.Length; i++)
                {
                    int2 neighbourOffset = neighbourOffsetArray[i];
                    int2 neighbourPosition = new int2(currentNode.x + neighbourOffset.x, currentNode.z + neighbourOffset.y);

                    if (!IsPositionInsideGrid(neighbourPosition, gridSize))
                    {
                        // Neighbour not valid position
                        continue;
                    }

                    int neighbourNodeIndex = CalculateIndex(neighbourPosition.x, neighbourPosition.y, gridSize.x);

                    if (closedList.Contains(neighbourNodeIndex))
                    {
                        // Already searched this node
                        continue;
                    }

                    GridNode neighbourNode = gridNodeArray[neighbourNodeIndex];
                    if (!neighbourNode.walkable)
                    {
                        // Not walkable
                        continue;
                    }

                    int2 currentNodePosition = new int2(currentNode.x, currentNode.z);

                    int tentativeGCost = currentNode.gCost + CalculateHCost(currentNodePosition, neighbourPosition);
                    if (tentativeGCost < neighbourNode.gCost)
                    {
                        neighbourNode.previousNodeIndex = currentNodeIndex;
                        neighbourNode.gCost = tentativeGCost;
                        neighbourNode.fCost = CalculateFCost(neighbourNode.gCost, neighbourNode.hCost);
                        gridNodeArray[neighbourNodeIndex] = neighbourNode;

                        if (!openList.Contains(neighbourNode.index))
                        {
                            openList.Add(neighbourNode.index);
                        }
                    }

                }
            }

            pathBuffer.Clear();


            GridNode endNode = gridNodeArray[endNodeIndex];
            if (endNode.previousNodeIndex == -1)
            {
                //Didn't find a path!
                Debug.Log("Didn't find a path!");
                pathFollowComponentDataFromEntity[entity] = new PathFollow { pathIndex = -1 };
            }
            else
            {
                //Found a path
                CalculatePath(gridNodeArray, endNode, pathBuffer);
                pathFollowComponentDataFromEntity[entity] = new PathFollow { pathIndex = pathBuffer.Length - 1 };
            }


            neighbourOffsetArray.Dispose();
            gridNodeArray.Dispose();
            openList.Dispose();
            closedList.Dispose();
        }
    }

    private static int CalculateIndex(int x, int z, int gridWidth)
    {        
        return x + z * gridWidth;
    }

    private static int CalculateHCost(int2 aPosition, int2 bPosition)
    {
        int xDistance = math.abs(aPosition.x - bPosition.x);
        int yDistance = math.abs(aPosition.y - bPosition.y);
        int remaining = math.abs(xDistance - yDistance);
        return DiagonalMoveCost * math.min(xDistance, yDistance) + StraightMoveCost * remaining;
    }

    private static int CalculateFCost(int a, int b)
    {
        return a + b;
    }

    private static void CalculatePath(NativeArray<GridNode> pathNodeArray, GridNode endNode, DynamicBuffer<Path> pathPositionBuffer)
    {
        if (endNode.previousNodeIndex == -1)
        {
            // Couldn't find a path!
        }
        else
        {
            // Found a path
            pathPositionBuffer.Add(new Path { position = new int2(endNode.x, endNode.z) });

            GridNode currentNode = endNode;
            while (currentNode.previousNodeIndex != -1)
            {
                GridNode cameFromNode = pathNodeArray[currentNode.previousNodeIndex];
                pathPositionBuffer.Add(new Path { position = new int2(cameFromNode.x, cameFromNode.z) });
                currentNode = cameFromNode;
            }
        }
    }


    private static int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<GridNode> pathNodeArray)
    {
        GridNode lowestCostPathNode = pathNodeArray[openList[0]];
        for (int i = 1; i < openList.Length; i++)
        {
            GridNode testPathNode = pathNodeArray[openList[i]];
            if (testPathNode.fCost < lowestCostPathNode.fCost)
            {
                lowestCostPathNode = testPathNode;
            }
        }
        return lowestCostPathNode.index;
    }

    private static bool IsPositionInsideGrid(int2 gridPosition, int2 gridSize)
    {
        return
            gridPosition.x >= 0 &&
            gridPosition.y >= 0 &&
            gridPosition.x < gridSize.x &&
            gridPosition.y < gridSize.y;
    }

    
    //private List<GridNode> FindPath(int startX, int startZ, int endX, int endZ)
    //{
    //    GridNode startNode = grid.GetGridNode(startX, startZ);
    //    GridNode endNode = grid.GetGridNode(endX, endZ);

    //    openList = new List<GridNode>() { startNode };
    //    closedList = new List<GridNode>();

    //    for (int x = 0; x < grid.Width; x++)
    //    {
    //        for (int z = 0; z < grid.Length; z++)
    //        {
    //            GridNode gridNode = grid.GetGridNode(x, z);
    //            gridNode.gCost = int.MaxValue;
    //            gridNode.CalculateFCost();
    //            gridNode.previousNode = null;
    //        }
    //    }

    //    startNode.gCost = 0;
    //    startNode.hCost = CalculateDistance(startNode, endNode);
    //    startNode.CalculateFCost();

    //    while (openList.Count > 0)
    //    {
    //        GridNode currentNode = GetLowestFCostNode(openList);
    //        if (currentNode == endNode)
    //        {
    //            return CalculatePath(endNode);
    //        }

    //        openList.Remove(currentNode);
    //        closedList.Add(currentNode);

    //        List<GridNode> neighbourList = GetNeighbourNodes(currentNode);

    //        foreach (GridNode neighbourNode in neighbourList)
    //        {
    //            if (closedList.Contains(neighbourNode)) continue;

    //            if(!neighbourNode.walkable)
    //            {
    //                closedList.Add(neighbourNode);
    //                continue;
    //            }

    //            int tentativeGCost = currentNode.gCost + CalculateDistance(currentNode, neighbourNode);
    //            if(tentativeGCost < neighbourNode.gCost)
    //            {
    //                neighbourNode.previousNode = currentNode;
    //                neighbourNode.gCost = tentativeGCost;
    //                neighbourNode.hCost = CalculateDistance(neighbourNode, endNode);
    //                neighbourNode.CalculateFCost();

    //                if (!openList.Contains(neighbourNode)) openList.Add(neighbourNode);
    //            }

    //        }
    //    }

    //    return null;
    //}

    //private List<GridNode> GetNeighbourNodes(GridNode currentNode)
    //{
    //    List<GridNode> neighbourList = new List<GridNode>();

    //    if (currentNode.x - 1 >= 0)
    //    {
    //        // Left
    //        neighbourList.Add(grid.GetGridNode(currentNode.x - 1, currentNode.z));
    //        // Left Down
    //        if (currentNode.z - 1 >= 0) neighbourList.Add(grid.GetGridNode(currentNode.x - 1, currentNode.z - 1));
    //        // Left Up
    //        if (currentNode.z + 1 < grid.Length) neighbourList.Add(grid.GetGridNode(currentNode.x - 1, currentNode.z + 1));
    //    }
    //    if (currentNode.x + 1 < grid.Width)
    //    {
    //        // Right
    //        neighbourList.Add(grid.GetGridNode(currentNode.x + 1, currentNode.z));
    //        // Right Down
    //        if (currentNode.z - 1 >= 0) neighbourList.Add(grid.GetGridNode(currentNode.x + 1, currentNode.z - 1));
    //        // Right Up
    //        if (currentNode.z + 1 < grid.Length) neighbourList.Add(grid.GetGridNode(currentNode.x + 1, currentNode.z + 1));
    //    }
    //    // Down
    //    if (currentNode.z - 1 >= 0) neighbourList.Add(grid.GetGridNode(currentNode.x, currentNode.z - 1));
    //    // Up
    //    if (currentNode.z + 1 < grid.Length) neighbourList.Add(grid.GetGridNode(currentNode.x, currentNode.z + 1));

    //    return neighbourList;
    //}

    
    //private List<GridNode> CalculatePath(GridNode endNode)
    //{
    //    List<GridNode> path = new List<GridNode>();
    //    path.Add(endNode);
    //    GridNode currentNode = endNode;
    //    while (currentNode.previousNode != null)
    //    {
    //        path.Add(currentNode.previousNode);
    //        currentNode = currentNode.previousNode;
    //    }
    //    path.Reverse();
    //    return path;
    //}

    //private int CalculateDistance(GridNode a, GridNode b)
    //{
    //    int xDistance = Mathf.Abs(a.x - b.x);
    //    int zDistance = Mathf.Abs(a.z - b.z);
    //    int remaining = Mathf.Abs(xDistance - zDistance);
        
    //    return DiagonalMoveCost * Mathf.Min(xDistance, zDistance) + StraightMoveCost * remaining;
    //}

    //private GridNode GetLowestFCostNode(List<GridNode> gridNodeList)
    //{
    //    GridNode lowestFCostNode = gridNodeList[0];
        
    //    for (int i = 1; i < gridNodeList.Count; i++)
    //    {
    //        if (gridNodeList[i].fCost < lowestFCostNode.fCost)
    //        {
    //            lowestFCostNode = gridNodeList[i];
    //        }
    //    }

    //    return lowestFCostNode;
    //}
}
