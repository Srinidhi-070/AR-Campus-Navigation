using System.Collections.Generic;
using UnityEngine;

public class DirectionManager : MonoBehaviour
{
    public List<string> GenerateDirections(List<NavigationNode> path)
    {
        List<string> directions = new List<string>();

        if (path == null || path.Count < 2)
            return directions;

        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector3 prev = path[i - 1].Position;
            Vector3 current = path[i].Position;
            Vector3 next = path[i + 1].Position;

            Vector3 dir1 = (current - prev).normalized;
            Vector3 dir2 = (next - current).normalized;

            float angle = Vector3.SignedAngle(dir1, dir2, Vector3.up);

            if (angle > 20)
                directions.Add("Turn Right");
            else if (angle < -20)
                directions.Add("Turn Left");
            else
                directions.Add("Go Straight");
        }

        directions.Insert(0, "Start");
        directions.Add("Destination Reached");

        return directions;
    }
}
