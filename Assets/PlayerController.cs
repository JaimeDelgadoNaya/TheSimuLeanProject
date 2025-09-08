using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Posición inicial")]
    public Vector3 startPosition = new Vector3(0, 1, 0);

    [Header("Referencias")]
    public Transform cam;
    public AudioSource audioSource;

    [Header("Movimiento")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 10f;
    public float airControl = 0.5f; // 0–1

    [Header("Salto & Gravedad")]
    public float jumpHeight = 2.0f;
    public float gravity = -9.81f;
    public float groundStick = -2f; // “pegar” al suelo al caer

    [Header("Ratón")]
    public float mouseSensitivity = 180f;
    public float maxLookAngle = 80f;

    [Header("Audio (opcional)")]
    public AudioClip footstepClip;
    public AudioClip jumpClip;
    public float stepIntervalWalk = 0.5f;
    public float stepIntervalRun = 0.35f;
    public float footstepMinSpeed = 0.1f;
    public Vector2 footstepPitchRange = new Vector2(0.95f, 1.05f);

    CharacterController cc;
    Vector3 velocity;      // sólo Y
    float xRot;            // pitch cámara
    float stepTimer;
    bool grounded;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        // Ajustes CC recomendados
        cc.center = new Vector3(0, 1, 0);
        cc.height = 2f;

        transform.position = startPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 1) Mirar
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);
        xRot -= mouseY; xRot = Mathf.Clamp(xRot, -maxLookAngle, maxLookAngle);
        if (cam) cam.localRotation = Quaternion.Euler(xRot, 0f, 0f);

        // 2) Estado de suelo ANTES de mover
        grounded = cc.isGrounded;

        // “pegar” al suelo si estamos aterrizados y cayendo
        if (grounded && velocity.y < 0f) velocity.y = groundStick;

        // 3) Entrada horizontal
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 inputDir = (transform.right * x + transform.forward * z).normalized;

        bool sprint = Input.GetKey(KeyCode.LeftShift);
        float targetSpeed = sprint ? sprintSpeed : walkSpeed;

        // Control en aire
        float control = grounded ? 1f : Mathf.Clamp01(airControl);
        Vector3 moveXZ = inputDir * targetSpeed * control;

        // 4) SALTO: sólo si grounded
        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (jumpClip && audioSource) audioSource.PlayOneShot(jumpClip);
            stepTimer = 0f;
            // Debug opcional:
            // Debug.Log("JUMP impulse " + velocity.y);
        }

        // 5) Gravedad
        velocity.y += gravity * Time.deltaTime;

        // 6) Mover UNA sola vez
        Vector3 fullMove = new Vector3(moveXZ.x, velocity.y, moveXZ.z);
        cc.Move(fullMove * Time.deltaTime);

        // 7) Pasos
        HandleFootsteps(sprint);

        // Respawn con R (por si caes)
        if (Input.GetKeyDown(KeyCode.R)) Respawn();
    }

    void HandleFootsteps(bool sprinting)
    {
        if (!audioSource || footstepClip == null) return;

        Vector3 hv = new Vector3(cc.velocity.x, 0f, cc.velocity.z);
        float speedMag = hv.magnitude;

        if (grounded && speedMag > footstepMinSpeed)
        {
            float interval = sprinting ? stepIntervalRun : stepIntervalWalk;
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                audioSource.pitch = Random.Range(footstepPitchRange.x, footstepPitchRange.y);
                audioSource.PlayOneShot(footstepClip);
                stepTimer = interval;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    void Respawn()
    {
        cc.enabled = false;
        transform.position = startPosition;
        velocity = Vector3.zero;
        cc.enabled = true;
    }
}
