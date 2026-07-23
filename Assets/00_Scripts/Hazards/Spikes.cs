using UnityEngine;

public class Spikes : MonoBehaviour
{
    [SerializeField] private float contactDamage = 10f;
    [SerializeField] private float contactKnockback = 300f;

    [HideInInspector] public GameObject player;
    [HideInInspector] public PlayerController playerController;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("Spikes.player MISSING FROM " + gameObject.name);
        }

        playerController = player.GetComponent<PlayerController>();

        if (playerController == null)
        {
            Debug.LogError("Spikes.playerController MISSING FROM " + gameObject.name);
        }

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogError("Spikes.spriteRenderer MISSING FROM " + gameObject.name);
        }

        if (Random.value > 0.5f)
        {
            spriteRenderer.flipX = true;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == player && contactDamage > 0f)
        {
            playerController.TakeDamage(contactDamage, contactKnockback, gameObject);
        }
    }
}
