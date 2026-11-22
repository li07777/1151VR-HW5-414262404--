
using UnityEngine;
using UnityEngine.InputSystem;

public class RilakkumaMovement : MonoBehaviour
{
    [Header("移動設定")]
    public float moveSpeed = 2.5f;
    public float runSpeed = 4.5f;
    public float rotationSpeed = 10f;

    [Header("攝影機設定")]
    public Transform cameraTransform;
    public bool cameraRelativeMovement = true;

    [Header("地面設定")]
    public Terrain terrain;
    public float groundOffset = 0f;

    [Header("模型方向")]
    public float modelForwardOffset = 0f;

    [Header("走路動畫物件")]
    public Transform legs;
    public Transform body;
    public Transform leftHand;
    public Transform rightHand;

    [Header("走路動畫設定")]
    public float walkFrequency = 7f;
    public float legSwingAngle = 15f;
    public float handSwingAngle = 18f;
    public float bodyBounceHeight = 0.04f;
    public float animationSmooth = 12f;

    [Header("拋球動畫協調")]
    public float throwHandLockTime = 1.2f;

    private Quaternion originalLegRotation;
    private Quaternion originalLeftHandRotation;
    private Quaternion originalRightHandRotation;

    private Vector3 originalBodyPosition;

    private float walkTime = 0f;
    private float animationWeight = 0f;

    private float rightHandLockUntil = 0f;
    private bool wasRightHandLocked = false;

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (legs != null)
            originalLegRotation = legs.localRotation;

        if (body != null)
            originalBodyPosition = body.localPosition;

        if (leftHand != null)
            originalLeftHandRotation = leftHand.localRotation;

        if (rightHand != null)
            originalRightHandRotation = rightHand.localRotation;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.aKey.isPressed)
            horizontal -= 1f;

        if (keyboard.dKey.isPressed)
            horizontal += 1f;

        if (keyboard.wKey.isPressed)
            vertical += 1f;

        if (keyboard.sKey.isPressed)
            vertical -= 1f;

        // 依攝影機方向移動
        Vector3 direction;

        if (cameraRelativeMovement && cameraTransform != null)
        {
            Vector3 right = cameraTransform.right;
            Vector3 forward = cameraTransform.forward;

            right.y = 0f;
            forward.y = 0f;

            right.Normalize();
            forward.Normalize();

            direction = right * horizontal + forward * vertical;
        }
        else
        {
            direction = new Vector3(
                horizontal,
                0f,
                vertical
            );
        }

        direction = Vector3.ClampMagnitude(direction, 1f);

        bool moving = direction.sqrMagnitude > 0.01f;

        bool running =
            keyboard.leftShiftKey.isPressed ||
            keyboard.rightShiftKey.isPressed;

        float speed = running ? runSpeed : moveSpeed;

        // 角色移動與轉向
        if (moving)
        {
            transform.position +=
                direction * speed * Time.deltaTime;

            Quaternion targetRotation =
                Quaternion.LookRotation(direction) *
                Quaternion.Euler(0f, modelForwardOffset, 0f);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Mathf.Clamp01(rotationSpeed * Time.deltaTime)
            );
        }

        // 貼合 Terrain 高度
        if (terrain != null)
        {
            Vector3 position = transform.position;

            position.y =
                terrain.SampleHeight(position) +
                terrain.transform.position.y +
                groundOffset;

            transform.position = position;
        }

        // Space 時暫停右手走路動畫
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            rightHandLockUntil =
                Time.time + throwHandLockTime;
        }

        // 按 R 時清除右手鎖定
        if (keyboard.rKey.wasPressedThisFrame)
        {
            rightHandLockUntil = 0f;
        }

        AnimateWalking(moving, running);
    }

    private void AnimateWalking(bool moving, bool running)
    {
        float targetWeight = moving ? 1f : 0f;

        animationWeight = Mathf.MoveTowards(
            animationWeight,
            targetWeight,
            animationSmooth * Time.deltaTime
        );

        if (moving)
        {
            walkTime +=
                Time.deltaTime *
                walkFrequency *
                (running ? 1.5f : 1f);
        }

        float swing =
            Mathf.Sin(walkTime) * animationWeight;

        float smooth =
            Mathf.Clamp01(animationSmooth * Time.deltaTime);

        // 腳部走路動畫
        if (legs != null)
        {
            Quaternion targetRotation =
                originalLegRotation *
                Quaternion.Euler(
                    swing * legSwingAngle,
                    0f,
                    0f
                );

            legs.localRotation = Quaternion.Slerp(
                legs.localRotation,
                targetRotation,
                smooth
            );
        }

        // 身體上下輕微晃動
        if (body != null)
        {
            float bounce =
                Mathf.Abs(Mathf.Sin(walkTime)) *
                bodyBounceHeight *
                animationWeight;

            Vector3 targetPosition =
                originalBodyPosition +
                Vector3.up * bounce;

            body.localPosition = Vector3.Lerp(
                body.localPosition,
                targetPosition,
                smooth
            );
        }

        // 左手走路擺動
        if (leftHand != null)
        {
            Quaternion targetRotation =
                originalLeftHandRotation *
                Quaternion.Euler(
                    0f,
                    swing * handSwingAngle,
                    0f
                );

            leftHand.localRotation = Quaternion.Slerp(
                leftHand.localRotation,
                targetRotation,
                smooth
            );
        }

        // 右手與拋球動畫協調
        bool rightHandLocked =
            Time.time < rightHandLockUntil;

        if (rightHand != null)
        {
            if (!rightHandLocked)
            {
                // 拋球結束後，重新接管右手
                if (wasRightHandLocked)
                {
                    // 不強制瞬間轉回，避免跳動
                    wasRightHandLocked = false;
                }

                Quaternion targetRotation =
                    originalRightHandRotation *
                    Quaternion.Euler(
                        0f,
                        -swing * handSwingAngle,
                        0f
                    );

                rightHand.localRotation = Quaternion.Slerp(
                    rightHand.localRotation,
                    targetRotation,
                    smooth
                );
            }
            else
            {
                // 拋球期間不修改右手
                wasRightHandLocked = true;
            }
        }
    }

    private void OnDisable()
    {
        if (legs != null)
            legs.localRotation = originalLegRotation;

        if (body != null)
            body.localPosition = originalBodyPosition;

        if (leftHand != null)
            leftHand.localRotation = originalLeftHandRotation;

        if (rightHand != null)
            rightHand.localRotation = originalRightHandRotation;
    }
}
