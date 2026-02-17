using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
public class PlayerController : MonoBehaviour
{
    private Vector2 moveInput;
    Animator animator;
    private Rigidbody2D rb;

    [Header("Movimento")]
    [SerializeField] private float walkSpeed = 20;
    private float lastXDirection = 1;

    [Header("Pulo & Double Jump")]
    [SerializeField] private float jumpForce = 32;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private int maxJumps = 2;
    private int remainingJumps; 

    [Header("Checagem de Chão")]
    [SerializeField] private Transform groundCheckA;
    [SerializeField] private Transform groundCheckB;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private Vector2 dashAttackSize = new Vector2(1f, 1f);
    private bool isDashing;
    private bool canDash = true;
    
    private List<Collider2D> enemiesHitByDash = new List<Collider2D>(); 

    public static PlayerController instance;

    [Header("Ataque (Espada)")]
    bool attackInput = false;
    [SerializeField] private float attackCooldown = 0.5f;
    private float currentAttackTimer = 0f;

    [SerializeField] Transform SideAttackTransform, UpAttackTransform, DownAttackTransform;
    [SerializeField] Vector2 SideAttackArea, UpAttackArea, DownAttackArea;
    [SerializeField] LayerMask attackableLayer;
    [SerializeField] float damage;

    private void Awake()
    {
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
        if (currentAttackTimer > 0)
        {
            currentAttackTimer -= Time.deltaTime;
        }

        if(isDashing) return;

        if (Keyboard.current == null) return;

        CheckGround();
        GetInputs();
        Move();
        JumpInput();
        DashInput();
        Flip();
        HandleAttack();
    }

    void GetInputs()
    {
        moveInput = Vector2.zero;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveInput.x = -1;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveInput.x = 1;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveInput.y = 1;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveInput.y = -1;
        
        attackInput = Input.GetMouseButton(0);

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

    void Flip()
    {
        if(moveInput.x < 0)
            transform.localScale = new Vector3(-1, 1, 1);
        else if(moveInput.x > 0)
            transform.localScale = new Vector3(1, 1, 1);
    }


    void JumpInput()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (remainingJumps > 0 && IsGrounded())
            {
                if(remainingJumps != 1)
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                else
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.8f);
                
                remainingJumps--;
            } else if (remainingJumps > 0 && !IsGrounded())
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.8f);
                remainingJumps = 0;
            }
        }

        if (Keyboard.current.spaceKey.wasReleasedThisFrame && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }

        animator.SetBool("Jump", !IsGrounded());
    }

    void CheckGround()
    {
        if (IsGrounded()) 
        {
            if(rb.linearVelocity.y < 3f) remainingJumps = maxJumps;
            canDash = true;
        }
    }

    bool IsGrounded()
    {
        return Physics2D.OverlapArea(groundCheckA.position, groundCheckB.position, groundLayer);
    }


    void HandleAttack()
    {
        if(attackInput && currentAttackTimer <= 0)
        {
            PerformAttack();
            currentAttackTimer = attackCooldown;
        }
    }

    void PerformAttack()
    {
        animator.SetTrigger("Attacking");
        
        if(moveInput.y > 0)
            Hit(UpAttackTransform, UpAttackArea);
        else if(moveInput.y < 0)
            Hit(DownAttackTransform, DownAttackArea);
        else
            Hit(SideAttackTransform, SideAttackArea);
    }

    private void Hit(Transform _attackTransform, Vector2 _attackArea)
    {
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(_attackTransform.position, _attackArea, 0, attackableLayer);
        foreach(Collider2D enemy in hitEnemies)
        {
            if(enemy.GetComponent<Enemy>() != null)
            {
                enemy.GetComponent<Enemy>().EnemyHit(damage);
            }
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
        
        enemiesHitByDash.Clear();

        float originalGravity = rb.gravityScale;
        Vector3 originalScale = transform.localScale; 

        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero; 

        Vector2 dashDir;
        if (moveInput == Vector2.zero) dashDir = new Vector2(lastXDirection, 0);
        else dashDir = moveInput.normalized;

        if(dashDir.y != 0)
        {
            animator.SetBool("DashUp", true);
            if(dashDir.y < 0) transform.localScale = new Vector3(originalScale.x, -originalScale.y, originalScale.z);
        }
        
        rb.linearVelocity = dashDir * dashSpeed;
        
        float timer = 0f;
        while (timer < dashDuration)
        {
            timer += Time.deltaTime;
            
            CheckDashHit();
            
            yield return null; 
        }

        transform.localScale = originalScale;
        rb.gravityScale = originalGravity;
        rb.linearVelocity = Vector2.zero;
        
        isDashing = false;
        animator.SetBool("Dash", false);
        animator.SetBool("DashUp", false);
        
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void CheckDashHit()
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, dashAttackSize, 0, attackableLayer);

        foreach (Collider2D enemy in hits)
        {
            if (!enemiesHitByDash.Contains(enemy))
            {
               enemiesHitByDash.Add(enemy);
               Debug.Log("Dano por dash!");
            }
        }
    }

    // Puramente visual isso aqui

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if(SideAttackTransform) Gizmos.DrawWireCube(SideAttackTransform.position, SideAttackArea);
        if(UpAttackTransform) Gizmos.DrawWireCube(UpAttackTransform.position, UpAttackArea);
        if(DownAttackTransform) Gizmos.DrawWireCube(DownAttackTransform.position, DownAttackArea);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, dashAttackSize);

        Gizmos.color = Color.green;
        if (groundCheckA != null) Gizmos.DrawWireSphere(groundCheckA.position, groundCheckRadius);
        if (groundCheckB != null) Gizmos.DrawWireSphere(groundCheckB.position, groundCheckRadius);
    }
}