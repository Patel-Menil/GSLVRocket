using UnityEngine;

public class RocketAutoAligner : MonoBehaviour
{
    [Header("References")]
    public Transform platformRoot;
    public Transform platformTopPoint;

    public float dist = 26.0f;
    public float x;

    public void AlignRocketIntuitively()
    {
        Bounds rocketBounds = GetCombinedBounds(transform);

        float rocketBottomY = rocketBounds.min.y;
        float platformTopY = GetHighestY(platformRoot);

        float deltaY = platformTopY - rocketBottomY - dist;

        float deltaX = platformTopPoint.position.x - rocketBounds.center.x;

        transform.position += new Vector3(deltaX - x, deltaY, 0f);

        Debug.Log("Rocket auto-aligned intuitively (X + Y).");
    }

    float GetLowestY(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return root.position.y;

        float minY = float.MaxValue;

        foreach (Renderer r in renderers)
            minY = Mathf.Min(minY, r.bounds.min.y);

        return minY;
    }

    float GetHighestY(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return root.position.y;

        float maxY = float.MinValue;

        foreach (Renderer r in renderers)
            maxY = Mathf.Max(maxY, r.bounds.max.y);

        return maxY;
    }

    Bounds GetCombinedBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;

        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);

        return bounds;
    }
}
