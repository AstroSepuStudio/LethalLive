using Mirror;
using System.Collections;
using TMPro;
using UnityEngine;

public class SellPoint : NetworkBehaviour
{
    [SerializeField] TextMeshProUGUI labelTxt;
    [SerializeField] TextMeshProUGUI totalBalanceTxt;
    [SerializeField] TextMeshProUGUI additionalMoneyTxt;
    [SerializeField] string originalLabel = "Be a good asset";

    string currentLabel;

    [Header("Animation Settings")]
    [SerializeField] float counterRollDuration = 0.8f;
    [SerializeField] float labelDuration = 2f;
    [SerializeField] float popupFloatDuration = 1.4f;
    [SerializeField] float popupFloatHeight = 80f;
    [SerializeField] AnimationCurve popupCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] Color colorBelowQuota = Color.red;
    [SerializeField] Color colorMetQuota = Color.green;

    [Header("Audio")]
    [SerializeField] AudioSource src;
    [SerializeField] AudioSFX[] typingSFX;
    [SerializeField] AudioSFX cashOut;
    [SerializeField] float typingSFXInterval = 0.08f;

    GM_EconomyModule EcoMod => GameManager.Instance.ecoMod;
    GM_DayCycleModule DayMod => GameManager.Instance.dayMod;

    private float _displayedBalance;
    private Coroutine _rollCoroutine;
    private Coroutine _popupCoroutine;
    private Coroutine _labelCoroutine;

    private static readonly (float minGain, string[] frames)[] LabelReactions =
    {
        (100f, new[] { "great!!", "great asset!!!", "!!!!!", "!!!!!!!" }),
        ( 50f, new[] { "!! !!",  "!!!!",   "!! !!", "!!!!"    }),
        ( 10f, new[] { "this is trash!!!",  "?!?!?",  "!?!?!", "get out!?!!"   }),
        (  0f, new[] { "you are getting executed", "execution",  "wtf!!", "you are done for"  }),
    };

    private void Start()
    {
        _displayedBalance = EcoMod.TotalBalance;
        RefreshBalanceImmediate();

        currentLabel = originalLabel;

        EcoMod.OnTeamBalanceChangedEv.AddListener(RefreshBalance);
        EcoMod.OnQuotaChangedEv.AddListener(OnQuotaChanged);
        DayMod.OnDayEnded.AddListener(RefreshBalance);
    }

    private void OnDestroy()
    {
        EcoMod.OnTeamBalanceChangedEv.RemoveListener(RefreshBalance);
        EcoMod.OnQuotaChangedEv.RemoveListener(OnQuotaChanged);
        DayMod.OnDayEnded.RemoveListener(RefreshBalance);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        if (other.TryGetComponent(out ItemBase item))
        {
            if (item.LastPlayer != null && item.ItemData.isSellable)
            {
                PlayerTeam team = item.LastPlayer.Team;
                float current = EcoMod.teamsBalance[team];
                float newBalance = current + item.ItemValue;

                EcoMod.teamsBalance[team] = newBalance;

                NetworkServer.Destroy(item.gameObject);
            }
        }
    }

    public void OnQuotaChanged(int newValue)
    {
        currentLabel = EcoMod.TotalBalance >= EcoMod.targetQuota ? "quota met" : originalLabel;
        labelTxt.SetText(currentLabel);

        UpdateBalanceText(newValue);
    }

    public void RefreshBalance(PlayerTeam t, float v) => RefreshBalance();
    public void RefreshBalance(int day) => RefreshBalance();

    public void RefreshBalance()
    {
        float newTotal = EcoMod.TotalBalance;
        float gained = newTotal - _displayedBalance;

        if (_rollCoroutine != null) StopCoroutine(_rollCoroutine);
        if (_popupCoroutine != null) StopCoroutine(_popupCoroutine);
        if (_labelCoroutine != null) StopCoroutine(_labelCoroutine);

        if (EcoMod.TotalBalance >= EcoMod.targetQuota) currentLabel = "quota met";

        AudioManager.Instance.PlayOneShot(src, cashOut, gameObject, SoundLoudness.NoSound);

        _rollCoroutine = StartCoroutine(RollCounter(_displayedBalance, newTotal));
        _labelCoroutine = StartCoroutine(AnimateLabel(gained));

        if (gained > 0)
        {
            if (_popupCoroutine != null) StopCoroutine(_popupCoroutine);
            _popupCoroutine = StartCoroutine(FloatingPopup(gained));
        }

        StartCoroutine(PulseText(totalBalanceTxt));
    }

    private void RefreshBalanceImmediate()
    {
        UpdateBalanceText(_displayedBalance);
        additionalMoneyTxt.alpha = 0f;
    }

    private void UpdateBalanceText(float balance)
    {
        float quota = EcoMod.targetQuota;
        string sign = balance < quota ? "<" : balance > quota ? ">" : "=";
        Color col = balance < quota ? colorBelowQuota : colorMetQuota;

        Debug.Log($"[SellPoint] Balance: {balance}; Quota: {quota}");

        totalBalanceTxt.color = col;
        totalBalanceTxt.SetText($"${balance:0} {sign} ${quota:0}");
    }

    private IEnumerator RollCounter(float from, float to)
    {
        float elapsed = 0f;
        float nextSFXTime = 0f;
        int sfxIdx = 0;

        while (elapsed < counterRollDuration)
        {
            elapsed += Time.deltaTime;

            if (typingSFX.Length > 0 && elapsed >= nextSFXTime)
            {
                AudioManager.Instance.PlayOneShot(src, typingSFX[sfxIdx % typingSFX.Length], gameObject, SoundLoudness.NoSound);
                sfxIdx++;
                nextSFXTime = elapsed + typingSFXInterval;
            }

            float t = Mathf.SmoothStep(0f, 1f, elapsed / counterRollDuration);
            _displayedBalance = Mathf.Lerp(from, to, t);
            totalBalanceTxt.color = _displayedBalance < EcoMod.targetQuota ? colorBelowQuota : colorMetQuota;
            totalBalanceTxt.SetText($"${_displayedBalance:0}/${EcoMod.targetQuota}");
            yield return null;
        }
        _displayedBalance = to;
        totalBalanceTxt.color = _displayedBalance < EcoMod.targetQuota ? colorBelowQuota : colorMetQuota;
        totalBalanceTxt.SetText($"${to:0}/${EcoMod.targetQuota}");
    }

    private IEnumerator AnimateLabel(float gained)
    {
        string[] frames = LabelReactions[^1].frames;
        foreach (var (minGain, f) in LabelReactions)
        {
            if (gained >= minGain) { frames = f; break; }
        }

        float elapsed = 0f;
        float framerate = 0.12f;
        float nextSwap = 0f;
        int idx = 0;

        while (elapsed < labelDuration)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= nextSwap)
            {
                labelTxt.SetText(frames[idx % frames.Length]);
                idx++;
                nextSwap = elapsed + framerate;
            }
            yield return null;
        }

        labelTxt.SetText(currentLabel);
    }

    private IEnumerator FloatingPopup(float amount)
    {
        additionalMoneyTxt.SetText($"+${amount:0}");

        RectTransform rt = additionalMoneyTxt.rectTransform;
        Vector2 startPos = rt.anchoredPosition;
        Color c = additionalMoneyTxt.color;

        float elapsed = 0f;
        while (elapsed < popupFloatDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popupFloatDuration;

            float yOffset = popupCurve.Evaluate(t) * popupFloatHeight;
            rt.anchoredPosition = startPos + new Vector2(0f, yOffset);

            float alpha 
                = t < 0.15f ? Mathf.InverseLerp(0f, 0.15f, t)
                : t > 0.65f ? Mathf.InverseLerp(1f, 0.65f, t) : 1f;

            additionalMoneyTxt.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }
        
        rt.anchoredPosition = startPos;
        additionalMoneyTxt.color = new Color(c.r, c.g, c.b, 0f);
    }

    private IEnumerator PulseText(TextMeshProUGUI tmp)
    {
        const float duration = 0.25f;
        const float peakScale = 1.18f;
        Transform t = tmp.transform;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / duration;
            float scale = p < 0.4f
                ? Mathf.Lerp(1f, peakScale, p / 0.4f)
                : Mathf.Lerp(peakScale, 1f, (p - 0.4f) / 0.6f);
            t.localScale = Vector3.one * scale;
            yield return null;
        }
        t.localScale = Vector3.one;
    }
}
