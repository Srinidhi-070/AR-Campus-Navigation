using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializableNode
{
    public int x, y;
    public bool isWalkable;
    public int nodeType;

    public string connectedMap;
    public Vector2Int connectedNode;
    public int connectionID;

    public string nodeName;
}

[Serializable]
public class SerializableMapData
{
    public string mapName;
    public string buildingName;
    public int floorNumber;
    public int width;
    public int height;

    public List<SerializableNode> nodes = new List<SerializableNode>();
}