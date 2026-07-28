using System.Collections;
using UnityEngine;

public class EnemyCore : MonoBehaviour
{
    // Debug
    [SerializeField] private bool debug = true;

    // Cache
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask hazardLayer;
    [SerializeField] private GameObject deathEffectPrefab;
    private Animator animator;
    private Collider2D myCollider;
    private SpriteRenderer spriteRenderer;
    [HideInInspector] public Rigidbody2D rigidbody2d;
    [HideInInspector] public GameObject player;
    [HideInInspector] public PlayerController playerController;

    // Variables
    [HideInInspector] public bool canAct = true;

    // Hitpoints & Damage
    private bool isDead = false;
    private float HP = 1f;
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float contactDamage = 10f;
    [SerializeField] private float contactKnockback = 300f;
    [SerializeField] private float armor = 0f;
    [SerializeField] private float knockbackResistance = 0f;
    private Shader shaderGUItext;
    private Shader defaultSpriteShader;
    private Color defaultColor;
    private Color HPColor;
    [SerializeField] private Color bloodColor = new Color(103f / 255f, 0f, 0f, 1f);

    void Awake()
    {
        HP = maxHP;

        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError("EnemyCore.animator MISSING FROM " + gameObject.name);
        }

        myCollider = GetComponent<Collider2D>();

        if (myCollider == null)
        {
            Debug.LogError("EnemyCore.myCollider MISSING FROM " + gameObject.name);
        }

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogError("EnemyCore.spriteRenderer MISSING FROM " + gameObject.name);
        }

        rigidbody2d = GetComponent<Rigidbody2D>();

        if (rigidbody2d == null)
        {
            Debug.LogError("EnemyCore.rigidbody2d MISSING FROM " + gameObject.name);
        }

        shaderGUItext = Shader.Find("GUI/Text Shader");
        defaultSpriteShader = Shader.Find("Sprites/Default");
        defaultColor = spriteRenderer.color;
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("EnemyCore.player MISSING FROM " + gameObject.name);
        }

        playerController = player.GetComponent<PlayerController>();

        if (playerController == null)
        {
            Debug.LogError("EnemyCore.playerController MISSING FROM " + gameObject.name);
        }
    }

    void FixedUpdate()
    {
        animator.SetFloat("velocityX", Mathf.Abs(rigidbody2d.linearVelocityX));
        animator.SetFloat("velocityY", rigidbody2d.linearVelocityY);
    }

    public bool CheckIfGrounded()
    {
        float distance = 0.2f;
        float angle = 0f;

        RaycastHit2D hit = Physics2D.BoxCast(myCollider.bounds.center, myCollider.bounds.size, angle, Vector2.down, distance, groundLayer);

        return hit;
    }

    public bool CheckIfWall()
    {
        float angle = 0f;
        Vector2 direction = Vector2.right * transform.localScale.x;
        float distance = myCollider.bounds.size.x * 0.3f;

        LayerMask wallLayers = groundLayer | enemyLayer | hazardLayer;

        RaycastHit2D[] hits = Physics2D.BoxCastAll(myCollider.bounds.center, myCollider.bounds.size * 0.8f, angle, direction, distance, wallLayers);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != myCollider)
                return true;
        }

        return false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject == player && contactDamage > 0f)
        {
            playerController.TakeDamage(contactDamage, contactKnockback, gameObject);
        }
    }

    public void TakeDamage(float damage, float knockback, GameObject source)
    {
        if (isDead) return;
        if (damage <= 0f || damage < armor) return;

        if (knockback > 0f && knockback > knockbackResistance)
        {
            rigidbody2d.linearVelocity = Vector2.zero;

            float knockbackX;
            float knockbackY = knockback;

            if (transform.position.x > source.transform.position.x)
            {
                knockbackX = knockback / 2 - knockbackResistance;
                transform.localScale = new Vector2(-1f, 1f);
            }
            else
            {
                knockbackX = -knockback / 2 + knockbackResistance;
                transform.localScale = new Vector2(1f, 1f);
            }

            rigidbody2d.AddForce(new Vector2(knockbackX, knockbackY));
        }

        HP -= damage - armor;

        float hpRatio = Mathf.Clamp01(HP / maxHP);
        HPColor = Color.Lerp(bloodColor, defaultColor, hpRatio);
        spriteRenderer.color = HPColor;

        if (HP <= 0f)
        {
            EnemyDeath();
            return;
        }

        StartCoroutine(SpriteFlash());
    }

    IEnumerator SpriteFlash()
    {
        canAct = false;
        spriteRenderer.material.shader = shaderGUItext;
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.material.shader = defaultSpriteShader;
        spriteRenderer.color = HPColor;
        canAct = true;
    }

    public void EnemyDeath()
    {
        Instantiate(deathEffectPrefab, myCollider.bounds.center, Quaternion.identity);
        Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        if (debug == false) return;

        Collider2D c = GetComponent<Collider2D>();
        if (c == null) return;

        // Wall checker
        Gizmos.color = Color.green;
        float distance = c.bounds.size.x * 0.3f;
        Vector2 size = c.bounds.size * 0.8f;
        Vector2 origin = c.bounds.center;
        Vector2 direction = Vector2.right * transform.localScale.x;
        Vector2 endPosition = origin + direction * distance;
        Gizmos.DrawWireCube(endPosition, size);

        // Grounded checker
        Gizmos.color = Color.yellow;
        float distance2 = 0.2f;
        Vector2 size2 = c.bounds.size;
        Vector2 origin2 = c.bounds.center;
        Vector2 direction2 = Vector2.down;
        Vector2 endPosition2 = origin2 + direction2 * distance2;
        Gizmos.DrawWireCube(endPosition2, size2);
    }
}
