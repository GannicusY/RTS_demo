using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public struct CoordinateGrid<TGridNode>
{
    public int Length { get; private set; }
    public int Width { get; private set; }
    public float Height { get; private set; }
    public float CellSize { get; private set; }

    private Vector3 originPosition;

    private TGridNode[,] gridArray;

    public CoordinateGrid(int length, int width, float cellSize, Vector3 originPosition, bool displayGrid, Func<CoordinateGrid<TGridNode>, int, int, TGridNode> createGridNote)
    {
        this.Length = length;
        this.Width = width;
        this.Height = originPosition.y;
        this.CellSize = cellSize;
        this.originPosition = originPosition;

        //gridArray = new TGridNode[length, width]; 
        gridArray = new TGridNode[length, width];

        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int z = 0; z < gridArray.GetLength(1); z++)
            {
                gridArray[x, z] = createGridNote(this, x, z);
            }
        }

        if (!displayGrid) return;

        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int z = 0; z < gridArray.GetLength(1); z++)
            {
                //RenumberCell((x+1).ToString() + ";" + (z+1).ToString(), GetWorldPosition(x, height, z) + new Vector3(cellSize, 0f, cellSize) * 0.5f, 3, Color.black);
                RenumberCell(gridArray[x,z]?.ToString(), GetWorldPosition(x, Height, z) + new Vector3(cellSize, 0f, cellSize) * 0.5f, 3, Color.black);
                Debug.DrawLine(GetWorldPosition(x, Height, z), GetWorldPosition(x, Height, z + 1), Color.black, Mathf.Infinity);
                Debug.DrawLine(GetWorldPosition(x, Height, z), GetWorldPosition(x + 1, Height, z), Color.black, Mathf.Infinity);
            }
        }

        Debug.DrawLine(GetWorldPosition(0, Height, length), GetWorldPosition(width, Height, length), Color.black, Mathf.Infinity);
        Debug.DrawLine(GetWorldPosition(width, Height, 0), GetWorldPosition(width, Height, length), Color.black, Mathf.Infinity);
        Debug.Log("CellSize: " + CellSize);
    }

    private Vector3 GetWorldPosition(float x, float y, float z)
    {
        return new Vector3(x, y, z) * CellSize + originPosition;
    }

    public void GetCellCoordinate(Vector3 worldPosition, out int x, out int z)
    {
        Debug.Log("worldPositionpositionX: " + worldPosition.x);
        Debug.Log("worldPositionpositionZ: " + worldPosition.z);

        Debug.Log("CellSize: " + CellSize);

        Debug.Log("Mathf.FloorToIntX: " + ((worldPosition - originPosition).x / CellSize));
        x = Mathf.FloorToInt((worldPosition - originPosition).x / CellSize);

        Debug.Log("Mathf.FloorToIntZ: " + ((worldPosition - originPosition).z / CellSize));
        z = Mathf.FloorToInt((worldPosition - originPosition).z / CellSize);

        Debug.Log("positionX: " + x);
        Debug.Log("positionZ: " + z);
    }

    public void SetValue(int x, int z, TGridNode value)
    {
        gridArray[x, z] = value;
    }

    public void SetValue(Vector3 worldPosition, TGridNode value)
    {
        int x, z;
        GetCellCoordinate(worldPosition, out x, out z);
        SetValue(x, z, value);
    }

    public TGridNode GetGridNode(int x, int z)
    {
        return gridArray[x, z];
    }

    public TGridNode GetGridNode(Vector3 worldPosition)
    {
        int x, z;
        GetCellCoordinate(worldPosition, out x, out z);
        return GetGridNode(x, z);
    }

    private void RenumberCell(string text, Vector3 localPosition, int fontSize, Color color)
    {
        GameObject gameObject = new GameObject("Text" + text, typeof(TextMeshPro));

        gameObject.transform.localPosition = localPosition;
        gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(1f, 1f);
        gameObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        TextMeshPro textMesh = gameObject.GetComponent<TextMeshPro>();        
        textMesh.alignment = TextAlignmentOptions.TopRight;
        textMesh.text = text;
        textMesh.fontSize = fontSize;
        textMesh.color = color;
    }
}


