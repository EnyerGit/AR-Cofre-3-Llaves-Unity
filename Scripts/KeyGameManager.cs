using System.Collections;
using TMPro;
using UnityEngine;

public class KeyGameManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI counterText;
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI timerText; // <-- NUEVO (TimerTMP)

    [Header("Tiempo (1 minuto)")]
    public float timeLimitSeconds = 60f;
    public bool autoResetOnTimeUp = false;
    public float autoResetDelay = 1.5f;
    public AudioClip timeUpClip; // opcional (si no pones nada, no suena)

    [Header("Cofre")]
    public GameObject cofreCerrado;
    public GameObject cofreAbierto;

    [Header("Llaves (VISUALES)")]
    public GameObject keyVisual1;
    public GameObject keyVisual2;
    public GameObject keyVisual3;

    [Header("VFX (opcional)")]
    public ParticleSystem chestOpenVFX;

    [Header("SFX (opcional)")]
    public AudioSource sfxSource;
    public AudioClip keyFoundClip;
    public AudioClip chestOpenClip;
    public AudioClip resetClip;

    [Header("Relock Vuforia al reiniciar (recomendado)")]
    public bool forceRelockOnReset = true;
    public float relockDelay = 0.10f;
    public GameObject itLlave1Target;
    public GameObject itLlave2Target;
    public GameObject itLlave3Target;

    bool key1, key2, key3;
    bool chestOpened; // evita que se ejecute 20 veces

    // --- NUEVO: control de ronda + tiempo ---
    bool roundLocked;
    float remainingTime;
    bool timerRunning;

    Coroutine msgRoutine;

    void Start() => ResetGame();

    void Update()
    {
        if (!timerRunning || roundLocked) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            UpdateTimerUI();
            TimeUp();
            return;
        }

        UpdateTimerUI();
    }

    public void FoundKey1()
    {
        if (roundLocked || key1) return;
        key1 = true;
        if (keyVisual1) keyVisual1.SetActive(true);
        PlaySFX(keyFoundClip);
        ShowMsg("Llave 1 encontrada");
        UpdateAll();
    }

    public void FoundKey2()
    {
        if (roundLocked || key2) return;
        key2 = true;
        if (keyVisual2) keyVisual2.SetActive(true);
        PlaySFX(keyFoundClip);
        ShowMsg("Llave 2 encontrada");
        UpdateAll();
    }

    public void FoundKey3()
    {
        if (roundLocked || key3) return;
        key3 = true;
        if (keyVisual3) keyVisual3.SetActive(true);
        PlaySFX(keyFoundClip);
        ShowMsg("Llave 3 encontrada");
        UpdateAll();
    }

    public void ResetGame()
    {
        key1 = key2 = key3 = false;
        chestOpened = false;
        roundLocked = false;

        // Tiempo
        remainingTime = timeLimitSeconds;
        timerRunning = true;
        UpdateTimerUI();

        // Oculta llaves (arregla lo de “queda visible la última”)
        if (keyVisual1) keyVisual1.SetActive(false);
        if (keyVisual2) keyVisual2.SetActive(false);
        if (keyVisual3) keyVisual3.SetActive(false);

        // Cofre
        if (cofreCerrado) cofreCerrado.SetActive(true);
        if (cofreAbierto) cofreAbierto.SetActive(false);

        // VFX
        if (chestOpenVFX)
            chestOpenVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        PlaySFX(resetClip);
        ShowMsg("Reiniciado");

        UpdateAll();

        if (forceRelockOnReset) StartCoroutine(RearmTargets());
    }

    void UpdateAll()
    {
        int count = (key1 ? 1 : 0) + (key2 ? 1 : 0) + (key3 ? 1 : 0);

        if (counterText) counterText.text = $"Llaves: {count}/3";

        if (count >= 3 && !chestOpened)
        {
            chestOpened = true;
            OpenChestSequence();
        }
    }

    void OpenChestSequence()
    {
        // Gana: detener tiempo y bloquear ronda
        timerRunning = false;
        roundLocked = true;

        if (cofreCerrado) cofreCerrado.SetActive(false);
        if (cofreAbierto) cofreAbierto.SetActive(true);

        PlaySFX(chestOpenClip);

        if (chestOpenVFX) chestOpenVFX.Play();

        ShowMsg("¡Cofre abierto!");
    }

    // --- NUEVO: cuando se acaba el tiempo ---
    void TimeUp()
    {
        timerRunning = false;
        roundLocked = true;

        // Asegura cofre cerrado si se acabó el tiempo
        if (cofreCerrado) cofreCerrado.SetActive(true);
        if (cofreAbierto) cofreAbierto.SetActive(false);

        PlaySFX(timeUpClip);
        ShowMsg("Tiempo agotado. Reinicia.");

        if (autoResetOnTimeUp)
            StartCoroutine(AutoResetRoutine());
    }

    IEnumerator AutoResetRoutine()
    {
        yield return new WaitForSeconds(autoResetDelay);
        ResetGame();
    }

    void UpdateTimerUI()
    {
        if (!timerText) return;

        int total = Mathf.CeilToInt(remainingTime);
        int min = total / 60;
        int sec = total % 60;

        timerText.text = $"Tiempo: {min:00}:{sec:00}";
    }

    void PlaySFX(AudioClip clip)
    {
        if (!sfxSource || !clip) return;
        sfxSource.PlayOneShot(clip);
    }

    void ShowMsg(string msg, float seconds = 1.5f)
    {
        if (!messageText) return;

        messageText.text = msg;
        messageText.alpha = 1f;

        if (msgRoutine != null) StopCoroutine(msgRoutine);
        msgRoutine = StartCoroutine(HideMsgAfter(seconds));
    }

    IEnumerator HideMsgAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (!messageText) yield break;

        float a = 1f;
        while (a > 0f)
        {
            a -= Time.deltaTime * 2.5f;
            messageText.alpha = a;
            yield return null;
        }
        messageText.text = "";
        messageText.alpha = 0f;
    }

    IEnumerator RearmTargets()
    {
        if (itLlave1Target) itLlave1Target.SetActive(false);
        if (itLlave2Target) itLlave2Target.SetActive(false);
        if (itLlave3Target) itLlave3Target.SetActive(false);

        yield return null;
        yield return new WaitForSeconds(relockDelay);

        if (itLlave1Target) itLlave1Target.SetActive(true);
        if (itLlave2Target) itLlave2Target.SetActive(true);
        if (itLlave3Target) itLlave3Target.SetActive(true);
    }
}
