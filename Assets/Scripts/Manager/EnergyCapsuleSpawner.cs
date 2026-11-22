using UnityEngine;

public class EnergyCapsuleSpawner : MonoBehaviour
{
    [Header("生成间隔（仅 Boss 战）")]
    [SerializeField] private float spawnIntervalBoss = 3f;

    [Header("生成位置")]
    [SerializeField] private float spawnXDistance = 20f;
    [SerializeField] private float spawnYMin = 0f;
    [SerializeField] private float spawnYMax = 8f;

    [Header("预制体")]
    [SerializeField] private GameObject capsulePrefab;

    private Transform playerTransform;
    private float timer;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    private void Update()
    {
        if (playerTransform == null) return;
        if (capsulePrefab == null) return;

        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.BossFight) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        SpawnCapsule();
        timer = spawnIntervalBoss;
    }

    private void SpawnCapsule()
    {
        float y = Random.Range(spawnYMin, spawnYMax);
        Vector3 pos = new Vector3(playerTransform.position.x + spawnXDistance, y, 0f);
        Instantiate(capsulePrefab, pos, Quaternion.identity);
    }
}
