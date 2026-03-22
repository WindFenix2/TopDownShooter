using UnityEngine;
using UnityEngine.UIElements;

public class Player_Movement : MonoBehaviour
{
    private Player player;

    private Player_EMIStatus emiStatus;

    private CharacterController characterController;
    private PlayerControls controls;
    private Animator animator;

    [Header("Movement info")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float runSpeed;
    [SerializeField] private float turnSpeed;
    private float baseSpeed;
    private float verticalVelocity;

    public Vector2 moveInput { get; private set; }
    private Vector3 movementDirection;

    private bool isRunning;

    private AudioSource walkSFX;
    private AudioSource runSFX;
    private bool canPlayFootsteps;

    private float baseAnimSpeed = 1f;

    private void Start()
    {
        player = GetComponent<Player>();

        walkSFX = player.sound.walkSFX;
        runSFX = player.sound.runSFX;
        Invoke(nameof(AllowfootstepsSFX), 1f);

        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (animator != null)
            baseAnimSpeed = animator.speed;

        emiStatus = GetComponent<Player_EMIStatus>();
        if (emiStatus == null)
            emiStatus = gameObject.AddComponent<Player_EMIStatus>();

        baseSpeed = walkSpeed;

        AssignInputEvents();
    }

    private void Update()
    {
        if (player.health.isDead)
            return;

        if (player.controlsEnabled == false)
            return;

        if (Time.timeScale == 0f)
            return;

        ApplyMovement();
        ApplyRotation();
        AnimatorControllers();
        ApplyAnimatorSpeedByEMI();
    }

    private void ApplyAnimatorSpeedByEMI()
    {
        if (animator == null)
            return;

        float mul = 1f;
        if (emiStatus != null)
            mul = emiStatus.GetSpeedMultiplier();

        float animMul = Mathf.Clamp(mul, 0.25f, 1f);
        animator.speed = baseAnimSpeed * animMul;
    }

    private void AnimatorControllers()
    {
        float xVelocity = Vector3.Dot(movementDirection.normalized, transform.right);
        float zVelocity = Vector3.Dot(movementDirection.normalized, transform.forward);

        animator.SetFloat("xVelocity", xVelocity, .1f, Time.deltaTime);
        animator.SetFloat("zVelocity", zVelocity, .1f, Time.deltaTime);

        bool playRunAnimation = isRunning & movementDirection.magnitude > 0;
        animator.SetBool("isRunning", playRunAnimation);
    }

    private void ApplyRotation()
    {
        if (player.aim == null)
            return;

        Vector3 lookingDirection = player.aim.GetMouseHitInfo().point - transform.position;
        lookingDirection.y = 0f;

        if (lookingDirection.sqrMagnitude < 0.0001f)
            return;

        lookingDirection.Normalize();

        Quaternion desiredRotation = Quaternion.LookRotation(lookingDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, turnSpeed * Time.deltaTime);
    }

    private void ApplyMovement()
    {
        movementDirection = new Vector3(moveInput.x, 0, moveInput.y);

        // Rotate movement input to match camera orientation
        if (CameraManager.instance != null)
        {
            float yaw = CameraManager.instance.GetCurrentYawAngle();
            movementDirection = Quaternion.Euler(0f, yaw, 0f) * movementDirection;
        }
        ApplyGravity();

        float mul = 1f;
        if (emiStatus != null)
            mul = emiStatus.GetSpeedMultiplier();

        float finalSpeed = baseSpeed * Mathf.Clamp(mul, 0.05f, 1f);

        if (movementDirection.magnitude > 0)
        {
            PlayFootstepsSFX();
            characterController.Move(movementDirection * Time.deltaTime * finalSpeed);
        }
    }

    private void PlayFootstepsSFX()
    {
        if (canPlayFootsteps == false)
            return;

        if (isRunning)
        {
            if (runSFX != null && runSFX.isPlaying == false)
                runSFX.Play();
        }
        else
        {
            if (walkSFX != null && walkSFX.isPlaying == false)
                walkSFX.Play();
        }
    }

    private void StopFootstepsSFX()
    {
        if (walkSFX != null) walkSFX.Stop();
        if (runSFX != null) runSFX.Stop();
    }

    private void AllowfootstepsSFX() => canPlayFootsteps = true;

    private void ApplyGravity()
    {
        if (characterController.isGrounded == false)
        {
            verticalVelocity -= 9.81f * Time.deltaTime;
            movementDirection.y = verticalVelocity;
        }
        else
        {
            verticalVelocity = -.5f;
        }
    }

    private void AssignInputEvents()
    {
        controls = player.controls;

        controls.Character.Movement.performed += context => moveInput = context.ReadValue<Vector2>();
        controls.Character.Movement.canceled += context =>
        {
            StopFootstepsSFX();
            moveInput = Vector2.zero;
        };

        controls.Character.Run.performed += context =>
        {
            baseSpeed = runSpeed;
            isRunning = true;
        };

        controls.Character.Run.canceled += context =>
        {
            baseSpeed = walkSpeed;
            isRunning = false;
        };
    }

    private void OnDisable()
    {
        StopFootstepsSFX();
        moveInput = Vector2.zero;

        if (animator != null)
            animator.speed = baseAnimSpeed;
    }
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        ZoneLimitation zone = hit.collider.GetComponent<ZoneLimitation>();
        if (zone != null)
            zone.ShowWallVisual();
    }
}
