using UnityEngine;
using UnityEngine.InputSystem;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public float playerSpeed = 10.0f;
    public float playerRotationSpeed = 50.0f;
    public float jumpForce = 3f;

    private float vertical;
    private float horizontal;
    private float turning;
    private bool onGround;

    private Rigidbody rb;

    [SerializeField]
    private InputAction jump;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb= GetComponent<Rigidbody>();
    }

    void FixedUpdate()  
    {
        horizontal = Input.GetAxis("Horizontal") * playerSpeed * Time.deltaTime;
        vertical = Input.GetAxis("Vertical") * playerSpeed * Time.deltaTime; 
        turning = Input.GetAxis("Mouse X") * playerRotationSpeed * Time.deltaTime;

        rb.MovePosition(rb.position + transform.forward * vertical + transform.right * horizontal);
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0, turning, 0));

        if(jump.IsPressed() && onGround)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            onGround = false;
        }
    }

    private void OnEnable()
    {
        jump.Enable();
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            onGround = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
