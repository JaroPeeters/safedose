using System.Collections;
using UnityEngine;

public class AppearScale : MonoBehaviour
{
    [Header("Scale Settings")]
    public Vector3 smallScale = Vector3.one * 0.01f;
    public Vector3 normalScale = Vector3.one;
    public float scaleDuration = 0.3f;

    [Header("Position Settings (LOCAL)")]
    public Vector3 startLocalOffset = Vector3.zero;  // where it spawns from
    public Vector3 endLocalOffset = Vector3.zero;    // usually 0 = editor position
    public float moveDuration = 0.4f;

    [Header("Ease")]
    public AnimationCurve ease =
        AnimationCurve.EaseInOut(0, 0, 1, 1);

    Coroutine routine;

    Vector3 baseLocalPosition;

    void Awake()
    {
        // remember editor position as the "true" end pose
        baseLocalPosition = transform.localPosition;

        // start invisible & offset
        transform.localScale = smallScale;
        transform.localPosition = baseLocalPosition + startLocalOffset;
    }

    public void PlayAppear()
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(AppearRoutine());
    }

    public void ResetToSmall()
    {
        if (routine != null)
            StopCoroutine(routine);

        transform.localScale = smallScale;
        transform.localPosition = baseLocalPosition + startLocalOffset;
        routine = null;
    }

    IEnumerator AppearRoutine()
    {
        float tScale = 0f;
        float tMove = 0f;

        Vector3 startPos = baseLocalPosition + startLocalOffset;
        Vector3 endPos   = baseLocalPosition + endLocalOffset;

        while (tScale < scaleDuration || tMove < moveDuration)
        {
            if (tScale < scaleDuration)
            {
                tScale += Time.deltaTime;
                float s = ease.Evaluate(Mathf.Clamp01(tScale / scaleDuration));
                transform.localScale = Vector3.Lerp(smallScale, normalScale, s);
            }

            if (tMove < moveDuration)
            {
                tMove += Time.deltaTime;
                float m = ease.Evaluate(Mathf.Clamp01(tMove / moveDuration));
                transform.localPosition = Vector3.Lerp(startPos, endPos, m);
            }

            yield return null;
        }

        // snap to final values
        transform.localScale = normalScale;
        transform.localPosition = endPos;
        routine = null;
    }
}
