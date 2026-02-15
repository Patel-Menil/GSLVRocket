using UnityEngine;

public class DeleteOnArea : MonoBehaviour
{
    
    public float minX = 25f;
    public float maxX = 40f;

    
    void Update()
    {
        Vector3 pos = transform.position;

        if (pos.x >= minX && pos.x <= maxX)
        {
            Destroy(gameObject);
        }
    }
}