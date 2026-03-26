using System.Collections;
using UnityEngine;

public class SellPointSignManager : MonoBehaviour
{
    [Header("Signs")]
    [SerializeField] SellPointSign[] signs;

    [Header("Arrow")]
    [SerializeField] SellPointSign arrowSign;

    [Header("Sequential Sign Timing")]
    [SerializeField] float fadeDuration = 0.4f;
    [SerializeField] float holdOnDuration = 0.6f;
    [SerializeField] float holdOffDuration = 0.1f;
    [SerializeField] float cycleRestDuration = 1.2f;

    [Header("Arrow Flicker Timing")]
    [SerializeField] float arrowMinOn = 0.08f;
    [SerializeField] float arrowMaxOn = 0.35f;
    [SerializeField] float arrowMinOff = 0.04f;
    [SerializeField] float arrowMaxOff = 0.20f;
    [SerializeField] float arrowFade = 0.05f;

    private void OnEnable()
    {
        StartCoroutine(SequentialFlickerLoop());
        StartCoroutine(ArrowFlickerLoop());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    IEnumerator SequentialFlickerLoop()
    {
        yield return null;

        while (true)
        {
            foreach (SellPointSign sign in signs)
            {
                yield return sign.FadeRoutine(0f, 1f, fadeDuration);
                yield return new WaitForSeconds(holdOnDuration);
            }

            yield return new WaitForSeconds(cycleRestDuration);

            foreach (SellPointSign sign in signs)
                StartCoroutine(sign.FadeRoutine(1f, 0f, fadeDuration));

            yield return new WaitForSeconds(fadeDuration + holdOffDuration);
        }
    }

    IEnumerator ArrowFlickerLoop()
    {
        yield return null;

        if (arrowSign == null) yield break;

        while (true)
        {
            yield return arrowSign.FadeRoutine(0f, 1f, arrowFade);
            yield return new WaitForSeconds(Random.Range(arrowMinOn, arrowMaxOn));

            yield return arrowSign.FadeRoutine(1f, 0f, arrowFade);
            yield return new WaitForSeconds(Random.Range(arrowMinOff, arrowMaxOff));
        }
    }
}
