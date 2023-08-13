using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    /*
     * Move states
     *
     * Defines the different move states of the player.
     */
    public enum MOVE_STATE { SPRINTING, RUNNING, WALKING, CROUCHING, CRAWLING, IDLE }

    /*
     * Input modes: Toggle, Hold
     * Toggle: Press once to toggle on, press again to toggle off
     * Hold: Press and hold to keep on, release to turn off
     */
    public enum INPUT_MODE { TOGGLE, HOLD }

    private Rigidbody _rb;
    private PlayerInput _playerInput;

    [Header("Player Settings")]
    public float moveSpeed = 5f;         // Base speed for player movement.
    public float jumpForce = 5f;         // Force applied when player jumps.
    public float lookSensitivity = 0.1f; // Sensitivity for camera movement.

    [Header("States & Events")]
    public MOVE_STATE moveState = MOVE_STATE.RUNNING; // Default movement state.

    // Interaction delegates.
    public Func<bool> PreInteract;           // Checks if primary interaction is allowed.
    public Action PostInteract;              // Primary interaction method.
    public Func<bool> PreInteractSecondary;  // Checks if secondary interaction is allowed.
    public Action PostInteractSecondary;     // Secondary interaction method.

    [Header("Input Modes")]
    // Input modes for actions.
    public INPUT_MODE crouchMode = INPUT_MODE.TOGGLE;
    public INPUT_MODE sprintMode = INPUT_MODE.HOLD;
    public INPUT_MODE walkMode = INPUT_MODE.HOLD;

    // Movement and look directions.
    private Vector2 _moveInput;
    private Vector2 _lookInput;

    private bool _isJumping; // Indicates if the player is currently attempting to jump.

    private bool IsGrounded => Physics.Raycast(transform.position, Vector3.down, 1.1f); // Checks if the player is touching the ground.

    public static bool LockCursor {
        get => Cursor.lockState == CursorLockMode.Locked;
        set => Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
    }

    private void Start() {
        _rb = GetComponent<Rigidbody>();
        _playerInput = GetComponent<PlayerInput>();

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
        if (!_isJumping) return;

        _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        _isJumping = false;
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
        Vector3 moveDirection = transform.forward * _moveInput.y + transform.right * _moveInput.x;
        Vector3 movement = moveDirection.normalized * (moveSpeed * GetSpeedMultiplier());
        _rb.MovePosition(_rb.position + movement * Time.fixedDeltaTime);
    }
    
    private void HandleLook() {
        float lookX = _lookInput.x * lookSensitivity;
        float lookY = _lookInput.y * lookSensitivity;

        transform.Rotate(Vector3.up * lookX);
        _playerInput.camera.transform.Rotate(Vector3.left * lookY);
    }

    private void OnMove(InputValue value) => _moveInput = value.Get<Vector2>();
    private void OnLook(InputValue value) => _lookInput = value.Get<Vector2>();
    private void OnJump(InputValue value) => _isJumping = value.isPressed && IsGrounded;

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