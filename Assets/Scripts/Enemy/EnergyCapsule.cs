using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnergyCapsule : MonoBehaviour
{
    [Header("Buff")]
    [SerializeField] private float buffDuration = 4f;

    private void Awake()
    {
        GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("[EnergyCapsule] 碰到物体: " + other.name + " Tag=" + other.tag);

        if (!other.CompareTag("Player")) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
            player.ActivateCapsuleBuff(buffDuration);

        Debug.Log("[EnergyCapsule] 玩家拾取！Buff 激活 " + buffDuration + " 秒");
        Destroy(gameObject);
    }
}
