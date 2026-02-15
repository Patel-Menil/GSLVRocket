using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RocketManager : MonoBehaviour
{
    private GameObject SelectedObj;

    void Start()
    {
        SelectedObj = null;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("0");
            if (SelectedObj == null)
            {
                Debug.Log("1");
                RaycastHit hit = CastRay();

                if (hit.collider != null)
                {
                    if (!hit.collider.CompareTag("Model"))
                    {
                        return;
                    }

                    SelectedObj = hit.collider.gameObject;
                    Debug.Log("Hit");
                }
            }
        }

        if (SelectedObj != null)
        {
            Debug.Log("3");
            Vector3 position = new Vector3(
                Input.mousePosition.x,
                Input.mousePosition.y,
                Camera.main.WorldToScreenPoint(SelectedObj.transform.position).z);

            Vector3 worldPostion = Camera.main.ScreenToWorldPoint(position);
            SelectedObj.transform.position = worldPostion;
        }
    }

    protected RaycastHit CastRay()
    {
        Vector3 screenMousePosFar = new Vector3(
            Input.mousePosition.x,
            Input.mousePosition.y,
            Camera.main.farClipPlane);

        Vector3 screenMousePosNear = new Vector3(
            Input.mousePosition.x,
            Input.mousePosition.y,
            Camera.main.nearClipPlane);

        Vector3 worldMousePosFar = Camera.main.ScreenToWorldPoint(screenMousePosFar);
        Vector3 worldMousePosNear = Camera.main.ScreenToWorldPoint(screenMousePosNear);

        RaycastHit hit;
        Physics.Raycast(worldMousePosNear, worldMousePosFar - worldMousePosNear, out hit);
        return hit;
    }
}
