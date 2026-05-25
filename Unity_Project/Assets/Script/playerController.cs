using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class playerController : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [Tooltip("Velocidad de traslación horizontal en metros por segundo.")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Tooltip("Altura máxima del salto en metros reales.")]
    [SerializeField] private float jumpHeight = 2f;
    
    [Tooltip("Valor de la gravedad aplicada al personaje.")]
    [SerializeField] private float gravity = -9.81f;

    [Header("Configuración de Cámara")]
    [Tooltip("Sensibilidad del ratón.")]
    [SerializeField] private float mouseSensitivity = 15f;
    
    [Tooltip("Referencia al Transform de la cámara (debe ser hijo de este GameObject).")]
    [SerializeField] private Transform playerCamera;

    [Header("Referencias de Input (Unity 6)")]
    [Tooltip("Referencia a la acción de movimiento (Vector2).")]
    [SerializeField] private InputActionReference moveAction;
    
    [Tooltip("Referencia a la acción de salto (Button).")]
    [SerializeField] private InputActionReference jumpAction;
    
    [Tooltip("Referencia a la acción de mirar (Vector2 - Delta del ratón o joystick).")]
    [SerializeField] private InputActionReference lookAction;

    // Caché de componentes
    private CharacterController characterController;
    
    // Variables de estado físicas y de cámara
    private Vector3 movementVelocity;
    private Vector2 inputDirection;
    private bool isJumpRequested;
    private float xRotation = 0f; // Acumulador para limitar la rotación vertical (Pitch)

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        // Bloquear y ocultar el cursor del SO para el control de cámara
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
        if (lookAction != null) lookAction.action.Enable();
        
        if (jumpAction != null)
        {
            jumpAction.action.Enable();
            jumpAction.action.performed += OnJumpPerformed;
        }
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.action.Disable();
        if (lookAction != null) lookAction.action.Disable();
        
        if (jumpAction != null)
        {
            jumpAction.action.Disable();
            jumpAction.action.performed -= OnJumpPerformed;
        }
    }

    private void Update()
    {
        ProcessMovement();
    }

    private void LateUpdate()
    {
        // Se ejecuta en LateUpdate para asegurar que el movimiento de Update ya se resolvió, evitando jitter.
        ProcessLook();
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (characterController.isGrounded)
        {
            isJumpRequested = true;
        }
    }

    private void ProcessMovement()
    {
        if (characterController.isGrounded && movementVelocity.y < 0)
        {
            movementVelocity.y = -2f; 
        }

        if (moveAction != null)
        {
            inputDirection = moveAction.action.ReadValue<Vector2>();
        }
        else
        {
            inputDirection = Vector2.zero;
        }

        // El movimiento debe ser relativo a la rotación actual del jugador
        Vector3 direction = transform.right * inputDirection.x + transform.forward * inputDirection.y;

        if (isJumpRequested)
        {
            movementVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            isJumpRequested = false;
        }

        movementVelocity.y += gravity * Time.deltaTime;

        Vector3 finalMotion = (direction * moveSpeed) + Vector3.up * movementVelocity.y;
        characterController.Move(finalMotion * Time.deltaTime);
    }

    private void ProcessLook()
    {
        // Validación de seguridad por si olvidaste asignar la cámara en el Inspector
        if (lookAction == null || playerCamera == null) return;

        // Leemos el delta del ratón
        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();

        // Dependiendo de cómo configures el "Processor" en el Input Action Asset, 
        // podrías o no necesitar Time.deltaTime aquí. Para raw delta, es necesario:
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        // 1. Rotación Vertical (Pitch) - Aplicada a la cámara
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Evitar que el jugador dé una vuelta de 360 grados sobre el cuello
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // 2. Rotación Horizontal (Yaw) - Aplicada al cuerpo (Capsule) del jugador
        transform.Rotate(Vector3.up * mouseX);
    }
}