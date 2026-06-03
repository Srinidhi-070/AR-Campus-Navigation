using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class LocationData
{
    public string id;
    public string[] neighbors;
}

[Serializable]
public class LocalNodesWrapper
{
    public List<LocationData> nodes;
}

public class Program
{
    public static void Main()
    {
        string json = File.ReadAllText(@\"D:\AR_Spatial_Client\ARSpatialClient\Assets\ProjectCore\Resources\nodes.json\");
        
        // Very basic manual parsing because no JsonUtility in console
        var lines = json.Split('\n');
        var nodes = new Dictionary<string, List<string>>();
        string currentId = null;
        
        foreach(var line in lines)
        {
            if (line.Contains(\"\"\"id\"\"\"))
            {
                var parts = line.Split('\"');
                for(int i=0; i<parts.Length; i++) {
                    if (parts[i] == \"id\" && i+2 < parts.Length) {
                        currentId = parts[i+2].Trim().ToUpperInvariant();
                        nodes[currentId] = new List<string>();
                    }
                }
            }
            else if (currentId != null && line.Contains(\"WAYPOINT\") || line.Contains(\"AB_\") || line.Contains(\"LIFT\") || line.Contains(\"STAIR\"))
            {
                if (line.Contains(\"\"\"id\"\"\")) continue;
                var parts = line.Split('\"');
                foreach(var p in parts) {
                    if ((p.StartsWith(\"WAY\") || p.StartsWith(\"AB_\") || p.StartsWith(\"LIFT\") || p.StartsWith(\"STAIR\")) && p != currentId) {
                        nodes[currentId].Add(p.Trim().ToUpperInvariant());
                    }
                }
            }
        }
        
        Console.WriteLine(\$"Parsed {nodes.Count} nodes.\");
        string start = \"AB_802_AD_FACULTY_LOUNGE\";
        string target = \"AB_901B_CLASSROOM\";
        
        if (!nodes.ContainsKey(start)) { Console.WriteLine(\"Start not found\"); return; }
        if (!nodes.ContainsKey(target)) { Console.WriteLine(\"Target not found\"); return; }
        
        HashSet<string> visited = new HashSet<string>();
        Queue<string> q = new Queue<string>();
        q.Enqueue(start);
        
        while(q.Count > 0)
        {
            string curr = q.Dequeue();
            if (curr == target) {
                Console.WriteLine(\"PATH EXISTS\");
                return;
            }
            
            if (visited.Contains(curr)) continue;
            visited.Add(curr);
            
            foreach(var nb in nodes[curr]) {
                q.Enqueue(nb);
            }
        }
        Console.WriteLine(\"NO PATH\");
    }
}
