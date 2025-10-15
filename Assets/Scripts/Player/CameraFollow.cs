using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10.0f);
    [SerializeField] private float followSpeed = 3.0f;

    private GameObject player;
    private Transform target;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("EnemyCore.player MISSING FROM " + gameObject.name);
            return;
        }

        target = player.transform;
        transform.position = target.position;
    }

    void LateUpdate()
    {
        Vector3 newPosition = Vector3.Lerp(transform.position, target.position + offset, followSpeed * Time.deltaTime);
        transform.position = newPosition;
    }
}
