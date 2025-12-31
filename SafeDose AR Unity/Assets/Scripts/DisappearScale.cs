using System.Collections;
using UnityEngine;

public class DisappearScale : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("How many seconds after the sequence starts before this disappear plays.")]
    public float delaySeconds = 2f;

    [Header("Scale Settings")]
    public Vector3 fromScale = Vector3.one;            // usually normal visible scale
    public Vector3 toScale   = Vector3.one * 0.01f;    // tiny / hidden
    public float scaleDuration = 0.3f;

    [Header("Position Settings (LOCAL)")]
    public Vector3 fromLocalOffset = Vector3.zero;     // visible position offset
    public Vector3 toLocalOffset   = Vector3.zero;     // where it should end up (e.g. sink down)
    public float moveDuration = 0.4f;

    [Header("Ease")]
    public AnimationCurve ease =
        AnimationCurve.EaseInOut(0, 0, 1, 1);

    Coroutine routine;
    Vector3 baseLocalPosition;

    void Awake()
    {
        // Remember editor position as base
        baseLocalPosition = transform.localPosition;
    }

    public void PlayDisappearWithDelay()
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(DisappearRoutine(delaySeconds));
    }

    public void PlayDisappearImmediate()
    {
        PlayDisappearWithCustomDelay(0f);
    }

    public void PlayDisappearWithCustomDelay(float customDelay)
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(DisappearRoutine(customDelay));
    }

    public void ResetToVisible()
    {
        if (routine != null)
            StopCoroutine(routine);

        transform.localScale = fromScale;
        transform.localPosition = baseLocalPosition + fromLocalOffset;
        routine = null;
    }

    IEnumerator DisappearRoutine(float customDelay)
    {
        // Wait for countdown
        if (customDelay > 0f)
            yield return new WaitForSeconds(customDelay);

        float tScale = 0f;
        float tMove  = 0f;

        Vector3 startPos = baseLocalPosition + fromLocalOffset;
        Vector3 endPos   = baseLocalPosition + toLocalOffset;

        // Ensure we start from the correct visible state
        transform.localScale = fromScale;
        transform.localPosition = startPos;

        while (tScale < scaleDuration || tMove < moveDuration)
        {
            if (scaleDuration > 0f && tScale < scaleDuration)
            {
                tScale += Time.deltaTime;
                float n = Mathf.Clamp01(tScale / scaleDuration);
                float e = ease.Evaluate(n);
                transform.localScale = Vector3.Lerp(fromScale, toScale, e);
            }

            if (moveDuration > 0f && tMove < moveDuration)
            {
                tMove += Time.deltaTime;
                float n = Mathf.Clamp01(tMove / moveDuration);
                float e = ease.Evaluate(n);
                transform.localPosition = Vector3.Lerp(startPos, endPos, e);
            }

            yield return null;
        }

        // Snap to final values
        transform.localScale    = toScale;
        transform.localPosition = endPos;
        routine = null;
    }
}
