using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public enum MOVE_STATE {
        SPRINTING,
        RUNNING,
        WALKING,
        CROUCHING,
        CRAWLING,
        IDLE
    }

    /*
     * Input modes: Toggle, Hold
     * Toggle: Press once to toggle on, press again to toggle off
     * Hold: Press and hold to keep on, release to turn off
     *
     * Example: Sprinting
     * Toggle: Press once to start sprinting, press again to stop sprinting
     * Hold: Press and hold to sprint, release to stop sprinting
     */
    public enum INPUT_MODE {
        TOGGLE,
        HOLD
    }

    private Rigidbody rb;
    private PlayerInput playerInput;

    [Header("Player Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float lookSensitivity = 0.1f;

    [Header("States & Events")]
    public MOVE_STATE moveState = MOVE_STATE.RUNNING;
    public Func<bool> preInteract;
    public Action postInteract;
    public Func<bool> preInteractSecondary;
    public Action postInteractSecondary;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isJumping;
    private bool IsGrounded => Physics.Raycast(transform.position, Vector3.down, 1.1f);

    [Header("Input Modes")]
    public INPUT_MODE crouchMode = INPUT_MODE.TOGGLE;
    public INPUT_MODE sprintMode = INPUT_MODE.HOLD;
    public INPUT_MODE walkMode = INPUT_MODE.HOLD;

    private bool isCrouching = false;
    private bool isSprinting = false;
    private bool isWalking = false;

    public static bool LockCursor {
        get => Cursor.lockState == CursorLockMode.Locked;
        set => Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
    }

    private void Start() {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();

        LockCursor = true;
    }

    private void FixedUpdate() {
        HandleMovement();
        HandleJump();
    }

    private void Update() {
        HandleLook();
    }

    private void HandleJump() {
        if (isJumping) {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isJumping = false;
        }
    }

    private float GetSpeedMultiplier() {
        return moveState switch {
            MOVE_STATE.SPRINTING => 1.5f,
            MOVE_STATE.WALKING => 0.7f,
            MOVE_STATE.CROUCHING => 0.5f,
            MOVE_STATE.CRAWLING => 0.3f,
            _ => 1f
        };
    }

    private void HandleMovement() {
        float speedMultiplier = GetSpeedMultiplier();
        Vector3 moveDirection = transform.forward * moveInput.y + transform.right * moveInput.x;
        Vector3 movement = moveDirection.normalized * (moveSpeed * speedMultiplier);
        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
    }


    private void HandleLook() {
        float lookX = lookInput.x * lookSensitivity;
        float lookY = lookInput.y * lookSensitivity;

        transform.Rotate(Vector3.up * lookX);
        playerInput.camera.transform.Rotate(Vector3.left * lookY);
    }

    private void OnMove(InputValue value) {
        moveInput = value.Get<Vector2>();
    }

    private void OnLook(InputValue value) {
        lookInput = value.Get<Vector2>();
    }

    private void OnJump(InputValue value) {
        if (value.isPressed && IsGrounded) {
            isJumping = true;
        }
    }

    private void OnInteract(InputValue value) {
        if (value.isPressed) {
            bool passedPre = true;
            foreach (Delegate func in preInteract.GetInvocationList()) {
                Func<bool> action = (Func<bool>)func;
                if (!action()) passedPre = false;
            }
            if (passedPre) postInteract?.Invoke();
        }
    }

    private void OnInteractSecondary(InputValue value) {
        if (value.isPressed) {
            bool passedPre = true;
            foreach (Delegate func in preInteractSecondary.GetInvocationList()) {
                Func<bool> action = (Func<bool>)func;
                if (!action()) passedPre = false;
            }
            if (passedPre) postInteractSecondary?.Invoke();
        }
    }

    private void OnCrouch(InputValue value) {
        if (value.isPressed) {
            if (IsGrounded) moveState = MOVE_STATE.CROUCHING;
        }
        else {
            moveState = MOVE_STATE.RUNNING;
        }
    }

    private void OnSprint(InputValue value) {
        if (value.isPressed) {
            moveState = MOVE_STATE.SPRINTING;
        }
        else {
            moveState = MOVE_STATE.RUNNING;
        }
    }

    private void OnWalk(InputValue value) {
        if (value.isPressed) {
            moveState = MOVE_STATE.WALKING;
        }
        else {
            moveState = MOVE_STATE.RUNNING;
        }
    }
}