// Player.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    PlayerControls input;
    Vector2 move;
    Vector2 look;

    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float lookSensitivity = 0.1f;
    [SerializeField] CameraFollow cameraFollow;

    void Awake()
    {
        input = new PlayerControls();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnEnable() { input.Enable(); }
    void OnDisable() { input.Disable(); }

    void Update()
    {
        move = input.Player.Move.ReadValue<Vector2>();
        look = input.Player.Look.ReadValue<Vector2>();

        // Vertical input
        var keyboard = Keyboard.current;
        float vertical = 0f;
        if (keyboard.spaceKey.isPressed) vertical = 1f;
        if (keyboard.leftShiftKey.isPressed) vertical = -1f;

        Vector3 direction = transform.right * move.x
                          + transform.forward * move.y
                          + Vector3.up * vertical;  // add vertical

        transform.Translate(direction * moveSpeed * Time.deltaTime, Space.World);

        transform.Rotate(0, look.x * lookSensitivity, 0);
        cameraFollow.AddPitch(look.y, lookSensitivity);
    }
}