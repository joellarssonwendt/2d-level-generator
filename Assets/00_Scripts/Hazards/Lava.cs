using UnityEngine;

public class Lava : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        GameObject triggeringObject = collision.gameObject;

        if (triggeringObject.CompareTag("Player"))
        {
            PlayerController player = triggeringObject.GetComponent<PlayerController>();

            if (player != null)
            {
                player.PlayerDeath();
            }
        }

        if (triggeringObject.CompareTag("Enemy"))
        {
            EnemyCore enemy = triggeringObject.GetComponent<EnemyCore>();

            if (enemy != null)
            {
                enemy.EnemyDeath();
            }
        }
    }
}
