using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(0, 5, -10);
    [SerializeField] float minPitch = -80f;
    [SerializeField] float maxPitch = 80f;

    float pitch = 0f;

    void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.position + target.TransformDirection(offset);

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        transform.rotation = Quaternion.Euler(pitch, target.eulerAngles.y, 0);
    }

    // Called by Player to pass mouse Y delta
    public void AddPitch(float delta, float sensitivity)
    {
        pitch -= delta * sensitivity; // subtract so moving mouse up looks up
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }
}