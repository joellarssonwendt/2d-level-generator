using System.Collections;
using UnityEngine;

public class FrogBehaviour : MonoBehaviour
{
    // Cache
    private EnemyCore enemyCore;

    // Variables
    private float jumpPowerX = 200.0f;
    private float jumpPowerY = 300.0f;

    void Start()
    {
        enemyCore = GetComponent<EnemyCore>();

        if (enemyCore == null)
        {
            Debug.LogError("FrogBehaviour.enemyCore MISSING FROM " + gameObject.name);
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
        while(true)
        {
            yield return new WaitForSeconds(Random.Range(1.5f, 4.5f));
            
            if (enemyCore.CheckIfWall())
            {
                transform.localScale = new Vector2(-transform.localScale.x, 1f);
            }

            Jump();
        }
    }
}
