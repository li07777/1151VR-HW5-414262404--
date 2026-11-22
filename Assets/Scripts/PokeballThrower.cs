
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PokeballThrower : MonoBehaviour
{
    [Header("角色與精靈球")]
    public GameObject pokeballPrefab;
    public Transform throwPoint;
    public Transform pokemonTarget;

    [Header("拉拉熊右手動畫")]
    public Transform rightHand;

    public float windupAngleY = -55f;
    public float releaseAngleY = 10f;

    public float windupDuration = 0.35f;
    public float releaseDuration = 0.18f;
    public float handReturnDuration = 0.35f;

    [Header("拋球設定")]
    public float throwDuration = 1.2f;
    public float arcHeight = 2f;
    public float rotationSpeed = 720f;
    public float targetHeight = 0.5f;

    [Header("收服特效")]
    public GameObject captureEffectPrefab;
    public float captureDuration = 0.6f;
    public float effectHeight = 0.5f;

    [Header("音效")]
    public AudioClip throwSound;
    public AudioClip captureSound;
    public AudioClip successSound;

    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("成功畫面")]
    public GameObject successText;
    public GameObject retryButton;

    private AudioSource audioSource;
    private GameObject currentBall;
    private GameObject currentEffect;

    private Vector3 originalPokemonScale;
    private Vector3 originalPokemonPosition;
    private Quaternion originalPokemonRotation;

    private Quaternion originalHandRotation;

    private bool isThrowing = false;
    private bool isCaptured = false;

    // ==========================
    // 初始化
    // ==========================
    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 1f;
        audioSource.mute = false;

        if (pokemonTarget != null)
        {
            originalPokemonScale = pokemonTarget.localScale;
            originalPokemonPosition = pokemonTarget.position;
            originalPokemonRotation = pokemonTarget.rotation;
        }

        if (rightHand != null)
        {
            originalHandRotation = rightHand.localRotation;
        }

        if (successText != null)
            successText.SetActive(false);

        if (retryButton != null)
            retryButton.SetActive(false);
    }

    // ==========================
    // 鍵盤控制
    // ==========================
    void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartGame();
            return;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame &&
            !isThrowing &&
            !isCaptured)
        {
            StartCoroutine(ThrowSequence());
        }
    }

    // ==========================
    // 拉拉熊揮手
    // ==========================
    IEnumerator ThrowSequence()
    {
        if (pokeballPrefab == null ||
            throwPoint == null ||
            pokemonTarget == null)
        {
            Debug.LogError("請設定精靈球、ThrowPoint 和 PokemonTarget！");
            yield break;
        }

        isThrowing = true;

        if (rightHand != null)
        {
            Quaternion windupRotation =
                originalHandRotation *
                Quaternion.Euler(0f, windupAngleY, 0f);

            yield return StartCoroutine(
                AnimateHand(
                    rightHand.localRotation,
                    windupRotation,
                    windupDuration
                )
            );

            Quaternion releaseRotation =
                originalHandRotation *
                Quaternion.Euler(0f, releaseAngleY, 0f);

            yield return StartCoroutine(
                AnimateHand(
                    rightHand.localRotation,
                    releaseRotation,
                    releaseDuration
                )
            );

            StartCoroutine(
                AnimateHand(
                    rightHand.localRotation,
                    originalHandRotation,
                    handReturnDuration
                )
            );
        }

        yield return StartCoroutine(ThrowBall());
    }

    IEnumerator AnimateHand(
        Quaternion startRotation,
        Quaternion endRotation,
        float duration)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / safeDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            if (rightHand != null)
            {
                rightHand.localRotation =
                    Quaternion.Slerp(
                        startRotation,
                        endRotation,
                        t
                    );
            }

            yield return null;
        }

        if (rightHand != null)
            rightHand.localRotation = endRotation;
    }

    // ==========================
    // 精靈球拋擲
    // ==========================
    IEnumerator ThrowBall()
    {
        currentBall = Instantiate(
            pokeballPrefab,
            throwPoint.position,
            Quaternion.identity
        );

        PlaySound(throwSound);

        Vector3 start = throwPoint.position;
        Vector3 end =
            pokemonTarget.position + Vector3.up * targetHeight;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, throwDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            Vector3 position = Vector3.Lerp(start, end, t);

            position.y += arcHeight * 4f * t * (1f - t);

            if (currentBall != null)
            {
                currentBall.transform.position = position;

                currentBall.transform.Rotate(
                    Vector3.right,
                    rotationSpeed * Time.deltaTime,
                    Space.Self
                );
            }

            yield return null;
        }

        if (currentBall != null)
            currentBall.transform.position = end;

        Debug.Log("精靈球命中寶可夢！");

        // 播放收服音效
        PlaySound(captureSound);

        // 播放粒子特效
        PlayCaptureEffect();

        // 同時執行寶可夢縮小動畫
        yield return StartCoroutine(ShrinkPokemon());

        if (currentBall != null)
        {
            Destroy(currentBall);
            currentBall = null;
        }

        // 等待收服音效完整播放完畢
        if (captureSound != null)
        {
            float remainingTime =
                Mathf.Max(
                    0f,
                    captureSound.length - captureDuration
                );

            yield return new WaitForSeconds(remainingTime);
        }

        // 收服完成
        isCaptured = true;
        isThrowing = false;

        // 不再呼叫 AudioSource.Stop()
        // 讓收服音效自然結束
        PlaySound(successSound);

        if (successText != null)
            successText.SetActive(true);

        if (retryButton != null)
            retryButton.SetActive(true);

        Debug.Log("Capture Success! 收服音效已完整播放！");
    }

    // ==========================
    // 收服粒子特效
    // ==========================
    void PlayCaptureEffect()
    {
        if (captureEffectPrefab == null ||
            pokemonTarget == null)
            return;

        Vector3 position =
            pokemonTarget.position + Vector3.up * effectHeight;

        currentEffect = Instantiate(
            captureEffectPrefab,
            position,
            Quaternion.identity
        );

        ParticleSystem[] particles =
            currentEffect.GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem particle in particles)
            particle.Play();

        Destroy(currentEffect, 5f);
    }

    // ==========================
    // 寶可夢縮小
    // ==========================
    IEnumerator ShrinkPokemon()
    {
        if (pokemonTarget == null)
            yield break;

        Vector3 startScale = pokemonTarget.localScale;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, captureDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            pokemonTarget.localScale =
                Vector3.Lerp(
                    startScale,
                    Vector3.zero,
                    t
                );

            yield return null;
        }

        pokemonTarget.localScale = Vector3.zero;
        pokemonTarget.gameObject.SetActive(false);
    }

    // ==========================
    // 音效播放
    // ==========================
    void PlaySound(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioClip 尚未設定！");
            return;
        }

        if (audioSource == null)
            return;

        audioSource.PlayOneShot(clip, soundVolume);

        Debug.Log("播放音效：" + clip.name);
    }

    // ==========================
    // 重新開始
    // ==========================
    public void RestartGame()
    {
        StopAllCoroutines();

        if (currentBall != null)
        {
            Destroy(currentBall);
            currentBall = null;
        }

        if (currentEffect != null)
        {
            Destroy(currentEffect);
            currentEffect = null;
        }

        if (pokemonTarget != null)
        {
            pokemonTarget.gameObject.SetActive(true);
            pokemonTarget.localScale = originalPokemonScale;
            pokemonTarget.position = originalPokemonPosition;
            pokemonTarget.rotation = originalPokemonRotation;
        }

        if (rightHand != null)
            rightHand.localRotation = originalHandRotation;

        if (successText != null)
            successText.SetActive(false);

        if (retryButton != null)
            retryButton.SetActive(false);

        if (audioSource != null)
            audioSource.Stop();

        isThrowing = false;
        isCaptured = false;

        Debug.Log("重新開始遊戲！");
    }
}
