using UnityEngine;

public class TempMasterController : MonoBehaviour
{
    // Assigned in editor
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private CapsuleCollider capsuleCollider;

    private float curSpeed = 2.5f;
    [SerializeField] private float defaultSpeed = 2.5f;
    [SerializeField] private float runSpeed = 5f;

    private Vector3 moveInput;
    private Vector2 mouseInput;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        // gets wasd controls... get move vector... +z is forward and +x is to right
        // gets mouse input for camera positoniong
        // assigns to variables to set for lateUpdate to handle..;.

        // gets wasd controls... get move vector... +z is forward and +x is to right
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // gets mouse input for camera positioning
        Vector2 move = new Vector2(0f, 0f);
        if (Input.GetKey(KeyCode.W))
        {
            move.x += 1f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            move.x -= 1f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            move.y += 1f;
        }
        if (Input.GetKey(KeyCode.A))
        {
            move.y -= 1f;
        }

        moveInput = new Vector3(move.y, 0f, move.x);

        if (Input.GetKey(KeyCode.LeftShift))
        {
            curSpeed = runSpeed;
        } 
        else if (curSpeed !=  defaultSpeed)
        {
            curSpeed = defaultSpeed;
        }


        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        mouseInput = new Vector2(mouseX, mouseY);



    }

    private void FixedUpdate()
    {
        // rotate camera based on mouse input
        transform.Rotate(0f, mouseInput.x, 0f);
        if (playerCamera != null)
        {
            playerCamera.transform.Rotate(-mouseInput.y, 0f, 0f);
        }

        // apply movement (for now just simple transform, physics can be added)
        // apply movement via velocity
        Vector3 moveDir = transform.TransformDirection(moveInput.normalized);
        Vector3 velocity = moveDir * curSpeed;
        velocity.y = rb.linearVelocity.y; // preserve vertical velocity (gravity/jumping)
        rb.linearVelocity = velocity;

        
    }
}
