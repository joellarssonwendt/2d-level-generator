using System.Collections;
using UnityEngine;

public class DrakeBehaviour : MonoBehaviour, IEnemyBehaviour
{
    // Cache
    [SerializeField] private GameObject fireballPrefab;
    private EnemyCore enemyCore;
    private Animator animator;
    private Coroutine coroutine;

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
    }

    public void Cull(bool cull)
    {
        if (cull)
        {
            if (coroutine == null) return;

            StopCoroutine(coroutine);
            coroutine = null;
        }
        else
        {
            if (coroutine != null) return;

            coroutine = StartCoroutine(MyCoroutine());
        }
    }

    private void Jump()
    {
        if (enemyCore.canAct && enemyCore.CheckIfGrounded())
        {
            enemyCore.rigidbody2d.AddForce(new Vector2(transform.localScale.x * jumpPowerX, jumpPowerY * 2f));
        }
    }

    private IEnumerator MyCoroutine()
    {
        bool isGrounded = false;

        while (true)
        {
            int jumps = Random.Range(3, 6);

            while (jumps > 0)
            {
                jumps--;

                while (!isGrounded)
                {
                    isGrounded = enemyCore.CheckIfGrounded();
                    yield return new WaitForSeconds(0.33f);
                }

                yield return new WaitForSeconds(0.5f);

                if (enemyCore.CheckIfWall())
                {
                    transform.localScale = new Vector2(-transform.localScale.x, 1f);
                }

                Jump();
                isGrounded = false;
            }

            while (!isGrounded)
            {
                isGrounded = enemyCore.CheckIfGrounded();
                yield return new WaitForSeconds(0.33f);
            }

            yield return new WaitForSeconds(0.5f);

            if (enemyCore.CheckIfWall())
            {
                transform.localScale = new Vector2(-transform.localScale.x, 1f);
            }

            animator.Play("Attack");
            yield return new WaitForSeconds(0.3f);
            GameObject fireball = Instantiate(fireballPrefab, transform.position + new Vector3(0, 0.5f, 0), Quaternion.identity);
            fireball.transform.localScale = transform.localScale;
            yield return new WaitForSeconds(0.3f);
            animator.Play("Idle");

            yield return null;
        }
    }
}
