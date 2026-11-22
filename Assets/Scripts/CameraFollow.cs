
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("跟隨目標")]
    public Transform target;

    [Header("鏡頭跟隨設定")]
    public float followSmoothTime = 0.25f;
    public bool followHorizontal = true;
    public bool followVertical = false;
    public bool followDepth = true;

    [Header("拋球時鎖定鏡頭")]
    public bool lockOnThrow = true;
    public float throwLockDuration = 2.5f;

    [Header("鏡頭限制")]
    public bool usePositionLimits = false;
    public Vector2 xLimits = new Vector2(-20f, 20f);
    public Vector2 zLimits = new Vector2(-20f, 20f);

    private Vector3 offset;
    private Vector3 velocity = Vector3.zero;

    private float lockUntilTime = 0f;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning(
                "CameraFollow：尚未指定 Target！"
            );
            enabled = false;
            return;
        }

        // 保留原本攝影機與拉拉熊的距離
        offset = transform.position - target.position;
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        // Space：拋球時暫時鎖定鏡頭
        if (lockOnThrow &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            lockUntilTime =
                Time.time + throwLockDuration;
        }

        // R：重新開始，解除鏡頭鎖定
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            lockUntilTime = 0f;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // 拋球期間不跟隨
        if (lockOnThrow && Time.time < lockUntilTime)
            return;

        Vector3 desiredPosition =
            target.position + offset;

        // 可選擇跟隨哪些軸
        if (!followHorizontal)
        {
            desiredPosition.x = transform.position.x;
        }

        if (!followVertical)
        {
            desiredPosition.y = transform.position.y;
        }

        if (!followDepth)
        {
            desiredPosition.z = transform.position.z;
        }

        // 限制鏡頭移動範圍
        if (usePositionLimits)
        {
            desiredPosition.x = Mathf.Clamp(
                desiredPosition.x,
                xLimits.x,
                xLimits.y
            );

            desiredPosition.z = Mathf.Clamp(
                desiredPosition.z,
                zLimits.x,
                zLimits.y
            );
        }

        // 平滑移動鏡頭
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            Mathf.Max(0.01f, followSmoothTime)
        );

        // 不修改 Rotation，保留正面視角
    }
}
