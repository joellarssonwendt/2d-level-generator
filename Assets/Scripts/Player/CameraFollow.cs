using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Singleton;

    void Awake()
    {
        if (CameraFollow.Singleton != null && CameraFollow.Singleton != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            CameraFollow.Singleton = this;
        }
    }

    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10.0f);
    [SerializeField] private float followSpeed = 3.0f;

    private GameObject player;
    private Transform target;
    private bool bFollowing = true;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("CameraFollow.player MISSING FROM " + gameObject.name);
            return;
        }

        target = player.transform;
        transform.position = target.position;
    }

    void LateUpdate()
    {
        if (!bFollowing)
        {
            return;
        }

        Vector3 newPosition = Vector3.Lerp(transform.position, target.position + offset, followSpeed * Time.deltaTime);
        transform.position = newPosition;
    }

    public void FollowTarget(bool b)
    {
        bFollowing = b;
    }

    public void SnapToTarget()
    {
        transform.position = target.position;
    }
}
