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
    [SerializeField] TextMeshProUGUI dayTxt;
    [SerializeField] TextMeshProUGUI dayNum;

    [SerializeField] GameObject dayEndGO;
    [SerializeField] CanvasGroup quotaStateGroup;
    [SerializeField] CanvasGroup newQuotaGroup;
    [SerializeField] TextMeshProUGUI quotaLabelTxt;
    [SerializeField] TextMeshProUGUI quotaMetStateTxt;
    [SerializeField] TextMeshProUGUI newQuotaLabelTxt;
    [SerializeField] TextMeshProUGUI newQuotaValueTxt;

    [SerializeField] DayTimeEvent[] dayTimeEvents;
    [SerializeField] DayTimeEvent[] clientTimeEvents;

    [Header("Audio")]
    [SerializeField] AudioSFX[] letterSfx;
    [SerializeField] AudioSFX numberSfx;
    [SerializeField] AudioSFX dewSFX;
    [SerializeField] AudioSFX intermissionSFX;
    [SerializeField] AudioSFX boomBasic;
    [SerializeField] AudioSFX boomDanger;
    [SerializeField] AudioSFX boomSnare;

    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioSource speakerSrc;

    [Header("Animation")]
    [SerializeField] float dayDuration = 900;
    [SerializeField] float letterDelay = 0.35f;

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
        OnDayStarted?.Invoke(currentDay);

        StartCoroutine(DayTimer());
        RpcStartDay();
    }

    [ClientRpc] void RpcStartDay()
    {
        if (currentDay == 1)
            AudioManager.Instance.PlayOneShot(speakerSrc, intermissionSFX, gameObject, SoundLoudness.NoSound);

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
    public void DisplayDayEndWarning() => AlertMessagerManager.Instance.SendAlert(
        "Watch your clock!",
        "Return to the office. The connection will be lost at 12 PM",
        AlertMessage.Severity.High);

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
        bool quotaMet = Instance.ecoMod.IsQuotaMet;
        int quotaTarget = Instance.ecoMod.targetQuota;
        int nextQuota = Instance.ecoMod.ComputeAndStageNextQuota(currentDay + 1);

        if (quotaMet) Instance.ecoMod.TakeQuotaValue();

        Instance.onDeadTime = true;

        RpcFinishDay(quotaMet, nextQuota);

        StartCoroutine(PostAnimationSequence(quotaMet));
    }

    [ClientRpc]
    void RpcFinishDay(bool quotaMet, int nextQuota)
    {
        clientSortedEvents?.Clear();
        StartCoroutine(FinishDayAnimation(quotaMet, nextQuota));
    }

    IEnumerator FinishDayAnimation(bool quotaMet, int nextQuota)
    {
        quotaLabelTxt.enabled = false;
        quotaMetStateTxt.enabled = false;
        newQuotaLabelTxt.enabled = false;
        newQuotaValueTxt.enabled = false;
        quotaStateGroup.alpha = 0f;
        newQuotaGroup.alpha = 0f;

        dayEndGO.SetActive(true);
        dayCycleCanvas.SetActive(true);

        quotaLabelTxt.enabled = true;
        AudioManager.Instance.PlayOneShot(audioSource, boomBasic);

        yield return StartCoroutine(FadeCanvasGroup(quotaStateGroup, 0f, 1f, 1f));
        yield return new WaitForSeconds(0.5f);

        quotaMetStateTxt.SetText(quotaMet ? "Met" : "Not Met");
        quotaMetStateTxt.color = Color.white;
        quotaMetStateTxt.enabled = true;

        if (!quotaMet)
        {
            AudioManager.Instance.PlayOneShot(audioSource, boomDanger);
            float colorTimer = 0f;
            float colorDuration = 1.5f;
            while (colorTimer < colorDuration)
            {
                colorTimer += Time.deltaTime;
                quotaMetStateTxt.color = Color.Lerp(Color.white, Color.red, colorTimer / colorDuration);
                yield return null;
            }
            quotaMetStateTxt.color = Color.red;
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            AudioManager.Instance.PlayOneShot(audioSource, boomSnare);
            quotaMetStateTxt.color = Color.green;
            yield return new WaitForSeconds(2f);
        }

        yield return StartCoroutine(FadeCanvasGroup(quotaStateGroup, 1f, 0f, 1f));
        quotaLabelTxt.enabled = false;
        quotaMetStateTxt.enabled = false;

        if (quotaMet)
        {
            yield return new WaitForSeconds(0.3f);

            AudioManager.Instance.PlayOneShot(audioSource, boomBasic);
            newQuotaLabelTxt.enabled = true;
            yield return StartCoroutine(FadeCanvasGroup(newQuotaGroup, 0f, 1f, 1f));

            newQuotaValueTxt.enabled = true;
            yield return StartCoroutine(RollNumber(newQuotaValueTxt, 0, nextQuota, 1.5f));

            yield return new WaitForSeconds(1.5f);

            yield return StartCoroutine(FadeCanvasGroup(newQuotaGroup, 1f, 0f, 1f));
            newQuotaLabelTxt.enabled = false;
            newQuotaValueTxt.enabled = false;
        }

        dayEndGO.SetActive(false);
    }

    IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        float timer = 0f;
        group.alpha = from;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, timer / duration);
            yield return null;
        }
        group.alpha = to;
    }

    IEnumerator RollNumber(TextMeshProUGUI label, int from, int to, float duration)
    {
        float timer = 0f;
        float nextSFXTime = 0f;
        int sfxIdx = 0;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            if (letterSfx.Length > 0 && timer >= nextSFXTime)
            {
                AudioManager.Instance.PlayOneShot(audioSource, letterSfx[sfxIdx % letterSfx.Length], gameObject, SoundLoudness.NoSound);
                sfxIdx++;
                nextSFXTime = timer + 0.08f;
            }

            float t = Mathf.SmoothStep(0f, 1f, timer / duration);
            int current = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
            label.SetText($"${current}");
            yield return null;
        }
        label.SetText($"${to}");
    }

    [Server]
    IEnumerator PostAnimationSequence(bool quotaMet)
    {
        yield return new WaitForSeconds(2.5f);

        foreach (var player in Instance.playMod.playersOnDungeon)
            player.Player_Stats.ExecutePlayer();

        Instance.playMod.playersOnDungeon.Clear();
        
        yield return new WaitForSeconds(3f);

        Instance.CheckQuotaCompletion(quotaMet);
        Instance.onDeadTime = false;

        OnDayEnded?.Invoke(currentDay);
        Instance.dngMod.CloseDungeon();

        dayStarted = false;
    }

    [Server]
    public void ResetDays()
    {
        Instance.playMod.playersOnDungeon.Clear();
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

    public string GetFormatedTime()
    {
        if (currentDayTime == -1 || !dayStarted)
            return "--:--";

        float inGameMinutesElapsed = currentDayTime * (TOTAL_IN_GAME_MINUTES / dayDuration);
        int totalMinutes = DAY_START_HOUR * 60 + Mathf.RoundToInt(inGameMinutesElapsed);

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
