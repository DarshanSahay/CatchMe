using System;
using System.Collections;
using PurrNet;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] CharacterController controller;
    [SerializeField] Animator animator;
    [SerializeField] public float currentSpeed = 5f;
    [SerializeField] public float moveSpeed = 5f;
    [SerializeField] float turnSpeed = 720f; // degrees per second
    [SerializeField] float jumpHeight = 2f;
    [SerializeField] float gravity = -9.8f;

    [SerializeField] bool isGrounded;
    [SerializeField] bool isJumping;
    [SerializeField] GameObject dustEffect;
    [SerializeField] GameObject jumpEffect;

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
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }
    }

    void Update()
    {
        // Ground check using CharacterController
        isGrounded = controller.isGrounded;
        animator.SetBool("isGrounded", isGrounded);

        if (isGrounded && velocity.y < 0f)
        {
            // Small downward force to keep controller grounded
            velocity.y = -2f;
            isJumping = false;
            //Time.timeScale = 1f;
        }


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
        Vector3 horizontalMove = movement * currentSpeed;

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
            ParticleSystem.EmissionModule module = dustEffect.GetComponent<ParticleSystem>().emission;
            module.enabled = false;
            //dustEffect.SetActive(false);
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

        currentNewVelocity = controller.velocity.y;
        currentVelocity = controller.velocity.y;
    }

    void SetInitialJumpVelocity()
    {
        initialVelocity = Mathf.Sqrt(2 * (Mathf.Abs(Physics.gravity.y) * jumpHeight));
        isJumping = true;
    }

    private IEnumerator OnTriggerEnter(Collider other)
    {
        if (isGrounded)
            yield return null;

        if (other.CompareTag("Ground"))
        {
            animator.SetTrigger("aboutToLand");
        }

        yield return new WaitWhile(() => !isGrounded);

        jumpEffect.SetActive(true);

        yield return new WaitForSeconds(0.1f);

        ParticleSystem.EmissionModule module = dustEffect.GetComponent<ParticleSystem>().emission;
        module.enabled = true;
        //dustEffect.SetActive(true);
    }

    private void OnDrawGizmos()
    {
        // Keep gizmo to show forward and ground direction (optional)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.up * 0.1f + Vector3.down * 0.3f);
    }
}