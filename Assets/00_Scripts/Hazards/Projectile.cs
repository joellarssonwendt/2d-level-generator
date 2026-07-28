using UnityEngine;

public class Projectile : MonoBehaviour
{
    // Cache
    [SerializeField] private GameObject specialEffectPrefab;
    private Collider2D myCollider;
    private Rigidbody2D rigidbody2d;
    [HideInInspector] public GameObject player;
    [HideInInspector] public PlayerController playerController;

    // Variables
    [SerializeField] private float contactDamage = 10f;
    [SerializeField] private float contactKnockback = 300f;
    [SerializeField] private float moveSpeed = 5f;

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();

        if (myCollider == null)
        {
            Debug.LogError("Projectile.myCollider MISSING FROM " + gameObject.name);
        }

        rigidbody2d = GetComponent<Rigidbody2D>();

        if (rigidbody2d == null)
        {
            Debug.LogError("Projectile.rigidbody2d MISSING FROM " + gameObject.name);
        }
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("Projectile.player MISSING FROM " + gameObject.name);
        }

        playerController = player.GetComponent<PlayerController>();

        if (playerController == null)
        {
            Debug.LogError("Projectile.playerController MISSING FROM " + gameObject.name);
        }

        rigidbody2d.linearVelocity = Vector2.right * transform.localScale.x * moveSpeed;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject == player && contactDamage > 0f)
        {
            Destroy(gameObject);
            Instantiate(specialEffectPrefab, collision.collider.ClosestPoint(myCollider.bounds.center), Quaternion.identity);

            playerController.TakeDamage(contactDamage, contactKnockback, gameObject);
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
            Instantiate(specialEffectPrefab, collision.collider.ClosestPoint(myCollider.bounds.center), Quaternion.identity);
        }
    }
}
