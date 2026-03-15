using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using static GameManager;

[Serializable]
public class DayTimeEvent
{
    public string label;
    [Range(0, 23)] public int hours;
    [Range(0, 59)] public int minutes;
    public bool triggered;
    public UnityEvent OnEventTriggered;
}

public class GM_DayCycleModule : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] GameObject dayCycleCanvas;
    [SerializeField] CanvasGroup dayGroupStart;
    [SerializeField] CanvasGroup dewCanvasGroup;
    [SerializeField] RectTransform dewInitialPos;
    [SerializeField] RectTransform dewTargetPos;
    [SerializeField] TextMeshProUGUI dayTxt;
    [SerializeField] TextMeshProUGUI dayNum;

    [SerializeField] DayTimeEvent[] dayTimeEvents;
    [SerializeField] DayTimeEvent[] clientTimeEvents;

    [Header("Audio")]
    [SerializeField] AudioSFX[] letterSfx;
    [SerializeField] AudioSFX numberSfx;
    [SerializeField] AudioSFX dewSFX;
    [SerializeField] AudioSource audioSource;

    [Header("Animation")]
    [SerializeField] float dayDuration = 900;
    [SerializeField] float letterDelay = 0.35f;

    [SerializeField] float dewMoveDuration = 1f;
    [SerializeField] float dewBlinkDuration = 4f;
    [SerializeField] float dewBlinkSpeed = 0.15f;
    [SerializeField] float dewFadeDuration = 0.5f;

    [SyncVar] public int currentDay = 1;
    [SyncVar] public bool dayStarted = false;
    [SyncVar(hook = nameof(OnDayTimeChanged))] public float currentDayTime = -1;

    public UnityEvent<int> OnDayStarted = new();
    public UnityEvent<int> OnDayEnded = new();

    private List<(float triggerTime, DayTimeEvent evt)> sortedEvents;
    private List<(float triggerTime, DayTimeEvent evt)> clientSortedEvents;
    private int nextEventIndex = 0;

    private const int DAY_START_HOUR = 8;
    private const int DAY_END_HOUR = 24;
    private const float TOTAL_IN_GAME_HOURS = DAY_END_HOUR - DAY_START_HOUR; // 24 - 8 = 16 hours dayyum
    private const float TOTAL_IN_GAME_MINUTES = TOTAL_IN_GAME_HOURS * 60;

    protected override void OnValidate()
    {
        base.OnValidate();

        foreach (var e in dayTimeEvents)
        {
            if (string.IsNullOrEmpty(e.label))
                Debug.LogWarning($"[DayCycle] An event has no label — consider naming it.");

            float t = ParseHourMinuteToTime(e.hours, e.minutes);
            if (t < 0f)
                Debug.LogWarning($"[DayCycle] Event '{e.label}' at {e.hours:00}:{e.minutes:00} is outside the day window (08:00–00:00) and will never fire.");
        }
    }

    void OnDayTimeChanged(float oldVal, float newVal)
    {
        CheckClientEvents(newVal);
    }

    void CheckClientEvents(float dayTime)
    {
        if (clientSortedEvents == null) return;

        foreach (var (triggerTime, evt) in clientSortedEvents)
        {
            if (evt.triggered)
            {
                continue;
            }
            if (dayTime >= triggerTime)
            {
                evt.triggered = true;
                evt.OnEventTriggered?.Invoke();
            }
        }
    }

    [Server]
    public void StartDay()
    {
        if (!Instance.gameStarted || !isServer) return;

        sortedEvents = new();
        foreach (var e in dayTimeEvents) sortedEvents.Add((ParseHourMinuteToTime(e.hours, e.minutes), e));
        sortedEvents.Sort((a, b) => a.triggerTime.CompareTo(b.triggerTime));
        nextEventIndex = 0;

        dayStarted = true;
        Instance.SetUpNewDay();
        OnDayStarted?.Invoke(currentDay);

        StartCoroutine(DayTimer());
        RpcStartDay();
    }

    [ClientRpc] void RpcStartDay()
    {
        clientSortedEvents = new();
        foreach (var e in clientTimeEvents)
        {
            e.triggered = false;
            float t = ParseHourMinuteToTime(e.hours, e.minutes);
            if (t < 0f) continue;

            clientSortedEvents.Add((t, e));
        }
        clientSortedEvents.Sort((a, b) => a.triggerTime.CompareTo(b.triggerTime));
    }

    public void DisplayDayStart() => StartCoroutine(DisplayDayStartAnim());
    public void DisplayDayEndWarning() => StartCoroutine(DisplayDEW());

    [Server]
    IEnumerator DayTimer()
    {
        currentDayTime = 0;
        while (currentDayTime < dayDuration)
        {
            if (!dayStarted) yield break;

            currentDayTime += Time.deltaTime;

            while (nextEventIndex < sortedEvents.Count && !sortedEvents[nextEventIndex].evt.triggered &&
                currentDayTime >= sortedEvents[nextEventIndex].triggerTime)
            {
                sortedEvents[nextEventIndex].evt.OnEventTriggered?.Invoke();
                sortedEvents[nextEventIndex].evt.triggered = true;
                nextEventIndex++;
            }

            yield return null;
        }

        FinishDay();
    }

    [Server]
    public void FinishDay()
    {
        OnDayEnded?.Invoke(currentDay);

        foreach (var player in Instance.playMod.playersOnDungeon)
        {
            player.Player_Stats.ExecutePlayer();
        }

        Instance.playMod.playersOnDungeon.Clear();

        dayStarted = false;

        Instance.dngMod.CloseDungeon();
        Instance.CheckQuotaCompletion();

        RpcFinishDay();
    }

    [ClientRpc]
    void RpcFinishDay()
    {
        clientSortedEvents?.Clear();
    }

    [Server]
    public void ResetDays()
    {
        currentDay = 1;
        currentDayTime = -1;
        dayStarted = false;
    }

    IEnumerator DisplayDayStartAnim()
    {
        yield return null;

        dayGroupStart.alpha = 1;
        dayCycleCanvas.SetActive(true);

        dayTxt.text = "";
        dayNum.text = "";
        dayNum.color = Color.white;

        string prefix = "Day";
        string numberPart = currentDay.ToString();
        WaitForSeconds letterDelay = new(this.letterDelay);
        int sfxIndex = 0;

        foreach (char c in prefix)
        {
            dayTxt.text += c;

            if (!char.IsWhiteSpace(c))
            {
                AudioManager.Instance.PlayOneShot(audioSource, letterSfx[sfxIndex]);
                sfxIndex++;
                if (sfxIndex >= letterSfx.Length) sfxIndex = 0;
            }
            else continue;

            yield return letterDelay;
        }

        dayNum.text = numberPart;
        AudioManager.Instance.PlayOneShot(audioSource, numberSfx);
        StartCoroutine(ChangeTextColor(dayNum, Color.white, Color.red, 5));

        float timer = 0;
        while (timer < 3)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        timer = 2;
        while (timer > 0)
        {
            dayGroupStart.alpha = timer / 2;
            timer -= Time.deltaTime;
            yield return null;
        }

        dayGroupStart.alpha = 0;
        dayCycleCanvas.SetActive(false);
    }

    IEnumerator ChangeTextColor(TextMeshProUGUI text, Color from, Color to, float duration)
    {
        float timer = 0;
        while (timer < duration)
        {
            text.color = Color.Lerp(from, to, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }
        text.color = to;
    }

    IEnumerator DisplayDEW()
    {
        AudioManager.Instance.PlayOneShot(audioSource, dewSFX);
        dayCycleCanvas.SetActive(true);
        dewCanvasGroup.alpha = 0.5f;
        ((RectTransform)dewCanvasGroup.transform).anchoredPosition = dewInitialPos.anchoredPosition;

        LeanTween.move((RectTransform)dewCanvasGroup.transform, dewTargetPos.anchoredPosition, dewMoveDuration)
                 .setEase(LeanTweenType.easeOutQuad);

        float blinkTimer = 0f;
        bool blinkUp = true;
        while (blinkTimer < dewBlinkDuration)
        {
            float blinkTarget = blinkUp ? 0.5f : 0.3f;
            dewCanvasGroup.alpha = Mathf.MoveTowards(dewCanvasGroup.alpha, blinkTarget, Time.deltaTime * dewBlinkSpeed);

            if (Mathf.Approximately(dewCanvasGroup.alpha, blinkTarget))
                blinkUp = !blinkUp;

            blinkTimer += Time.deltaTime;
            yield return null;
        }

        LeanTween.alphaCanvas(dewCanvasGroup, 0f, dewFadeDuration).setEase(LeanTweenType.easeInQuad);

        yield return new WaitForSeconds(dewFadeDuration);

        dayCycleCanvas.SetActive(false);
    }

    public string GetFormatedTime()
    {
        if (currentDayTime == -1 || !dayStarted)
            return "--:--";

        float inGameMinutesElapsed = currentDayTime * (TOTAL_IN_GAME_MINUTES / dayDuration);
        int totalMinutes = DAY_START_HOUR * 60 + Mathf.RoundToInt(inGameMinutesElapsed);

        // Roll over midnight
        totalMinutes %= 1440;

        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;

        return $"{hours:00}:{minutes:00}";
    }

    public float TimeStringToDayTime(string timeStr)
    {
        if (string.IsNullOrWhiteSpace(timeStr) || !timeStr.Contains(':'))
            return -1f;

        string[] parts = timeStr.Split(':');
        if (parts.Length != 2) return -1f;

        if (!int.TryParse(parts[0], out int hours) ||
            !int.TryParse(parts[1], out int minutes))
            return -1f;

        return ParseHourMinuteToTime(hours, minutes);
    }

    public float ParseHourMinuteToTime(int hours, int minutes)
    {
        int totalMinutes = hours * 60 + minutes;

        if (totalMinutes == 0) totalMinutes = 1440;

        int dayStartMinutes = DAY_START_HOUR * 60;
        int dayEndMinutes = DAY_END_HOUR * 60;

        if (totalMinutes < dayStartMinutes || totalMinutes > dayEndMinutes)
            return -1f;

        float inGameMinutesElapsed = totalMinutes - dayStartMinutes;
        return inGameMinutesElapsed * (dayDuration / TOTAL_IN_GAME_MINUTES);
    }

    public void RegisterEvent(int hours, int minutes, UnityAction callback)
    {
        float t = ParseHourMinuteToTime(hours, minutes);
        if (t < 0f)
        {
            Debug.LogWarning($"[DayCycle] RegisterEvent: {hours:00}:{minutes:00} is outside the day window.");
            return;
        }

        if (dayStarted && t <= currentDayTime)
        {
            callback?.Invoke();
            return;
        }

        var evt = new DayTimeEvent
        {
            label = $"Runtime_{hours:00}:{minutes:00}",
            hours = hours,
            minutes = minutes,
        };
        evt.OnEventTriggered.AddListener(callback);

        sortedEvents ??= new();
        sortedEvents.Add((t, evt));
        sortedEvents.Sort((a, b) => a.triggerTime.CompareTo(b.triggerTime));

        nextEventIndex = sortedEvents.FindIndex(e => !e.evt.triggered && e.triggerTime > currentDayTime);
        if (nextEventIndex == -1) nextEventIndex = sortedEvents.Count;
    }
}
