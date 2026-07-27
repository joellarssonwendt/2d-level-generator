using System.Collections;
using UnityEngine;

public class DrakeBehaviour : MonoBehaviour
{
    // Cache
    private EnemyCore enemyCore;
    private Animator animator;

    // Variables
    private float jumpPowerX = 125.0f;
    private float jumpPowerY = 150.0f;

    void Start()
    {
        enemyCore = GetComponent<EnemyCore>();

        if (enemyCore == null)
        {
            Debug.LogError("DrakeBehaviour.enemyCore MISSING FROM " + gameObject.name);
        }

        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError("DrakeBehaviour.animator MISSING FROM " + gameObject.name);
        }

        StartCoroutine(JumpRoutine());
    }

    void Jump()
    {
        if (enemyCore.canAct && enemyCore.CheckIfGrounded())
        {
            enemyCore.rigidbody2d.AddForce(new Vector2(transform.localScale.x * jumpPowerX, jumpPowerY * 2f));
        }
    }

    IEnumerator JumpRoutine()
    {
        while (true)
        {
            int jumps = Random.Range(3, 6);

            while (jumps > 0)
            {
                jumps--;

                yield return new WaitUntil(enemyCore.CheckIfGrounded);
                yield return new WaitForSeconds(1f);

                if (enemyCore.CheckIfWall())
                {
                    transform.localScale = new Vector2(-transform.localScale.x, 1f);
                }

                Jump();                

                yield return null;
            }

            yield return new WaitUntil(enemyCore.CheckIfGrounded);
            yield return new WaitForSeconds(1f);

            if (enemyCore.CheckIfWall())
            {
                transform.localScale = new Vector2(-transform.localScale.x, 1f);
            }

            animator.Play("Attack");
            yield return new WaitForSeconds(0.3f);
            // Shoot fireball projectile
            yield return new WaitForSeconds(0.3f);
            animator.Play("Idle");

            yield return null;
        }
    }
}
