using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class EnemyCore : MonoBehaviour
{
    // Debug
    [SerializeField] private bool debug = true;

    // Cache
    [SerializeField] private LayerMask groundLayer;
    private Animator animator;
    private AudioSource audioSource;
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

    // SFX
    [SerializeField] private AudioClip[] hurtSFX;
    [SerializeField] private AudioClip[] deathSFX;

    void Start()
    {
        HP = maxHP;

        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError("EnemyCore.animator MISSING FROM " + gameObject.name);
        }

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            Debug.LogError("EnemyCore.audioSource MISSING FROM " + gameObject.name);
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

        shaderGUItext = Shader.Find("GUI/Text Shader");
        defaultSpriteShader = Shader.Find("Sprites/Default");
        defaultColor = spriteRenderer.color;
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

        RaycastHit2D hit = Physics2D.BoxCast(transform.position, myCollider.bounds.size, angle, Vector2.down, distance, groundLayer);

        return hit;
    }

    public bool CheckIfWall()
    {
        Vector2 direction = Vector2.right * transform.localScale.x;
        float distance = myCollider.bounds.size.x;

        RaycastHit2D hit = Physics2D.Raycast(myCollider.bounds.center, direction, distance, groundLayer);

        return hit;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject == player && contactDamage > 0f)
        {
            playerController.TakeDamage(contactDamage, contactKnockback, gameObject);
        }
    }

    public void TakeDamage(float damage, float knockback, GameObject attacker)
    {
        if (isDead) return;
        if (damage <= 0f || damage < armor) return;

        if (knockback > 0f && knockback > knockbackResistance)
        {
            rigidbody2d.linearVelocity = Vector2.zero;

            float knockbackX;
            float knockbackY = knockback;

            if (transform.position.x > attacker.transform.position.x)
            {
                knockbackX = knockback / 2;
            }
            else
            {
                knockbackX = -knockback / 2;
            }

            knockbackY -= knockbackResistance;
            knockbackX -= knockbackResistance;

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
        // Handle enemy death
        // VFX
        // SFX
        Destroy(gameObject);
    }

    void PlaySFX(AudioClip[] audioClips, float volume = 1f, float pitch = 1f)
    {
        audioSource.pitch = Random.Range(0.9f, 1.1f) * pitch;
        audioSource.volume = Random.Range(0.9f, 1.1f) * volume;

        int randomIndex = Random.Range(0, audioClips.Length);
        audioSource.PlayOneShot(audioClips[randomIndex]);
    }

    void OnDrawGizmos()
    {
        if (debug == false) return;

        // Wall checker
        Gizmos.color = Color.green;
        Vector3 from = GetComponent<Collider2D>().bounds.center;
        Vector3 direction = Vector3.right * transform.localScale.x;
        float distance = GetComponent<Collider2D>().bounds.size.x;
        Vector3 to = from + direction * distance;
        Gizmos.DrawLine(from, to);

        // Grounded checker
        Gizmos.color = Color.yellow;
        float distance2 = 0.2f;
        Vector2 size = GetComponent<Collider2D>().bounds.size;
        Vector2 origin = GetComponent<Collider2D>().bounds.center;
        Vector2 direction2 = Vector2.down;
        Vector2 endPosition = origin + direction2 * distance2;
        Gizmos.DrawWireCube(endPosition, size);
    }
}
