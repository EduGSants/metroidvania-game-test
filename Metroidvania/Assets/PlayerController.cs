using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private float xAxis;

    [Header("Movimento")]
    [SerializeField] private float walkSpeed = 20;

    [Header("Pulo & Double Jump")]
    [SerializeField] private float jumpForce = 32;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private int maxJumps = 2;
    private int remainingJumps; // Contador interno

    [Header("Checagem de Chão")]
    [SerializeField] private Transform groundCheckA;

    [SerializeField] private Transform groundCheckB;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        remainingJumps = maxJumps;
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        CheckGround();
        GetInputs();
        Move();
        JumpInput();
    }

    void CheckGround()
    {
        // Se está no chão E não está subindo muito rápido (ex: acabou de pular)
        // Aumentei a tolerância de 0.1f para 3.0f para evitar o bug de travar
        if (IsGrounded() && rb.linearVelocity.y < 3f) 
        {
            remainingJumps = maxJumps;
        }
    }

    void GetInputs()
    {
        xAxis = 0;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) xAxis = -1;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) xAxis = 1;
    }

    void Move()
    {
        rb.linearVelocity = new Vector2(walkSpeed * xAxis, rb.linearVelocity.y);
    }

    void JumpInput()
    {
        // Pulo
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (remainingJumps > 0)
            {
                if(remainingJumps != 1)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                } else
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce*(float) 0.8);
                }
                remainingJumps--;
            }
        }

        // Corte do pulo
        if (Keyboard.current.spaceKey.wasReleasedThisFrame && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }

    bool IsGrounded()
    {
        // Desenha uma área. Se bater na Layer do Chão, retorna true.
        return Physics2D.OverlapArea(groundCheckA.position, groundCheckB.position, groundLayer);
    }

    private void OnDrawGizmos()
    {
        if (groundCheckA != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheckA.position, groundCheckRadius);
        }
        if (groundCheckB != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheckB.position, groundCheckRadius);
        }
    }
}