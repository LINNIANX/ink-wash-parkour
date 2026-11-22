using UnityEngine;

/// <summary>
/// 金币：玩家触碰后拾取，增加能量/分数，调用反馈，销毁自身
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Coin : MonoBehaviour
{
    [Header("金币参数")]
    [SerializeField] private int scoreValue = 10;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (EnergySkillSystem.Instance != null)
            {
                EnergySkillSystem.Instance.AddEnergy(scoreValue, transform.position);
            }

            if (FeedbackManager.Instance != null)
            {
                FeedbackManager.Instance.PlayCoinPickup();
            }

            Destroy(gameObject);
        }
    }
}
