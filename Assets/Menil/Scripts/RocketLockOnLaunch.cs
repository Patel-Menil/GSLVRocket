using UnityEngine;

public class RocketLockOnLaunch : MonoBehaviour
{
    private bool locked = false;

    public void LockRocket()
    {
        if (locked) return;
        locked = true;

        PartDrag[] allParts = FindObjectsOfType<PartDrag>();

        foreach (PartDrag part in allParts)
        {
            part.transform.SetParent(transform, true);
            part.enabled = false;

            Collider col = part.GetComponent<Collider>();
            if (col != null)
                col.enabled = false;
        }

        Debug.Log("Rocket locked, drag disabled.");
    }
}
