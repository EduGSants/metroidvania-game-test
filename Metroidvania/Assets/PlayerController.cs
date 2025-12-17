using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    private Vector2 moveInput;
    Animator animator;
    private Rigidbody2D rb;

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

    [Header("Dash")]
    // Dash variables can be added here
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    private bool isDashing;
    private bool canDash = true;

    public static PlayerController instance;

    private float lastXDirection = 1;

    bool attack = false;

    float timeBetweenAttack, timeSinceAttack;

    private void Awake()
    {
        // Singleton
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        remainingJumps = maxJumps;

        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if(isDashing) return;
        if (Keyboard.current == null) return;

        CheckGround();
        GetInputs();
        Move();
        JumpInput();
        DashInput();
        Flip();
        Attack();
    }


    void Flip()
    {
        if(moveInput.x < 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if(moveInput.x > 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    void CheckGround()
    {
        // Se está no chão E não está subindo muito rápido (ex: acabou de pular)
        // Aumentei a tolerância de 0.1f para 3.0f para evitar o bug de travar
        if (IsGrounded()) 
        {
            if(rb.linearVelocity.y < 3f) remainingJumps = maxJumps;
            canDash = true;
        }
        
    }

    void Attack()
    {
        timeSinceAttack += Time.deltaTime;
        if(attack && timeSinceAttack >= timeBetweenAttack)
        {
            timeSinceAttack = 0f;
            animator.SetTrigger("Attacking");
        }
    }

    void GetInputs()
    {
        moveInput = Vector2.zero;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveInput.x = -1;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveInput.x = 1;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveInput.y = 1;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveInput.y = -1;
        attack = Input.GetMouseButton(0);

        // Atualiza a última direção horizontal para saber para onde dar dash se estiver parado
        if (moveInput.x != 0)
        {
            lastXDirection = moveInput.x;
        }
    }

    void Move()
    {
        rb.linearVelocity = new Vector2(walkSpeed * moveInput.x, rb.linearVelocity.y);
        animator.SetBool("Walking", rb.linearVelocityX != 0 && IsGrounded());
    }

    void JumpInput()
    {
        // Pulo
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (remainingJumps > 0 && IsGrounded())
            {
                if(remainingJumps != 1)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                } else
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.8f);
                }
                remainingJumps--;
            } else if (remainingJumps > 0 && !IsGrounded())
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.8f);
                remainingJumps = 0;
            }
        }

        // Corte do pulo
        if (Keyboard.current.spaceKey.wasReleasedThisFrame && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }

        animator.SetBool("Jump", !IsGrounded());
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

    void DashInput()
    {
        if (Keyboard.current.leftShiftKey.wasPressedThisFrame && canDash)
        {
            StartCoroutine(PerformDash());
            animator.SetBool("Dash", true);
        }
    }

    private IEnumerator PerformDash()
    {
        isDashing = true;
        canDash = false;
        
        // Guarda a gravidade e a escala original
        float originalGravity = rb.gravityScale;
        Vector3 originalScale = transform.localScale; 

        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero; 

        // Define direção do Dash
        Vector2 dashDir;
        if (moveInput == Vector2.zero) dashDir = new Vector2(lastXDirection, 0);
        else dashDir = moveInput.normalized;
        if(dashDir.y != 0)
        {
            animator.SetBool("DashUp", true);
            if(dashDir.y < 0)
            {
                transform.localScale = new Vector3(originalScale.x, -originalScale.y, originalScale.z);
            }
        }
        rb.linearVelocity = dashDir * dashSpeed;
        float timer = 0f;
        while (timer < dashDuration)
        {
            timer += Time.deltaTime;

            yield return null; // Espera o próximo frame
        }

        // Restaura tudo ao final
        transform.localScale = originalScale; // Garante que não alterou permanente o sprite
        rb.gravityScale = originalGravity;
        rb.linearVelocity = Vector2.zero;
        isDashing = false;
        animator.SetBool("Dash", false);
        animator.SetBool("DashUp", false);
        yield return new WaitForSeconds(dashCooldown);
    }
}