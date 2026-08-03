using UnityEngine;

public class TorchSetup : MonoBehaviour
{
    [SerializeField] private GameObject fireAnimation;

    void Start()
    {
        fireAnimation.transform.rotation = Quaternion.Euler(0, 0, 270);
        GetComponentInChildren<Animator>().Play("Torch", 0, (float)WorldGenerator.GetRNG().NextDouble());
        fireAnimation.GetComponent<SpriteRenderer>().flipX = WorldGenerator.GetRNG().NextDouble() > 0.5;
    }
}
