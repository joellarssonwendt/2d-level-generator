using NUnit.Framework.Internal;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class PlayerController : MonoBehaviour
{
    // Debug
    [SerializeField] private bool debug = true;

    // Cache
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask hazardLayer;
    private Animator animator;
    private AudioSource audioSource;
    private CapsuleCollider2D capsuleCollider;
    private InputSystem_Actions inputActions;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rigidbody2d;

    // SFX
    [SerializeField] private AudioClip[] attackSwingsSFX;
    [SerializeField] private AudioClip[] attackHitsSFX;
    [SerializeField] private AudioClip[] chargingMediumSFX;
    [SerializeField] private AudioClip[] chargingHeavySFX;
    [SerializeField] private AudioClip[] dodgeRollSFX;
    [SerializeField] private AudioClip[] footStepsSFX;
    [SerializeField] private AudioClip[] jumpSFX;
    [SerializeField] private AudioClip[] landingSFX;
    [SerializeField] private AudioClip[] ledgeGrabSFX;
    [SerializeField] private AudioClip[] hurtSFX;
    [SerializeField] private AudioClip[] deathSFX;

    // Variables
    [HideInInspector] public bool canAct = true; // Disables input while false
    private bool isGrounded = false;
    private bool wasGroundedLastFrame = false;
    private float defaultGravityScale;
    private float moveSpeed = 5.0f;
    private Vector2 moveInput;
    private Vector2 startingPosition;

    // Jumping
    private bool canJump = true;
    private float coyoteTime = 0.15f;
    private float coyoteTimer;
    private float jumpInputBuffer = 0.15f;
    private float jumpInputTimer;
    private float jumpPower = 14.0f;

    // Dodge Roll
    private float dodgeRollDistance = 8.0f;
    private float dodgeRollDuration = 0.8f;
    private float dodgeRollCooldown = 0.3f;
    private bool canDodgeRoll = true;

    // Ledge Grab
    [SerializeField] private GameObject ledgeDetector;
    [SerializeField] private float ledgeDetectorRadius;
    [SerializeField] private GameObject wallFilter;
    [SerializeField] private Vector2 wallFilterSize;
    [SerializeField] private GameObject ceilingFilter;
    [SerializeField] private Vector2 ceilingFilterSize;
    private bool ledgeDetected;
    private bool ledgeDetectorActive;
    private bool isLedgeGrabbing = false;
    private float ledgeGrabCooldown = 0.2f;
    private float ledgeGrabTimer = 0f;
    private Collider2D ledgeCollision;

    // Hitpoints & Knockback
    private float HP = 1f;
    private float maxHP = 100f;
    private bool isKnockedBack = false;
    private float knockbackMargin = 0.2f;
    private float knockbackTimer;
    private Shader shaderGUItext;
    private Shader defaultSpriteShader;
    private Color defaultColor;
    private Color HPColor;
    [SerializeField] private Color bloodColor = new Color(103f / 255f, 0f, 0f, 1f);
    private bool flashSprite = false;
    private bool isDead = false;

    // Attack
    [SerializeField] private GameObject attackPoint;
    [SerializeField] private float attackAreaRadius;
    [SerializeField] private GameObject heavyAttackPoint;
    [SerializeField] private float heavyAttackAreaRadius;
    private float attackPower = 10f;
    private float knockbackPower = 200f;
    private float attackCooldown = 0.3f;
    private float attackTimer;
    private bool canAttack = true;
    private bool isCharging = false;
    private float chargeTime = 0f;
    private float lightThreshold = 0.5f;
    private float mediumThreshold = 1.5f;
    private float heavyThreshold = 3.0f;
    private bool attackAnimationLocked = false;

    void Start()
    {
        HP = maxHP;

        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rigidbody2d = GetComponent<Rigidbody2D>();

        defaultGravityScale = rigidbody2d.gravityScale;
        startingPosition = transform.position;

        shaderGUItext = Shader.Find("GUI/Text Shader");
        defaultSpriteShader = Shader.Find("Sprites/Default");
        defaultColor = spriteRenderer.color;

        inputActions = new InputSystem_Actions();
        inputActions.Player.Enable();
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
        inputActions.Player.Jump.performed += OnJump;
        inputActions.Player.Jump.canceled += OnJumpCanceled;
        inputActions.Player.Attack.performed += OnAttack;
        inputActions.Player.Attack.canceled += OnAttackRelease;
        inputActions.Player.DodgeRoll.performed += OnDodgeRoll;
    }

    void Update()
    {
        wasGroundedLastFrame = isGrounded;
        isGrounded = CheckIfGrounded();

        HandleJumping();
        HandleLedgeGrab();
        HandleKnockback();
        HandleAttacking();
    }

    void FixedUpdate()
    {
        if (canAct && !isLedgeGrabbing)
        {
            if (isCharging && isGrounded && chargeTime >= lightThreshold)
            {
                rigidbody2d.linearVelocityX = 0f;
            }
            else if (isCharging && !isGrounded && chargeTime >= lightThreshold)
            {
                // Preserve momentum
            }
            else
            {
                rigidbody2d.linearVelocityX = moveInput.x * moveSpeed;
            }

            if (moveInput.x != 0f && !attackAnimationLocked)
            {
                transform.localScale = new Vector2(Mathf.Sign(rigidbody2d.linearVelocityX), 1f);
            }
        }

        animator.SetFloat("velocityX", Mathf.Abs(rigidbody2d.linearVelocityX));
        animator.SetFloat("velocityY", rigidbody2d.linearVelocityY);
    }

    void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();

        if (isLedgeGrabbing && moveInput.y < 0f)
        {
            canJump = false;
            rigidbody2d.linearVelocityY = -0.1f;
        }
    }

    void OnJump(InputAction.CallbackContext context)
    {
        jumpInputTimer = jumpInputBuffer;
    }

    void DoJump()
    {
        if (canAct && canJump && coyoteTimer > 0f)
        {
            CancelCharging();

            canJump = false;
            jumpInputTimer = 0f;
            coyoteTimer = 0f;
            rigidbody2d.linearVelocityY = jumpPower;
            PlaySFX(jumpSFX, 0.1f);
        }
    }

    void OnJumpCanceled(InputAction.CallbackContext context)
    {
        if (rigidbody2d.linearVelocityY > 0f)
        {
            rigidbody2d.linearVelocityY *= 0.3f;
        }
    }

    void HandleJumping()
    {
        if (isGrounded || isLedgeGrabbing)
        {
            if (!wasGroundedLastFrame && !isLedgeGrabbing)
            {
                canJump = true;
                PlaySFX(landingSFX, 0.5f);
            }

            coyoteTimer = coyoteTime;
            animator.SetBool("isGrounded", true);
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
            animator.SetBool("isGrounded", false);
        }

        if (jumpInputTimer > 0f)
        {
            DoJump();
            jumpInputTimer -= Time.deltaTime;
        }
    }

    void OnAttack(InputAction.CallbackContext context)
    {
        if (isLedgeGrabbing || !canAct || !canAttack) return;

        isCharging = true;
        chargeTime = 0f;
        canAttack = false;
    }

    void OnAttackRelease(InputAction.CallbackContext context)
    {
        if (!isCharging) return;

        isCharging = false;

        if (chargeTime < lightThreshold)
        {
            animator.SetTrigger("LightAttack");
            DoAttack(1f, 1f, "light");
        }
        else if (chargeTime < mediumThreshold)
        {
            animator.Play("Attack_2");
            DoAttack(2f, 2f, "medium");
        }
        else
        {
            animator.Play("Attack_3");
            DoAttack(5f, 5f, "heavy");
        }

        //Debug.Log($"ChargeTime = {chargeTime}");
        chargeTime = 0f;
        attackTimer = attackCooldown;
        PlaySFX(attackSwingsSFX, 0.2f);

        Invoke("ResetAttackAnimationLocked", 0.1f);
    }

    void ResetAttackAnimationLocked()
    {
        attackAnimationLocked = false;
    }

    void HandleAttacking()
    {
        if (isCharging)
        {
            chargeTime += Time.deltaTime;
            chargeTime = Mathf.Min(chargeTime, heavyThreshold);

            if (chargeTime >= lightThreshold && chargeTime < mediumThreshold)
            {
                attackAnimationLocked = true;
                animator.Play("ChargingMedium");
            }
            else if (chargeTime >= mediumThreshold)
            {
                attackAnimationLocked = true;
                animator.Play("ChargingHeavy");
            }
        }

        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;

            if (attackTimer <= 0f)
            {
                attackTimer = 0f;
                canAttack = true;
            }
        }
    }

    void CancelCharging()
    {
        if (isCharging)
        {
            isCharging = false;
            chargeTime = 0f;
            attackTimer = attackCooldown;
            attackAnimationLocked = false;
            animator.Play("Idle");
        }
    }

    public void DoAttack(float damageMultiplier = 1f, float knockbackMultiplier = 1f, string attackLabel = "light")
    {
        Collider2D[] attackArea = null;
        Collider2D[] breakArea = null;

        if (attackLabel.Equals("light"))
        {
            attackArea = Physics2D.OverlapCircleAll(attackPoint.transform.position, attackAreaRadius, enemyLayer);
            breakArea = Physics2D.OverlapCircleAll(attackPoint.transform.position, attackAreaRadius, hazardLayer);
        }
        else if (attackLabel.Equals("medium"))
        {
            attackArea = Physics2D.OverlapCircleAll(heavyAttackPoint.transform.position, heavyAttackAreaRadius, enemyLayer);
            breakArea = Physics2D.OverlapCircleAll(heavyAttackPoint.transform.position, heavyAttackAreaRadius, hazardLayer);
        }
        else if (attackLabel.Equals("heavy"))
        {
            attackArea = Physics2D.OverlapCircleAll(heavyAttackPoint.transform.position, heavyAttackAreaRadius, enemyLayer);
            breakArea = Physics2D.OverlapCircleAll(heavyAttackPoint.transform.position, heavyAttackAreaRadius, hazardLayer);
        }

        for (int i = 0; i < attackArea.Length; i++)
        {
            if (attackArea[i].gameObject.CompareTag("Enemy"))
            {
                attackArea[i].gameObject.GetComponent<EnemyCore>().TakeDamage(attackPower * damageMultiplier, knockbackPower * knockbackMultiplier, gameObject);
            }
        }

        for (int i = 0; i < breakArea.Length; i++)
        {
            /*
            if (breakArea[i].gameObject.CompareTag("Destructible"))
            {
                breakArea[i].gameObject.GetComponent<DestructibleScript>().TakeDamage(attackPower);
            }
            */
        }
    }

    void OnDodgeRoll(InputAction.CallbackContext context)
    {
        if (canAct && canDodgeRoll && isGrounded && !isLedgeGrabbing)
        {
            StartCoroutine(DodgeRoll());
        }
    }

    IEnumerator DodgeRoll()
    {
        CancelCharging();

        canAct = false;
        canDodgeRoll = false;
        IgnoreEnemyCollision(true);
        rigidbody2d.linearVelocityX = transform.localScale.x * dodgeRollDistance;
        animator.SetBool("isDodgeRolling", true);
        animator.Play("DodgeRoll");
        PlaySFX(dodgeRollSFX, 0.5f);

        yield return new WaitForSeconds(dodgeRollDuration * 0.8f);
        IgnoreEnemyCollision(false);

        yield return new WaitForSeconds(dodgeRollDuration * 0.2f);
        rigidbody2d.linearVelocityX = 0f;
        animator.SetFloat("velocityX", 0f);
        canAct = true;
        animator.SetBool("isDodgeRolling", false);

        yield return new WaitForSeconds(dodgeRollCooldown);
        canDodgeRoll = true;
    }

    void IgnoreEnemyCollision(bool b)
    {
        Physics2D.IgnoreLayerCollision(3, 7, b); // Ignore collisions with enemies(7) for player(3)
    }

    bool CheckIfGrounded()
    {
        float distance = 0.2f;
        float angle = 0f;

        RaycastHit2D hit = Physics2D.CapsuleCast(capsuleCollider.bounds.center, capsuleCollider.bounds.size, capsuleCollider.direction, angle, Vector2.down, distance, groundLayer);

        return hit;
    }

    void HandleLedgeGrab()
    {
        ledgeDetectorActive = Physics2D.OverlapBox(wallFilter.transform.position, wallFilterSize, 0f, groundLayer) == null &&
            Physics2D.OverlapBox(ceilingFilter.transform.position, ceilingFilterSize, 0f, groundLayer) == null;

        if (moveInput.y < 0f || isKnockedBack || !canAct)
        {
            ledgeDetectorActive = false;
        }

        if (!isLedgeGrabbing && ledgeDetectorActive && rigidbody2d.linearVelocityY < 0f)
        {
            ledgeCollision = Physics2D.OverlapCircle(ledgeDetector.transform.position, ledgeDetectorRadius, groundLayer);
            ledgeDetected = ledgeCollision != null;
        }

        //Debug.Log("PlayerController::ledgeDetectorActive == " + ledgeDetectorActive);
        //Debug.Log("PlayerController::ledgeDetected == " + ledgeDetected);

        if (!isLedgeGrabbing && ledgeDetected && ledgeGrabTimer <= 0f) // Perform ledge-grab
        {
            CancelCharging();

            isLedgeGrabbing = true;
            canAttack = false;
            ledgeDetected = false;
            rigidbody2d.gravityScale = 0f;
            rigidbody2d.linearVelocity = Vector2.zero;

            Vector2 ledgePoint = ledgeCollision.ClosestPoint(ledgeDetector.transform.position);
            Vector2 grabAnchorWorld = ledgeDetector.transform.position;
            Vector2 offset = ledgePoint - grabAnchorWorld;
            Vector2 ledgeSnapTarget = (Vector2)transform.position + offset;
            rigidbody2d.MovePosition(ledgeSnapTarget);

            animator.Play("LedgeGrab");
            canJump = true;
            PlaySFX(ledgeGrabSFX, 0.3f);

            //Debug.Log("PlayerController::isLedgeGrabbing == " + isLedgeGrabbing);
        }

        if (isLedgeGrabbing && rigidbody2d.linearVelocity != Vector2.zero)
        {
            ledgeGrabTimer = ledgeGrabCooldown;
            isLedgeGrabbing = false;
            canAttack = true;
            rigidbody2d.gravityScale = defaultGravityScale;

            if (!isKnockedBack)
            {
                animator.Play("Idle");
            }
        }

        if (ledgeGrabTimer > 0f)
        {
            ledgeGrabTimer -= Time.deltaTime;
        }
    }

    public void TakeDamage(float damage, float knockback, GameObject enemy)
    {
        if (isDead) return;

        CancelCharging();

        canAct = false;
        rigidbody2d.linearVelocity = Vector2.zero;
        isKnockedBack = true;
        knockbackTimer = knockbackMargin;

        IgnoreEnemyCollision(true);

        float knockbackX;
        float knockbackY = knockback;

        if (transform.position.x > enemy.transform.position.x)
        {
            knockbackX = knockback / 2;
        }
        else
        {
            knockbackX = -knockback / 2;
        }

        rigidbody2d.AddForce(new Vector2(knockbackX, knockbackY));

        HP -= damage;

        float hpRatio = Mathf.Clamp01(HP / maxHP);
        HPColor = Color.Lerp(bloodColor, defaultColor, hpRatio);
        spriteRenderer.color = HPColor;

        if (HP <= 0f)
        {
            PlayerDeath();
            return;
        }

        PlaySFX(hurtSFX, 0.5f);
        animator.SetBool("isKnockedBack", true);
        animator.Play("Hurt");
        flashSprite = true;
        StartCoroutine(SpriteFlash());
    }

    void HandleKnockback()
    {
        if (isKnockedBack && isGrounded && knockbackTimer <= 0f)
        {
            canAct = true;
            isKnockedBack = false;
            animator.SetBool("isKnockedBack", false);
            flashSprite = false;
            StopCoroutine(SpriteFlash());
            spriteRenderer.material.shader = defaultSpriteShader;
            spriteRenderer.color = HPColor;
            IgnoreEnemyCollision(false);
        }

        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
        }
    }

    IEnumerator SpriteFlash()
    {
        while (flashSprite)
        {
            spriteRenderer.material.shader = shaderGUItext;
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.material.shader = defaultSpriteShader;
            spriteRenderer.color = HPColor;
            yield return new WaitForSeconds(0.1f);
        }
    }

    public void PlayerDeath()
    {
        isDead = true;
        canAct = false;
        flashSprite = false;
        StopCoroutine(SpriteFlash());
        spriteRenderer.material.shader = defaultSpriteShader;
        spriteRenderer.color = HPColor;
        PlaySFX(deathSFX, 0.8f);
        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        // Play death animation
        // VFX
        // SFX
        // Slow time (?)
        //Debug.Log("PlayerController::PlayerDeath()");

        yield return new WaitForSeconds(1.5f);
        ResetPlayer();
    }

    private void ResetPlayer()
    {
        // Reset transform
        transform.position = startingPosition;
        transform.localScale = new Vector2(1f, 1f);

        // Reset physics
        rigidbody2d.linearVelocity = Vector2.zero;
        rigidbody2d.gravityScale = defaultGravityScale;
        IgnoreEnemyCollision(false);

        // Reset core variables
        isDead = false;
        canAct = true;
        isGrounded = false;
        wasGroundedLastFrame = false;
        canJump = true;
        canDodgeRoll = true;
        isLedgeGrabbing = false;
        ledgeDetected = false;
        ledgeDetectorActive = false;
        HP = maxHP;
        isKnockedBack = false;

        // Reset colors
        flashSprite = false;
        StopCoroutine(SpriteFlash());
        spriteRenderer.material.shader = defaultSpriteShader;
        HPColor = defaultColor;
        spriteRenderer.color = defaultColor;

        // Reset timers
        coyoteTimer = 0f;
        jumpInputTimer = 0f;
        ledgeGrabTimer = 0f;

        // Reset input
        moveInput = Vector2.zero;

        // Reset animator
        animator.SetBool("isGrounded", false);
        animator.SetBool("isDodgeRolling", false);
        animator.Play("Idle");
    }


    void PlaySFX(AudioClip[] audioClips, float volume = 1f, float pitch = 1f)
    {
        audioSource.pitch = Random.Range(0.9f, 1.1f) * pitch;
        audioSource.volume = Random.Range(0.9f, 1.1f) * volume;

        int randomIndex = Random.Range(0, audioClips.Length);
        audioSource.PlayOneShot(audioClips[randomIndex]);
    }

    public void Footsteps() // Called from animation events
    {
        PlaySFX(footStepsSFX, 0.4f);
    }

    void OnDrawGizmos()
    {
        if (debug == false) return;

        // Ledge filters
        Gizmos.color = Color.red;
        Gizmos.DrawCube(wallFilter.transform.position, wallFilterSize);
        Gizmos.DrawCube(ceilingFilter.transform.position, ceilingFilterSize);

        // Ledge detector
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(ledgeDetector.transform.position, ledgeDetectorRadius);

        // Grounded checker
        Gizmos.color = Color.yellow;
        float distance = 0.2f;
        Vector2 size = GetComponent<CapsuleCollider2D>().bounds.size;
        Vector2 origin = GetComponent<CapsuleCollider2D>().bounds.center;
        Vector2 direction = Vector2.down;
        Vector2 endPosition = origin + direction * distance;
        Gizmos.DrawWireCube(endPosition, size);

        // Attack area
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(attackPoint.transform.position, attackAreaRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(heavyAttackPoint.transform.position, heavyAttackAreaRadius);
    }
}
