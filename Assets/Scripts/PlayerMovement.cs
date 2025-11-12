//using System;
//using PurrNet;
//using Unity.Cinemachine;
//using UnityEngine;
//using UnityEngine.EventSystems;

//public class PlayerMovement : NetworkBehaviour
//{
//    [SerializeField] Rigidbody rb_player;
//    [SerializeField] Animator animator;
//    [SerializeField] float moveSpeed;
//    [SerializeField] float turnSpeed;
//    [SerializeField] bool canJump;
//    [SerializeField] bool isGrounded;
//    [SerializeField] bool isJumping;
//    [SerializeField] Transform groundCheckOrigin;
//    [SerializeField] LayerMask groundLayer;

//    [SerializeField] float currentNewVelocity = 0f;
//    [SerializeField] float currentVelocity = 0f;
//    [SerializeField] float gravity = -9.8f;
//    [SerializeField] float jumpHeight = 2f;
//    [SerializeField] float initialVelocity = 0f;
//    [SerializeField] float timeSinceJump = 0f;

//    [SerializeField] Vector3 movement;

//    protected override void OnSpawned()
//    {
//        base.OnSpawned();

//        enabled = isOwner;

//        if(isOwner)
//        {
//            GameManager.Instance.GetCamera().gameObject.SetActive(true);
//            GameManager.Instance.GetCamera().SetTarget(transform);
//        }
//    }

//    private void Start()
//    {
//        SetInitialJumpVelocity();
//    }

//    void Update()
//    {
//        float horizontalX = Input.GetAxisRaw("Horizontal");
//        float horizontalY = Input.GetAxisRaw("Vertical");

//        movement = new Vector3(horizontalX, 0f, horizontalY);
//        if (movement.sqrMagnitude > 1f) movement.Normalize();

//        if (movement.sqrMagnitude != 0)
//        {
//            Quaternion targetRotation = Quaternion.LookRotation(movement, Vector3.up);
//            Quaternion newRot = Quaternion.RotateTowards(rb_player.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);
//            rb_player.MoveRotation(newRot);
//        }

//        if (movement.magnitude != 0)
//        {
//            rb_player.MovePosition(transform.position + movement * Time.deltaTime * moveSpeed);       
//        }

//        animator.SetBool("isRunning", movement.magnitude > 0.1f);

//        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
//        {
//            timeSinceJump = 0f;
//            canJump = true;
//        }

//        currentNewVelocity = rb_player.linearVelocity.y;
//    }

//    private void FixedUpdate()
//    {
//        isGrounded = Physics.Raycast(groundCheckOrigin.position, Vector3.down, 0.3f, groundLayer);

//        if (!isGrounded)
//        {
//            //canJump = false;
//            timeSinceJump += Time.fixedDeltaTime;
//        }

//        if (canJump)
//        {
//            SetInitialJumpVelocity();
//            currentVelocity = initialVelocity - (Mathf.Abs(gravity) * 2 * timeSinceJump);
//            rb_player.linearVelocity = new Vector3(rb_player.linearVelocity.x, currentVelocity, rb_player.linearVelocity.z);
//        }
//    }

//    void SetInitialJumpVelocity()
//    {
//        initialVelocity = Mathf.Sqrt(2 * (Mathf.Abs(Physics.gravity.y) * jumpHeight));
//        isJumping = true;
//    }

//    private void OnDrawGizmos()
//    {
//        Gizmos.color = Color.red;
//        Gizmos.DrawLine(groundCheckOrigin.position, groundCheckOrigin.position + Vector3.down * 0.3f);
//    }
//}

using System;
using PurrNet;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] CharacterController controller;
    [SerializeField] Animator animator;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float turnSpeed = 720f; // degrees per second
    [SerializeField] float jumpHeight = 2f;
    [SerializeField] float gravity = -9.8f;

    [SerializeField] bool isGrounded;
    [SerializeField] bool isJumping;

    // Debug / telemetry
    [SerializeField] float currentNewVelocity = 0f;
    [SerializeField] float currentVelocity = 0f;
    [SerializeField] float initialVelocity;
    [SerializeField] float timeSinceJump = 0f;

    Vector3 velocity;        // stores vertical velocity
    Vector3 movement;        // horizontal movement vector (x,z)

    protected override void OnSpawned()
    {
        base.OnSpawned();

        enabled = isOwner;

        if (isOwner)
        {
            GameManager.Instance.GetCamera().gameObject.SetActive(true);
            GameManager.Instance.GetCamera().SetTarget(transform);
        }
    }

    private void Start()
    {
        // Ensure CharacterController is present
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }
    }

    void Update()
    {
        float horizontalX = Input.GetAxisRaw("Horizontal");
        float horizontalY = Input.GetAxisRaw("Vertical");

        movement = new Vector3(horizontalX, 0f, horizontalY);
        if (movement.sqrMagnitude > 1f) movement.Normalize();
        animator.SetFloat("isMoving", movement.magnitude);

        // Rotation: face movement direction (world-space)
        if (movement.sqrMagnitude > 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        // Horizontal move (character controller)
        Vector3 horizontalMove = movement * moveSpeed;

        // Ground check using CharacterController
        isGrounded = controller.isGrounded;
        animator.SetBool("isGrounded", isGrounded);

        if (isGrounded && velocity.y < 0f)
        {
            // Small downward force to keep controller grounded
            velocity.y = -2f;
            isJumping = false;
            Time.timeScale = 1f;
        }

        if (!isGrounded)
        {
            timeSinceJump += Time.deltaTime;
            currentVelocity = initialVelocity - (Mathf.Abs(gravity) * 2 * timeSinceJump);
            velocity.y = currentVelocity;
        }

        // Jump input
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            //Time.timeScale = 0.2f;
            timeSinceJump = 0f;
            SetInitialJumpVelocity();
            velocity.y = Mathf.Sqrt(0.2f * -2f * gravity);
            isJumping = true;
            animator.SetTrigger("isJumping");
        }

        // Apply gravity every frame
        velocity.y += gravity * Time.deltaTime;
        animator.SetFloat("yVelocity", velocity.y);

        // Move controller (horizontal + vertical)
        Vector3 finalMove = horizontalMove + new Vector3(0f, velocity.y, 0f);
        controller.Move(finalMove * Time.deltaTime);

        // Animator
        animator.SetBool("isRunning", movement.magnitude > 0.1f);

        // Debug values (map to your previous fields)
        currentNewVelocity = controller.velocity.y;
        currentVelocity = controller.velocity.y;
    }

    void SetInitialJumpVelocity()
    {
        initialVelocity = Mathf.Sqrt(2 * (Mathf.Abs(Physics.gravity.y) * jumpHeight));
        isJumping = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isGrounded)
            return;

        if (other.CompareTag("Ground"))
        {
            animator.SetTrigger("aboutToLand");
        }
    }

    private void OnDrawGizmos()
    {
        // Keep gizmo to show forward and ground direction (optional)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.up * 0.1f + Vector3.down * 0.3f);
    }
}