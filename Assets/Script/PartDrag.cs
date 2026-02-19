using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PartRole
{
    CoreTank,
    CoreThruster,
    LiquidTank,
    LargeThruster,
    Payload,
    SideTank,
    SideThruster,
    Separator1,
    Separator2
}

public class PartDrag : MonoBehaviour
{
    public static event System.Action OnAssemblyChanged;

    private static RectTransform deletePanelRect;

    [Header("Snap Settings")]
    public Transform snapPointsRoot;
    public float snapDistance = 0.8f;

    [Header("State")]
    public bool locked = false;
    public bool isRootPart = false;

    public static long pauseBeforeFrame = 0;


    [Header("Identity")]
    public PartRole role;

    [Header("Runtime Connections")]
    public PartDrag connectedPart;
    public Transform mySnapUsed;
    public Transform otherSnapUsed;
    public List<PartDrag> attachedParts = new List<PartDrag>();

    public static int resultNumber = 0;

    [Header("Snap Audio")]
    private static AudioSource rocketAudio;
    public AudioClip snapClip;

    [Header("Parts Weight and Thruster")]
    public float partWeight;
    public float partThrust;

    private Camera cam;
    private Vector3 offset;
    private bool dragging;
    private static Transform rocketRoot;

    public static List<PartDrag> assemblyParts = new List<PartDrag>();

    public static PartDrag GetPayload()
    {
        return assemblyParts.Find(p => p.role == PartRole.Payload);
    }

    public static List<PartRole> GetMainChain()
    {
        var graph = BuildGraph();

        PartDrag payload = GetPayload();
        if (payload == null)
            return new List<PartRole>();

        List<PartDrag> bestPath = new List<PartDrag>();

        void DFS(PartDrag current, PartDrag parent, List<PartDrag> path)
        {
            path.Add(current);

            bool isSide =
                current.role == PartRole.SideThruster ||
                current.role == PartRole.SideTank;

            if (!isSide && path.Count > bestPath.Count)
                bestPath = new List<PartDrag>(path);

            foreach (var next in graph[current])
            {
                if (next == parent)
                    continue;

                DFS(next, current, path);
            }

            path.RemoveAt(path.Count - 1);
        }

        DFS(payload, null, new List<PartDrag>());

        return bestPath.ConvertAll(p => p.role);
    }

    void Awake()
    {
        if (!assemblyParts.Contains(this))
            assemblyParts.Add(this);

        OnAssemblyChanged?.Invoke();

        if (rocketAudio == null)
        {
            GameObject rocket = GameObject.Find("Rocket");
            if (rocket != null)
                rocketAudio = rocket.GetComponent<AudioSource>();
        }
    }

    void OnDestroy()
    {
        assemblyParts.Remove(this);
        DetachCompletely();
        OnAssemblyChanged?.Invoke();
    }

    void Start()
    {
        cam = Camera.main;
        EnsureRocketExists();
        transform.SetParent(rocketRoot, true);
    }

    void EnsureRocketExists()
    {
        if (rocketRoot != null) return;

        GameObject rocketGO = GameObject.Find("Rocket");

        if (rocketGO == null)
        {
            rocketGO = new GameObject("Rocket");
            rocketGO.transform.position = Vector3.zero;
        }

        rocketRoot = rocketGO.transform;
    }

    void OnMouseDown()
    {
        if (locked) return;

        dragging = true;

        Vector3 screenPos = cam.WorldToScreenPoint(transform.position);
        offset = transform.position -
                 cam.ScreenToWorldPoint(
                     new Vector3(
                         Input.mousePosition.x,
                         Input.mousePosition.y,
                         screenPos.z
                     ));

        DetachCompletely();
    }

    void OnMouseDrag()
    {
        if (locked || !dragging) return;

        Vector3 screenPos = new Vector3(
            Input.mousePosition.x,
            Input.mousePosition.y,
            cam.WorldToScreenPoint(transform.position).z
        );

        transform.position = cam.ScreenToWorldPoint(screenPos) + offset;
    }

    void OnMouseUp()
    {
        dragging = false;
        WillDestroy();
        TrySnap();
    }

    private void WillDestroy()
    {
        if (deletePanelRect == null)
        {
            GameObject deletePanel = GameObject.Find("DeletePart");
            if (deletePanel == null) return;

            deletePanelRect = deletePanel.GetComponent<RectTransform>();
            if (deletePanelRect == null) return;
        }

        Vector2 screenPoint = cam.WorldToScreenPoint(transform.position);

        if (RectTransformUtility.RectangleContainsScreenPoint(deletePanelRect, screenPoint))
        {
            Destroy(gameObject);
        }
    }

    void TrySnap()
    {
        PartDrag[] allParts = FindObjectsOfType<PartDrag>();

        Transform bestMySnap = null;
        Transform bestOtherSnap = null;
        PartDrag bestOtherPart = null;
        float bestDistance = snapDistance;

        foreach (var other in allParts)
        {
            if (other == this) continue;

            foreach (Transform mySnap in snapPointsRoot)
            {
                foreach (Transform otherSnap in other.snapPointsRoot)
                {
                    float dist = Vector3.Distance(
                        mySnap.position,
                        otherSnap.position
                    );

                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestMySnap = mySnap;
                        bestOtherSnap = otherSnap;
                        bestOtherPart = other;
                    }
                }
            }
        }

        if (bestMySnap == null || bestOtherSnap == null) return;

        if (bestOtherPart.role == PartRole.Payload)
        {
            if (bestOtherPart.connectedPart != null || bestOtherPart.attachedParts.Count > 0)
            {
                Debug.LogWarning($"Payload already has a connection. Detach the existing connection first.");
                return;
            }
        }

        Vector3 delta = bestOtherSnap.position - bestMySnap.position;
        transform.position += delta;

        RegisterAttachment(bestOtherPart);
        OnAssemblyChanged?.Invoke();
        PlaySnapSound();

        mySnapUsed = bestMySnap;
        otherSnapUsed = bestOtherSnap;

        Debug.Log(
            $"{name} snapped to {bestOtherPart.name} " +
            $"via {mySnapUsed.name} -> {otherSnapUsed.name}"
        );
    }

    void PlaySnapSound()
    {
        if (rocketAudio != null && snapClip != null)
            rocketAudio.PlayOneShot(snapClip);
    }

    void RegisterAttachment(PartDrag other)
    {
        if (role == PartRole.Payload && connectedPart != null && connectedPart != other)
        {
            Debug.LogWarning($"Payload already connected to {connectedPart.name}. Cannot connect to {other.name}.");
            return;
        }

        if (connectedPart != null && connectedPart != other)
        {
            PartDrag oldConnection = connectedPart;

            oldConnection.attachedParts.Remove(this);

            connectedPart = null;

            Debug.Log($"{name} detached from {oldConnection.name}");
        }

        connectedPart = other;

        if (!other.attachedParts.Contains(this))
            other.attachedParts.Add(this);
    }

    void DetachCompletely()
    {
        if (connectedPart != null)
        {
            connectedPart.attachedParts.Remove(this);
            connectedPart = null;
        }

        foreach (var attached in new List<PartDrag>(attachedParts))
        {
            if (attached != null && attached.connectedPart == this)
            {
                attached.connectedPart = null;
            }
        }
        attachedParts.Clear();

        mySnapUsed = null;
        otherSnapUsed = null;
    }

    public void TrySnapExternally()
    {
        if (locked) return;
        TrySnap();
    }

    public static bool ValidateAssembly(out string error)
    {
        error = null;

        int count = assemblyParts.Count;
        if (count != 4 && count != 7 && count != 10)
        {
            error = "Rocket components are not connected properly. Rocket components are connected at wrong position. Check Blue Print For help for required configuration.";
            return false;
        }

        PartDrag payload = GetPayload();
        if (payload == null)
        {
            error = "Payload missing";
            return false;
        }

        var graph = BuildGraph();

        foreach (var p in assemblyParts)
        {
            if (graph[p].Count == 0)
            {
                error = $"Rocket components are not connected properly. {GetRequiredPartsList(count)} are required for launch";
                return false;
            }
        }

        if (graph[payload].Count != 1)
        {
            error = "Rocket components are connected at wrong position. Check Blue Print For help";

            Debug.LogError($"Payload connections:");
            foreach (var conn in graph[payload])
            {
                Debug.LogError($"  - {conn.name} ({conn.role})");
            }

            return false;
        }

        List<PartRole> chain = GetMainChain();

        foreach (var r in chain)
        {
            if (r == PartRole.SideTank || r == PartRole.SideThruster)
            {
                error = "Rocket components are connected at wrong position. Check Blue Print For help";
                return false;
            }
        }

        if (count == 4)
        {
            PartDrag.pauseBeforeFrame = 180;
            PartDrag.resultNumber = 1;
            resultNumber = 1;
            return Match(chain, new[]
            {
                PartRole.Payload,
                PartRole.Separator2,
                PartRole.CoreTank,
                PartRole.CoreThruster
            }, out error);
        }

        if (count == 7)
        {
            {
                PartDrag.pauseBeforeFrame = 610 ;
                PartDrag.resultNumber = 2;
            }
            if (chain.Count < 6)
            {
                error = $"Chain too short: {chain.Count}";
                return false;
            }
            resultNumber = 2;
            if (!Match(chain.GetRange(0, 6), new[]
            {
                PartRole.Payload,
                PartRole.Separator2,
                PartRole.CoreTank,
                PartRole.CoreThruster,
                PartRole.Separator1,
                PartRole.LiquidTank
            }, out error))
                return false;

            PartRole last = chain[6];
            if (last != PartRole.LargeThruster)
            {
                error = "Invalid final thruster";
                return false;
            }

            return true;
        }

        if (count == 10)
        {
            {
                PartDrag.pauseBeforeFrame = 510;
                PartDrag.resultNumber = 3;
            }
            resultNumber = 3;
            if (!Match(chain, new[]
            {
                PartRole.Payload,
                PartRole.Separator2,
                PartRole.CoreTank,
                PartRole.CoreThruster,
                PartRole.Separator1,
                PartRole.LiquidTank,
                PartRole.LargeThruster
            }, out error))
                return false;

            var sideTanks = assemblyParts.FindAll(p => p.role == PartRole.SideTank);
            var sideThrusters = assemblyParts.FindAll(p => p.role == PartRole.SideThruster);

            if (sideTanks.Count != 1 || sideThrusters.Count != 2)
            {
                error = "Invalid side booster count";
                return false;
            }

            PartDrag sideTank = sideTanks[0];

            int thrusterLinks = 0;
            bool mainLink = false;

            foreach (var p in graph[sideTank])
            {
                if (p.role == PartRole.SideThruster) thrusterLinks++;
                else mainLink = true;
            }

            if (!mainLink || thrusterLinks != 2)
            {
                error = "Side tank topology invalid";
                return false;
            }

            return true;
        }

        return false;
    }

    static Dictionary<PartDrag, List<PartDrag>> BuildGraph()
    {
        var graph = new Dictionary<PartDrag, List<PartDrag>>();

        foreach (var p in assemblyParts)
        {
            graph[p] = new List<PartDrag>();
        }

        foreach (var p in assemblyParts)
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

    static string GetRequiredPartsList(int count)
    {
        if (count == 4)
            return "Payload, Separator2, CoreTank, CoreThruster";

        if (count == 7)
            return "Payload, Separator2, CoreTank, CoreThruster, Separator1, LiquidTank, LargeThruster";

        if (count == 10)
            return "Full booster configuration with SideTank and SideThrusters";

        return "Required parts";
    }


    static bool Match(
    List<PartRole> chain,
    PartRole[] expected,
    out string error)
    {
        // ❌ Wrong number of parts in main chain → blueprint issue
        if (chain.Count != expected.Length)
        {
            error = "Rocket parts are incorrect. Check Blue Print For help";
            return false;
        }

        // ❌ Wrong order / wrong parts
        for (int i = 0; i < expected.Length; i++)
        {
            if (chain[i] != expected[i])
            {
                error = "Rocket parts are incorrect. Check Blue Print For help";
                return false;
            }
        }

        error = null;
        return true;
    }

}
