using UnityEngine;

public class CameraFocus : MonoBehaviour
{
    [Header("References")]
    public Transform rocket;

    [Header("Motion")]
    public float cameraAccelerationFactor = 0.9f;

    private bool focusLocked = false;
    private float velocity = 0f;
    private float rocketAcceleration = 0f;

    public void Focus(float rocketAccel)
    {
        Debug.Log("Camera Focus CALLED");
        rocketAcceleration = rocketAccel * cameraAccelerationFactor;
        focusLocked = true;
        LookAtRocket();
    }

    void LateUpdate()
    {
        if (!focusLocked || rocket == null) return;

        velocity += rocketAcceleration * Time.deltaTime;
        transform.position += Vector3.up * velocity * Time.deltaTime;
        LookAtRocket();
    }

    public void StopFocus()
    {
        focusLocked = false;
        velocity = 0f;
        Debug.Log("Camera focus released");
    }

    void LookAtRocket()
    {
        Vector3 direction = rocket.position - transform.position;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }
}
