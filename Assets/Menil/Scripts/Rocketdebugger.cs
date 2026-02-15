using System.Collections.Generic;
using UnityEngine;

public class RocketDebugger : MonoBehaviour
{
    [ContextMenu("Debug Assembly State")]
    public void DebugAssemblyState()
    {
        Debug.Log("=== ROCKET ASSEMBLY DEBUG ===");
        Debug.Log($"Total Parts: {PartDrag.assemblyParts.Count}");

        foreach (var part in PartDrag.assemblyParts)
        {
            Debug.Log($"\n[{part.name}] Role: {part.role}");
            Debug.Log($"  connectedPart: {(part.connectedPart != null ? part.connectedPart.name : "NULL")}");
            Debug.Log($"  attachedParts count: {part.attachedParts.Count}");

            if (part.attachedParts.Count > 0)
            {
                foreach (var attached in part.attachedParts)
                {
                    if (attached != null)
                    {
                        Debug.Log($"    - {attached.name} ({attached.role})");
                    }
                    else
                    {
                        Debug.Log($"    - NULL REFERENCE!");
                    }
                }
            }
        }

        var graph = BuildGraphDebug();

        Debug.Log("\n=== GRAPH STRUCTURE ===");
        foreach (var kvp in graph)
        {
            Debug.Log($"{kvp.Key.name} ({kvp.Key.role}) has {kvp.Value.Count} edges:");
            foreach (var neighbor in kvp.Value)
            {
                Debug.Log($"  -> {neighbor.name} ({neighbor.role})");
            }
        }

        var payload = PartDrag.GetPayload();
        if (payload != null)
        {
            Debug.Log($"\n=== PAYLOAD CHECK ===");
            Debug.Log($"Payload connectedPart: {(payload.connectedPart != null ? payload.connectedPart.name : "NULL")}");
            Debug.Log($"Payload attachedParts: {payload.attachedParts.Count}");
            Debug.Log($"Payload graph degree: {graph[payload].Count}");

            if (graph[payload].Count > 1)
            {
                Debug.LogError("PROBLEM: Payload has multiple graph connections!");
                foreach (var conn in graph[payload])
                {
                    Debug.LogError($"  Connected to: {conn.name} ({conn.role})");
                }
            }
        }

        string error;
        bool valid = PartDrag.ValidateAssembly(out error);
        Debug.Log($"\n=== VALIDATION ===");
        Debug.Log($"Valid: {valid}");
        if (!valid)
        {
            Debug.LogError($"Error: {error}");
        }
    }

    [ContextMenu("Clean All Connections")]
    public void CleanAllConnections()
    {
        Debug.Log("=== CLEANING ALL CONNECTIONS ===");

        foreach (var part in PartDrag.assemblyParts)
        {
            part.connectedPart = null;
            part.attachedParts.Clear();
            part.mySnapUsed = null;
            part.otherSnapUsed = null;
        }

        Debug.Log("All connections cleared. Re-snap parts to rebuild.");
    }

    [ContextMenu("Force Re-snap All Parts")]
    public void ForceResnap()
    {
        Debug.Log("=== FORCE RE-SNAP ===");

        CleanAllConnections();

        foreach (var part in PartDrag.assemblyParts)
        {
            if (!part.locked)
            {
                part.TrySnapExternally();
            }
        }

        Debug.Log("Re-snap complete. Check assembly state.");
        DebugAssemblyState();
    }

    private Dictionary<PartDrag, List<PartDrag>> BuildGraphDebug()
    {
        var graph = new Dictionary<PartDrag, List<PartDrag>>();

        foreach (var p in PartDrag.assemblyParts)
        {
            graph[p] = new List<PartDrag>();
        }

        foreach (var p in PartDrag.assemblyParts)
        {
            if (p.connectedPart != null)
            {
                if (graph.ContainsKey(p.connectedPart))
                {
                    if (!graph[p].Contains(p.connectedPart))
                    {
                        graph[p].Add(p.connectedPart);
                    }
                    if (!graph[p.connectedPart].Contains(p))
                    {
                        graph[p.connectedPart].Add(p);
                    }
                }
            }
        }

        return graph;
    }
}
