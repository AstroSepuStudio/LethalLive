using Mirror;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using static GameManager;

public class GM_DayCycleModule : NetworkBehaviour
{
    [SerializeField] GameObject dayCycleCanvas;
    [SerializeField] CanvasGroup dayGroupStart;
    [SerializeField] CanvasGroup dayGroupAb2End;
    [SerializeField] TextMeshProUGUI dayTxt;
    [SerializeField] TextMeshProUGUI dayNum;

    [SerializeField] AudioSFX[] letterSfx;
    [SerializeField] AudioSFX numberSfx;
    [SerializeField] AudioSource audioSource;

    [SerializeField] float dayDuration = 900;
    [SerializeField] float letterDelay = 0.35f;

    [SyncVar] public int currentDay = 1;
    [SyncVar] public float currentDayTime = -1;
    [SyncVar] public bool dayStarted = false;

    public UnityEvent<int> OnDayStarted = new();
    public UnityEvent<int> OnDayEnded = new();

    private const int DAY_START_HOUR = 8;
    private const int DAY_END_HOUR = 24;
    private const float TOTAL_IN_GAME_HOURS = DAY_END_HOUR - DAY_START_HOUR; // 24 - 8 = 16 hours dayyum
    private const float TOTAL_IN_GAME_MINUTES = TOTAL_IN_GAME_HOURS * 60;

    [Server]
    public void StartDay()
    {
        if (!Instance.gameStarted || !isServer) return;

        dayStarted = true;
        Instance.SetUpNewDay();
        OnDayStarted?.Invoke(currentDay);

        StartCoroutine(DayTimer());
        StartCoroutine(DisplayDayStart());
    }

    [Server]
    IEnumerator DayTimer()
    {
        currentDayTime = 0;
        while (currentDayTime < dayDuration)
        {
            if (!dayStarted) yield break;

            currentDayTime += Time.deltaTime;
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
    }

    [Server]
    public void ResetDays()
    {
        currentDay = 1;
        dayStarted = false;
    }

    IEnumerator DisplayDayStart()
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
        if (Instance.dayMod.currentDayTime == -1 || !Instance.dayMod.dayStarted)
            return "--:--";

        float inGameMinutesElapsed = currentDayTime * (TOTAL_IN_GAME_MINUTES / dayDuration);
        int totalMinutes = DAY_START_HOUR * 60 + Mathf.RoundToInt(inGameMinutesElapsed);

        // Roll over midnight
        totalMinutes %= 1440;

        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;

        return $"{hours:00}:{minutes:00}";
    }
}
