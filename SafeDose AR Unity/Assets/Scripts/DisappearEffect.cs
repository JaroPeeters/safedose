using System.Collections;
using UnityEngine;

public class DisappearEffect : MonoBehaviour
{
    [Header("Effect")]
    public ParticleSystem disappearEffect;

    [Header("SFX")]
    public AudioSource disappearSFX;

    [Header("Timing")]
    [Tooltip("How many seconds after the sequence starts before this effect plays.")]
    public float delaySeconds = 2f;

    Coroutine routine;

    void Start()
    {
        if (disappearEffect != null)
            disappearEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void PlayDisappearWithDelay()
    {
        PlayDisappearWithCustomDelay(delaySeconds);
    }

    public void PlayDisappearImmediate()
    {
        PlayDisappearWithCustomDelay(0f);
    }

    public void PlayDisappearWithCustomDelay(float customDelay)
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(PlayDelayed(customDelay));
    }

    public void StopEffect()
    {
        if (disappearEffect == null) return;

        disappearEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    IEnumerator PlayDelayed(float customDelay)
    {
        if (customDelay > 0f)
            yield return new WaitForSeconds(customDelay);

        if (disappearEffect != null)
        {
            disappearEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            disappearEffect.Play(true);
        }

        if (disappearSFX != null)
        {
            disappearSFX.Play();
            // Or: disappearSFX.PlayOneShot(disappearSFX.clip);
        }

        routine = null;
    }
}
