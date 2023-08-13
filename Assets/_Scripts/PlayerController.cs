using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    /*
     * Move states: Sprinting, Running, Walking, Crouching, Crawling, Idle
     * Sprinting: Moving at max speed
     * Running: Moving at normal speed
     * Walking: Moving at half speed
     * Crouching: Moving at 1/3 speed
     * Crawling: Moving at 1/5 speed
     * Idle: Not moving
     */
    public enum MOVE_STATE { SPRINTING, RUNNING, WALKING, CROUCHING, CRAWLING, IDLE }

    /*
     * Input modes: Toggle, Hold
     * Toggle: Press once to toggle on, press again to toggle off
     * Hold: Press and hold to keep on, release to turn off
     *
     * Example: Sprinting
     * Toggle: Press once to start sprinting, press again to stop sprinting
     * Hold: Press and hold to sprint, release to stop sprinting
     */
    public enum INPUT_MODE { TOGGLE, HOLD }

    private Rigidbody rb;
    private PlayerInput playerInput;

    [Header("Player Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float lookSensitivity = 0.1f;

    [Header("States & Events")]
    public MOVE_STATE moveState = MOVE_STATE.RUNNING;
    public Func<bool> PreInteract;
    public Action PostInteract;
    public Func<bool> PreInteractSecondary;
    public Action PostInteractSecondary;

    [Header("Input Modes")]
    public INPUT_MODE crouchMode = INPUT_MODE.TOGGLE;
    public INPUT_MODE sprintMode = INPUT_MODE.HOLD;
    public INPUT_MODE walkMode = INPUT_MODE.HOLD;

    private bool isCrouching;
    private bool isSprinting;
    private bool isWalking;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isJumping;
    private bool IsGrounded => Physics.Raycast(transform.position, Vector3.down, 1.1f);

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
            MOVE_STATE.SPRINTING => 2f,
            MOVE_STATE.WALKING => 0.5f,
            MOVE_STATE.CROUCHING => 0.33f,
            MOVE_STATE.CRAWLING => 0.2f,
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
            foreach (Delegate func in PreInteract?.GetInvocationList() ?? Array.Empty<Delegate>()) {
                Func<bool> action = (Func<bool>)func;
                if (!action()) passedPre = false;
            }
            if (passedPre) PostInteract?.Invoke();
        }
    }

    private void OnInteractSecondary(InputValue value) {
        if (value.isPressed) {
            bool passedPre = true;
            foreach (Delegate func in PreInteractSecondary?.GetInvocationList() ?? Array.Empty<Delegate>()) {
                Func<bool> action = (Func<bool>)func;
                if (!action()) passedPre = false;
            }
            if (passedPre) PostInteractSecondary?.Invoke();
        }
    }

    private void OnCrouch(InputValue value) {
        switch (crouchMode) {
            case INPUT_MODE.TOGGLE when value.isPressed:
                isCrouching = !isCrouching;
                moveState = isCrouching ? MOVE_STATE.CROUCHING : MOVE_STATE.RUNNING;
                break;
            case INPUT_MODE.HOLD:
                moveState = value.isPressed && IsGrounded ? MOVE_STATE.CROUCHING : MOVE_STATE.RUNNING;
                break;
        }
    }

    private void OnSprint(InputValue value) {
        switch (sprintMode) {
            case INPUT_MODE.TOGGLE when value.isPressed:
                isSprinting = !isSprinting;
                moveState = isSprinting ? MOVE_STATE.SPRINTING : MOVE_STATE.RUNNING;
                break;
            case INPUT_MODE.HOLD:
                moveState = value.isPressed ? MOVE_STATE.SPRINTING : MOVE_STATE.RUNNING;
                break;
        }
    }

    private void OnWalk(InputValue value) {
        switch (walkMode) {
            case INPUT_MODE.TOGGLE when value.isPressed:
                isWalking = !isWalking;
                moveState = isWalking ? MOVE_STATE.WALKING : MOVE_STATE.RUNNING;
                break;
            case INPUT_MODE.HOLD:
                moveState = value.isPressed ? MOVE_STATE.WALKING : MOVE_STATE.RUNNING;
                break;
        }
    }
}