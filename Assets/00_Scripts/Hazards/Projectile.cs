using UnityEngine;

public class Projectile : MonoBehaviour
{
    // Constants
    private const float CULL_DISTANCE_SQUARED = 225; // 15^2

    // Cache
    [SerializeField] private GameObject specialEffectPrefab;
    [SerializeField] private AudioClip[] launchSFX;
    private Collider2D myCollider;
    private Rigidbody2D rigidbody2d;
    [HideInInspector] public GameObject player;
    [HideInInspector] public PlayerController playerController;
    private Transform playerTransform;

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

        playerTransform = player.transform;
        rigidbody2d.linearVelocity = Vector2.right * transform.localScale.x * moveSpeed;
        AudioSourcePool.Play(launchSFX, 0.5f);
    }

    void Update()
    {
        //Debug.Log($"{Vector2.SqrMagnitude(playerTransform.position - transform.position)}");

        if (Vector2.SqrMagnitude(playerTransform.position - transform.position) > CULL_DISTANCE_SQUARED)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject == player && contactDamage > 0f)
        {
            Destroy(gameObject);
            Instantiate(specialEffectPrefab, collision.collider.ClosestPoint(myCollider.bounds.center), Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));

            playerController.TakeDamage(contactDamage, contactKnockback, gameObject);
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
            Instantiate(specialEffectPrefab, collision.collider.ClosestPoint(myCollider.bounds.center), Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
        }
    }
}
