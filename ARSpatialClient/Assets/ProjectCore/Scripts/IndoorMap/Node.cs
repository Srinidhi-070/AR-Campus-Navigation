using UnityEngine;

public enum NodeType
{
    Normal,
    Obstacle,
    StairEntry,
    LiftEntry,
    RoomDoor
}

public class Node
{
    public string nodeName = "";
    public int connectionID = -1;
    public string mapName;
    public Vector2Int position;
    public bool isWalkable = true;
    public NodeType nodeType = NodeType.Normal;
    public string connectedMap;
    public Vector2Int connectedNode;

    public int gCost;
    public int hCost;

    public Node parent;

    public int fCost
    {
        get { return gCost + hCost; }
    }

    public Node(int x, int y)
    {
        this.position = new Vector2Int(x, y);
    }

    public int x => position.x;
    public int y => position.y;
}