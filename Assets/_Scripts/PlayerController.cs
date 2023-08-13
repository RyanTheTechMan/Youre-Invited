using System;
using System.Linq;
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
        if (!isJumping) return;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        isJumping = false;
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
        Vector3 moveDirection = transform.forward * moveInput.y + transform.right * moveInput.x;
        Vector3 movement = moveDirection.normalized * (moveSpeed * GetSpeedMultiplier());
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

    private void OnInteract(InputValue value) => HandleInteraction(value, PreInteract, PostInteract);
    private void OnInteractSecondary(InputValue value) => HandleInteraction(value, PreInteractSecondary, PostInteractSecondary);

    private void OnCrouch(InputValue value) => HandleModeAction(value, crouchMode, MOVE_STATE.CROUCHING);
    private void OnSprint(InputValue value) => HandleModeAction(value, sprintMode, MOVE_STATE.SPRINTING);
    private void OnWalk(InputValue value) => HandleModeAction(value, walkMode, MOVE_STATE.WALKING);

    private void HandleInteraction(InputValue value, Func<bool> preCheck, Action action) {
        if (!value.isPressed) return;
        if (preCheck == null || preCheck.GetInvocationList().All(func => ((Func<bool>)func)()))
            action?.Invoke();
    }

    private void HandleModeAction(InputValue value, INPUT_MODE mode, MOVE_STATE desiredState) {
        moveState = mode switch {
            INPUT_MODE.TOGGLE when value.isPressed => moveState == desiredState ? MOVE_STATE.RUNNING : desiredState,
            INPUT_MODE.HOLD => value.isPressed ? desiredState : MOVE_STATE.RUNNING,
            _ => moveState
        };
    }
}