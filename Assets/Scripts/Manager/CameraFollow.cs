using UnityEngine;

/// <summary>
/// 相机跟随：正交相机在 LateUpdate 中平滑跟随玩家 X 轴位置，Y/Z 保持不变
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("跟随目标")]
    [SerializeField] private Transform target;

    [Header("跟随参数")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float xOffset = 3f;

    private float fixedY;
    private float fixedZ;

    private void Start()
    {
        Initialize();
    }

    /// <summary>
    /// 初始化：记录相机的固定 Y 和 Z 轴位置
    /// </summary>
    private void Initialize()
    {
        if (target == null)
        {
            Debug.LogWarning("[CameraFollow] 未设置跟随目标");
        }

        fixedY = transform.position.y;
        fixedZ = transform.position.z;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPosition = new Vector3(target.position.x + xOffset, fixedY, fixedZ);
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
    }
}
