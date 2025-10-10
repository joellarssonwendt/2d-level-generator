using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10.0f);
    [SerializeField] private float followSpeed = 3.0f;

    void LateUpdate()
    {
        Vector3 newPosition = Vector3.Lerp(transform.position, target.position + offset, followSpeed * Time.deltaTime);
        transform.position = newPosition;
    }
}
