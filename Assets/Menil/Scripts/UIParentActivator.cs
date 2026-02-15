using UnityEngine;

public class UIParentActivator : MonoBehaviour
{
    private GameObject[] cachedChildren;
    private bool hasEnabledOnce = false;

    void Awake()
    {
        int count = transform.childCount;
        cachedChildren = new GameObject[count];

        for (int i = 0; i < count; i++)
        {
            cachedChildren[i] = transform.GetChild(i).gameObject;
        }
    }

    void OnEnable()
    {
        hasEnabledOnce = true;

        foreach (var child in cachedChildren)
        {
            if (child != null)
                child.SetActive(true);
        }
    }

    void OnDisable()
    {
        if (!hasEnabledOnce)
            return;

        foreach (var child in cachedChildren)
        {
            if (child != null)
                child.SetActive(false);
        }
    }
}
