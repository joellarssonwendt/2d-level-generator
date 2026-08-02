using UnityEngine;

public class TorchSetup : MonoBehaviour
{
    [SerializeField] private GameObject fireAnimation;

    void Awake()
    {
        GetComponentInChildren<Animator>().Play("Torch", 0, Random.Range(0, 1f));
        GetComponentInChildren<SpriteRenderer>().flipX = Random.value > 0.5f;
    }

    void Start()
    {
        fireAnimation.transform.rotation = Quaternion.Euler(0, 0, 270);
    }
}
