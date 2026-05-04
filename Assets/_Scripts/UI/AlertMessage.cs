using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AlertMessage : MonoBehaviour
{
    public enum Severity { Low, Medium, High }

    [SerializeField] TextMeshProUGUI labelTxt;
    [SerializeField] TextMeshProUGUI descriptionTxt;
    [SerializeField] Image alertImg;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] RectTransform rect;

    [Header("Animation")]
    [SerializeField] float moveDuration = 0.3f;
    [SerializeField] float stayDuration = 2f;
    [SerializeField] float fadeDuration = 0.3f;
    [SerializeField] float blinkDuration = 1f;
    [SerializeField] float blinkSpeed = 2f;

    [SerializeField] RectTransform startPos;
    [SerializeField] RectTransform targetPos;

    Action<AlertMessage> onFinished;

    public void SetAlertMessage(string label, string description, Severity severity)
    {
        Color clr = severity switch
        {
            Severity.Low => Color.white,
            Severity.Medium => Color.yellow,
            Severity.High => Color.red,
            _ => Color.white
        };

        labelTxt.SetText(label);
        descriptionTxt.SetText(description);

        labelTxt.color = clr;
        alertImg.color = clr;
    }

    public void Play(Action<AlertMessage> onComplete)
    {
        onFinished = onComplete;
        gameObject.SetActive(true);
        StartCoroutine(DisplayRoutine());
    }

    IEnumerator DisplayRoutine()
    {
        canvasGroup.alpha = 0.5f;
        rect.anchoredPosition = startPos.anchoredPosition;

        LeanTween.move(rect, targetPos.anchoredPosition, moveDuration)
            .setEase(LeanTweenType.easeOutQuad);

        float timer = 0f;
        bool blinkUp = true;

        while (timer < blinkDuration)
        {
            float target = blinkUp ? 0.5f : 0.3f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, Time.deltaTime * blinkSpeed);

            if (Mathf.Approximately(canvasGroup.alpha, target))
                blinkUp = !blinkUp;

            timer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(stayDuration);

        LeanTween.alphaCanvas(canvasGroup, 0f, fadeDuration)
            .setEase(LeanTweenType.easeInQuad);

        yield return new WaitForSeconds(fadeDuration);

        gameObject.SetActive(false);
        onFinished?.Invoke(this);
    }
}
