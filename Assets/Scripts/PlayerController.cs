using UnityEngine;

public class PlayerController : MonoBehaviour
{

    public CharacterController controller;
    public float speed = 15f;
    public Transform GroundCheck;
    public LayerMask groundMask;
    public bool isGrounded;

    public float gravity = -9.81f;
    public float jumpHeight = 3f;
    private Vector3 velocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        GroundCheck = GameObject.Find("GroundCheck").transform;
        
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        isGrounded = Physics.CheckSphere(GroundCheck.position, 0.4f, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        controller.Move(move * speed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
        
    }
}
