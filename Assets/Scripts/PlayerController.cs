using UnityEngine;

public class PlayerController : MonoBehaviour
{

    public CharacterController controller;
    public float speed;
    public Transform GroundCheck;
    public LayerMask groundMask;
    public bool isGrounded;

    void Awake()
    {
        speed = 5f;
        controller = GetComponent<CharacterController>();
        GroundCheck = GameObject.Find("GroundCheck").transform;
        
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        controller.Move(move * speed * Time.deltaTime);

        isGrounded = Physics.CheckSphere(GroundCheck.position, 0.4f, groundMask);
    }
}
