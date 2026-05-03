using System.Collections.Generic;
using UnityEngine;

public class PathfindingManager : MonoBehaviour
{
    public List<NavigationNode> FindPath(NavigationNode startNode, NavigationNode goalNode)
    {
        List<NavigationNode> openSet = new List<NavigationNode>();
        HashSet<NavigationNode> closedSet = new HashSet<NavigationNode>();

        Dictionary<NavigationNode, NavigationNode> cameFrom = new Dictionary<NavigationNode, NavigationNode>();
        Dictionary<NavigationNode, float> gScore = new Dictionary<NavigationNode, float>();
        Dictionary<NavigationNode, float> fScore = new Dictionary<NavigationNode, float>();

        openSet.Add(startNode);
        gScore[startNode] = 0;
        fScore[startNode] = Heuristic(startNode, goalNode);

        while (openSet.Count > 0)
        {
            NavigationNode current = GetLowestFScore(openSet, fScore);

            if (current == goalNode)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (var neighbor in current.ConnectedNodes)
            {
                if (closedSet.Contains(neighbor)) continue;

                float tentativeG = gScore[current] + Vector3.Distance(current.Position, neighbor.Position);

                if (!openSet.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                    gScore[neighbor] = Mathf.Infinity;
                }
                else if (tentativeG >= gScore[neighbor])
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;
                fScore[neighbor] = tentativeG + Heuristic(neighbor, goalNode);
            }
        }

        return null;
    }

    private float Heuristic(NavigationNode a, NavigationNode b)
    {
        return Vector3.Distance(a.Position, b.Position);
    }

    private NavigationNode GetLowestFScore(List<NavigationNode> nodes, Dictionary<NavigationNode, float> fScore)
    {
        NavigationNode best = nodes[0];

        foreach (var node in nodes)
        {
            if (fScore.GetValueOrDefault(node, Mathf.Infinity) < fScore.GetValueOrDefault(best, Mathf.Infinity))
                best = node;
        }

        return best;
    }

    private List<NavigationNode> ReconstructPath(Dictionary<NavigationNode, NavigationNode> cameFrom, NavigationNode current)
    {
        List<NavigationNode> path = new List<NavigationNode> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }

        return path;
    }
}
