
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
  [SerializeField] public GameObject respawnCheckpoint;
  [SerializeField] private float ySpawnOffset = 1.0f; // Small lift to prevent falling through floor

  enum MovementState
  {
    Standing,
    Crouching
  }

  MovementState movementState = MovementState.Standing;

  [Header("Crouching")]
  [SerializeField] float crouchingHeight = 1.2f;
  [SerializeField] float crouchingSpeed = 4f;
  [SerializeField] float crouchingCameraYOffset = 0.1f;

  [Header("Sliding")]
  [SerializeField] float slideImpulse = 8f;
  [SerializeField] float slideBufferTime = 0.3f;
  [SerializeField] float slideCooldown = 2.2f; // Duration before another ground slide can trigger

  float slideBufferTimer;
  float slideCooldownTimer;
  bool canSlide = true;

  [Header("Camera")]
  [SerializeField] float standingCameraYOffset = 0.9f;
  [SerializeField] float cameraTransitionSpeed = 10f;

  [Header("Ground")]
  [SerializeField] float groundAcceleration = 85.5f;
  [SerializeField] float groundDeceleration = 25f;
  [SerializeField] LayerMask obstacleLayer;


  [Header("Air")]
  [SerializeField] float airBrakeAcceleration = 8.7f;


  bool shiftHeld;

  [Header("Crouching options and Fields")]
  [SerializeField] CapsuleCollider capsule;
  [SerializeField] Transform cameraPivot;
  [SerializeField] Camera playerCamera;
  Vector3 standingCapsuleCenter;
  float standingHeight;


  [Header("Jump")]
  [SerializeField] float jumpStrength = 13f;
  [SerializeField] float jumpBufferTime = 0.15f;

  float jumpBufferTimer;


  [Header("Grounded Check Fields")]
  [SerializeField] Transform groundCheck;
  [SerializeField] float groundCheckRadius = 0.2f;
  [SerializeField] LayerMask groundLayer;



  [Header("General")]
  [SerializeField] float moveSpeed = 13f;
  [SerializeField] float mouseSensitivity = 0.5f;

  Rigidbody rb;

  Vector2 moveInput;
  Vector2 mouseDelta;

  float pitch;
  float yaw;

  private bool crouchRequested = false;

  [Header("Momentum")]
  [SerializeField] float groundMomentumBonus;
  [SerializeField] float groundMomentumDecay = 5f;

  void Start()
  {
    rb = GetComponent<Rigidbody>();
    capsule = GetComponent<CapsuleCollider>();
    standingHeight = capsule.height;
    standingCapsuleCenter = capsule.center;

    // Recommended Rigidbody configuration
    rb.useGravity = true;
    rb.isKinematic = false;

    // Prevent physics from tipping the player over
    rb.constraints =
      RigidbodyConstraints.FreezeRotationX |
      RigidbodyConstraints.FreezeRotationZ;

    yaw = transform.eulerAngles.y;
    
    if (respawnCheckpoint != null)
      RespawnAtCheckpoint();
  }

  void Update()
  {
    // Read movement input
    moveInput = GetMoveInput();

    // Read mouse input and rotate camera/player
    RotateCamera();

    // Smooth camera transition
    UpdateCameraHeight();

    // 1. Check if Shift is currently held (for Crouch height & camera)
    shiftHeld = Keyboard.current != null &&
      (Keyboard.current.leftShiftKey.isPressed ||
       Keyboard.current.rightShiftKey.isPressed);

    // 2. Check if Shift was JUST pressed this frame (ONLY for Slide impulse)
    bool shiftPressedThisFrame = Keyboard.current != null &&
      (Keyboard.current.leftShiftKey.wasPressedThisFrame ||
       Keyboard.current.rightShiftKey.wasPressedThisFrame);

    if (shiftPressedThisFrame)
    {
      slideBufferTimer = slideBufferTime;
    }
    else
    {
      slideBufferTimer -= Time.deltaTime;
    }

    // Tick down the slide cooldown timer
    if (slideCooldownTimer > 0f)
    {
      slideCooldownTimer -= Time.deltaTime;
    }
    else if (IsGrounded())
    {
      // Re-enable sliding once grounded cooldown finishes
      canSlide = true;
    }

    // Handle jump buffer
    if (Keyboard.current != null &&
        Keyboard.current.spaceKey.wasPressedThisFrame)
    {
      jumpBufferTimer = jumpBufferTime;
    }
    else
    {
      jumpBufferTimer -= Time.deltaTime;
    }

    // Reset momentum bonus if below base move speed
    Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    if (horizontalVelocity.magnitude < moveSpeed)
      groundMomentumBonus = 0f;
  }

  void FixedUpdate()
  {
    Vector3 horizontalVelocity = new Vector3(
        rb.linearVelocity.x,
        0f,
        rb.linearVelocity.z
        );

    // Handle crouch and auto-uncrouch when space clears
    if (shiftHeld)
    {
      StartCrouching();
    }
    else if (movementState == MovementState.Crouching)
    {
      StopCrouching(); // retries uncrouching every physics frame until clear
    }

    // Update collider dimensions for crouch/stand height
    UpdateColliderHeight();

    Vector3 moveDirection =
      transform.forward * moveInput.y +
      transform.right * moveInput.x;

    moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

    if (IsGrounded())
    { 
      // Decrease ground momentum bonus linearly
      groundMomentumBonus = Mathf.MoveTowards(
          groundMomentumBonus,
          0f,
          groundMomentumDecay * Time.fixedDeltaTime
          );

      ApplyGroundMovement(moveDirection);
    }
    else
    {
      ApplyAirMovement(moveDirection);
    }

    if (!IsGrounded())
    {
        canSlide = true;
    }

    // Slide Impulse: ONLY triggers if shift was JUST pressed (buffer > 0)
    if (slideBufferTimer > 0f && IsGrounded() && canSlide)
    {
      if (horizontalVelocity.magnitude > 0.1f)
      {
        StartSliding();
        slideBufferTimer = 0f; // Consume slide buffer immediately

        slideCooldownTimer = slideCooldown;
        canSlide = false;
      }
    }

    // Jumping
    if (jumpBufferTimer > 0f && IsGrounded())
    {
      rb.AddForce(
          Vector3.up * jumpStrength,
          ForceMode.Impulse
          );

      jumpBufferTimer = 0f;
    }
  }


  void ApplyGroundMovement(Vector3 moveDirection)
  {
    Vector3 horizontalVelocity = new Vector3(
        rb.linearVelocity.x,
        0f,
        rb.linearVelocity.z
        );

    float allowedSpeed = moveSpeed + groundMomentumBonus;

    if (moveDirection.sqrMagnitude > 0.001f)
    {
      // Accelerate in the desired direction.
      Vector3 targetVelocity = moveDirection * allowedSpeed;

      Vector3 velocityChange = targetVelocity - horizontalVelocity;

      // Limit how much we can change velocity this physics frame.
      velocityChange = Vector3.ClampMagnitude(
          velocityChange,
          groundAcceleration * Time.fixedDeltaTime
          );

      rb.AddForce(
          velocityChange,
          ForceMode.VelocityChange
          );
    }
    else
    {
      // Decelerate when no movement input is given.
      Vector3 velocityChange = -horizontalVelocity;

      velocityChange = Vector3.ClampMagnitude(
          velocityChange,
          groundDeceleration * Time.fixedDeltaTime
          );

      rb.AddForce(
          velocityChange,
          ForceMode.VelocityChange
          );
    }
  }


  void ApplyAirMovement(Vector3 moveDirection)
  {
    Vector3 horizontalVelocity = new Vector3(
        rb.linearVelocity.x,
        0f,
        rb.linearVelocity.z
        );

    float speed = horizontalVelocity.magnitude;

    /*
       if (speed < 0.01f)
       return;
       */

    Vector3 velocityDirection = horizontalVelocity.normalized;

    float alignment = Vector3.Dot(
        moveDirection,
        velocityDirection
        );

    float brakingAmount = Mathf.Clamp01(-alignment);

    rb.AddForce(
        -velocityDirection * brakingAmount * airBrakeAcceleration,
        ForceMode.Acceleration
        );
  }



  Vector2 GetMoveInput()
  {
    Vector2 input = Vector2.zero;

    if (Keyboard.current != null)
    {
      if (Keyboard.current.wKey.isPressed) input.y += 1f;
      if (Keyboard.current.sKey.isPressed) input.y -= 1f;
      if (Keyboard.current.dKey.isPressed) input.x += 1f;
      if (Keyboard.current.aKey.isPressed) input.x -= 1f;
    }

    return Vector2.ClampMagnitude(input, 1f);
  }

  void RotateCamera()
  {
    Vector2 mouseDelta =
      Mouse.current?.delta.ReadValue() ?? Vector2.zero;

    pitch = Mathf.Clamp(
        pitch - mouseDelta.y * mouseSensitivity,
        -89f,
        89f
        );

    yaw += mouseDelta.x * mouseSensitivity;

    // Rotate player horizontally
    transform.rotation = Quaternion.Euler(0f, yaw, 0f);

    // Rotate camera vertically
    playerCamera.transform.localRotation =
      Quaternion.Euler(pitch, 0f, 0f);
  }


  void StartCrouching()
  {
    movementState = MovementState.Crouching;
  }

  bool StopCrouching()
  {
    if (!CanStand())
      return false;

    movementState = MovementState.Standing;
    return true;
  }


  void StartSliding()
  {
    if (!IsGrounded())
      return;

    Vector3 horizontalVelocity = new Vector3(
        rb.linearVelocity.x,
        0f,
        rb.linearVelocity.z
        );

    float currentSpeed = horizontalVelocity.magnitude;

    // Preserve horizontal speed while redirecting momentum.
    float newSpeed = currentSpeed + slideImpulse;

    Vector3 newVelocity = transform.forward * newSpeed;

    rb.linearVelocity = new Vector3(
        newVelocity.x,
        rb.linearVelocity.y,
        newVelocity.z
        );

    // Ensure ground movement can maintain the new speed.
    groundMomentumBonus = Mathf.Max(
        groundMomentumBonus,
        newSpeed - moveSpeed
        );
  }

  void UpdateColliderHeight()
  {
    float targetHeight =
      movementState == MovementState.Standing
      ? standingHeight
      : crouchingHeight;

    capsule.height = targetHeight;

    // Keep the bottom of the capsule in the same position.
    capsule.center = standingCapsuleCenter -
      Vector3.up * ((standingHeight - targetHeight) / 2f);
  }

  void UpdateCameraHeight()
  {
    float targetHeight =
      movementState == MovementState.Standing
      ? standingCameraYOffset
      : crouchingCameraYOffset;

    Vector3 position = cameraPivot.localPosition;

    position.y = Mathf.Lerp(
        position.y,
        targetHeight,
        cameraTransitionSpeed * Time.deltaTime
        );

    cameraPivot.localPosition = position;
  }


  bool CanStand()
  {
    float radius = capsule.radius * 0.95f;

    Vector3 worldCenter =
      transform.TransformPoint(standingCapsuleCenter);

    float halfSegment = standingHeight / 2f - radius;

    Vector3 bottom = worldCenter - transform.up * (halfSegment -0.1f);
    Vector3 top = worldCenter + transform.up * halfSegment;

    return !Physics.CheckCapsule(
        bottom,
        top,
        radius,
        obstacleLayer,
        QueryTriggerInteraction.Ignore
        );
  }

  bool IsGrounded()
  {
    return Physics.CheckSphere(
        groundCheck.position,
        groundCheckRadius,
        groundLayer
        );
  }


  void OnTriggerEnter(Collider other)
  {
    // if hit deathbarrier
    if (other.gameObject.CompareTag("DeathBarrier"))
    {
      // put at respawn pos
      //
      RespawnAtCheckpoint();
    }
  }

  void RespawnAtCheckpoint()
  {
    if (respawnCheckpoint != null)
    {
      transform.position = respawnCheckpoint.transform.position + (Vector3.up * ySpawnOffset);
      rb.linearVelocity = Vector3.zero;
    }
  }
}


