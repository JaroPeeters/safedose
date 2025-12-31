using UnityEngine;

public class ParticleTrigger : MonoBehaviour
{
    public ParticleSystem appearEffect;

    void Start()
    {
        if (appearEffect != null)
            appearEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void PlayAppearEffect(float delay = 0f)
    {
        StartCoroutine(PlayDelayed(delay));
    }

    private System.Collections.IEnumerator PlayDelayed(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (appearEffect == null) yield break;

        appearEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        appearEffect.Play(true);
    }

    public void StopEffect()
    {
        if (appearEffect == null) return;

        appearEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
