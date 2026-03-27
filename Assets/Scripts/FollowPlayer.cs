using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    public GameObject player;
    public float damping = 1f; //Amortecimento
    private Vector3 offset;
    private float currentAngle;
    private float desiredAngle;
    private float angle;
    private float rotationSpeed = 60.0f;
    private float turning;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        offset = transform.position - player.transform.position; 
    }


    // Update is called once per frame
    void LateUpdate() //O código é o último a ser reenderizado antes do run. Para movimentos da câmara
    {
        currentAngle = transform.eulerAngles.y;
        desiredAngle = player.transform.eulerAngles.y;
        angle = Mathf.LerpAngle(currentAngle, desiredAngle, Time.deltaTime * damping);
        turning = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

        Quaternion rotation = Quaternion.Euler(0, angle, 0);
        transform.position = player.transform.position - (rotation * (-offset));
        transform.LookAt(player.transform.position);
    }
}
