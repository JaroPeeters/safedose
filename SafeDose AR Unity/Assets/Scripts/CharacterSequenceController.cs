using System.Collections;
using UnityEngine;

public class CharacterSequenceController : MonoBehaviour
{
    [Header("References")]
    public Animator animator;

    [Header("Appear Stuff")]
    public AppearScale appearScale;
    public ParticleTrigger appearParticleTrigger;
    public float appearEffectDelay = 0.2f;
    public AudioSource appearSound;
    public AudioSource appearPopSFX;
    [Header("Ragdoll SFX")]
    public AudioSource ragdollSound;
    public float ragdollVolume = 1f;

    [Header("Disappear Stuff (timed)")]
    public DisappearScale disappearScale;
    public DisappearEffect disappearEffect;
    [Tooltip("If true, trigger disappear FX right after the animation finishes (ignores delaySeconds).")]
    public bool disappearAfterAnimation = true;

    [Header("Animation State Names (Base Layer)")]
    public string nurofen = "nurofen";

    [Header("Blend Settings")]
    public float crossFadeDuration = 0.15f;   // 0.1–0.3 is usually fine
    [Tooltip("Safety cap for waiting on an animation to finish.")]
    public float maxAnimationWaitSeconds = 10f;

    Coroutine sequenceRoutine;
    bool isPlaying = false;
    public System.Action onSequenceFinished;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void PlaySequence()
    {
        if (isPlaying) return;

        isPlaying = true;
        sequenceRoutine = StartCoroutine(PlaySequenceRoutine());
    }

    public void StopAndReset()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        isPlaying = false;

        // Reset appear/disappear states if you want a hard reset
        if (appearScale != null)
            appearScale.ResetToSmall();

        if (disappearScale != null)
            disappearScale.ResetToVisible();

        if (appearParticleTrigger != null)
            appearParticleTrigger.StopEffect();

        if (disappearEffect != null)
            disappearEffect.StopEffect();

        StopAudio();
    }

    private IEnumerator PlaySequenceRoutine()
    {
        // 1) APPEAR PHASE
        if (appearScale != null)
            appearScale.PlayAppear();

        if (appearParticleTrigger != null)
            appearParticleTrigger.PlayAppearEffect(appearEffectDelay);

        if (appearSound != null)
            appearSound.Play();

        if (appearPopSFX != null)
            appearPopSFX.Play();

        // 2) START DISAPPEAR COUNTDOWNS (they handle their own delays)
        if (!disappearAfterAnimation)
        {
            if (disappearScale != null)
                disappearScale.PlayDisappearWithDelay();

            if (disappearEffect != null)
                disappearEffect.PlayDisappearWithDelay();
        }

        // 3) PLAY ANIMATION & WAIT UNTIL IT'S DONE (optional, but you already had this)
        yield return PlayAndWait(nurofen);

        if (disappearAfterAnimation)
        {
            if (disappearScale != null)
                disappearScale.PlayDisappearImmediate();

            if (disappearEffect != null)
                disappearEffect.PlayDisappearImmediate();
        }

        isPlaying = false;
        sequenceRoutine = null;
        onSequenceFinished?.Invoke();
    }

    private IEnumerator PlayAndWait(string stateName)
    {
        if (animator == null)
        {
            Debug.LogError("[CharacterSequenceController] Animator is NULL");
            yield break;
        }

        if (string.IsNullOrEmpty(stateName))
        {
            Debug.LogError("[CharacterSequenceController] State name is empty");
            yield break;
        }

        int stateHash = Animator.StringToHash(stateName);

        // Check if that state actually exists in Base Layer (layer 0)
        if (!animator.HasState(0, stateHash))
        {
            Debug.LogError($"[CharacterSequenceController] Animator does NOT have a state called '{stateName}' in Base Layer.");
            yield break;
        }

        Debug.Log($"[CharacterSequenceController] Playing state '{stateName}'");

        // Force the state to play from the beginning
        animator.CrossFadeInFixedTime(stateName, crossFadeDuration);

        // Wait until we are actually in the target state.
        float elapsed = 0f;
        while (elapsed < maxAnimationWaitSeconds)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.shortNameHash == stateHash || info.fullPathHash == stateHash)
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Now wait for the state to finish its first cycle.
        elapsed = 0f;
        while (elapsed < maxAnimationWaitSeconds)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if ((info.shortNameHash == stateHash || info.fullPathHash == stateHash) &&
                info.normalizedTime >= 1f)
            {
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public void StopAudio()
    {
        if (appearSound != null)
            appearSound.Stop();

        if (appearPopSFX != null)
            appearPopSFX.Stop();
    }

    public void PlayRagdollSound()
    {
        if (ragdollSound == null)
            return;

        ragdollSound.Stop();
        ragdollSound.time = 0f;

        if (ragdollSound.clip != null)
            ragdollSound.PlayOneShot(ragdollSound.clip, ragdollVolume);
        else
            ragdollSound.Play();
    }

    public void StopRagdollSound()
    {
        if (ragdollSound != null)
            ragdollSound.Stop();
    }

    public void PlayRagdollDisappearFX()
    {
        if (disappearEffect != null)
            disappearEffect.PlayDisappearImmediate();

        if (appearPopSFX != null)
            appearPopSFX.Play();
    }
}

