using UnityEngine;

public class Item_HealingSyringe : ItemBase
{
    [SerializeField] AudioSFX usageSFX;
    [SerializeField] SkinnedMeshRenderer skRenderer;
    [SerializeField] float overtimeHealing = 10f;
    [SerializeField] float completedHealing = 15f;
    [SerializeField] float minUsageDelay = 0.7f;
    [SerializeField] float maxUsageDelay = 1.3f;

    IA_Inject injectAction;
    float totalHealed;
    float maxTotalHeal;
    float usageTimer;

    public override void OnStartServer()
    {
        base.OnStartServer();
        Setup();
    }

    public override bool IsUsageCriteriaMet() => PData.Player_Stats.currentHP < PData.Player_Stats.maxHP;

    void Start()
    {
        if (!isServer) Setup();
    }

    void Setup()
    {
        injectAction = PrimaryAction as IA_Inject;
        if (injectAction == null) injectAction = SecondaryAction as IA_Inject;
        if (injectAction == null) return;

        maxTotalHeal = overtimeHealing + completedHealing;
        usageTimer = Random.Range(minUsageDelay, maxUsageDelay);

        totalHealed = 0f;
        skRenderer.SetBlendShapeWeight(0, 0);

        injectAction.OnOvertimeInjection.AddListener(OnOvertimeHeal);
        injectAction.OnInjectionCompleted.AddListener(OnInjectionCompleted);
    }

    void OnDestroy()
    {
        if (injectAction == null) return;
        injectAction.OnOvertimeInjection.RemoveListener(OnOvertimeHeal);
        injectAction.OnInjectionCompleted.RemoveListener(OnInjectionCompleted);
    }

    void OnOvertimeHeal(float delta)
    {
        if (PData == null) return;

        float heal = Mathf.Min(delta * overtimeHealing, overtimeHealing - totalHealed);
        if (heal <= 0f) return;

        usageTimer -= Time.deltaTime;
        if (usageTimer < 0)
        {
            AudioManager.Instance.PlayOneShot(itemAudioSrc, usageSFX, PData.gameObject, SoundLoudness.Moderate);
            usageTimer = Random.Range(minUsageDelay, maxUsageDelay);
        }

        totalHealed += heal;
        PData.Player_Stats.RestoreHealth(heal);

        float consumed = Mathf.Clamp01(totalHealed / maxTotalHeal);
        skRenderer.SetBlendShapeWeight(0, consumed * 100f);
    }

    void OnInjectionCompleted()
    {
        if (PData == null) return;

        float remainingHeal = Mathf.Max(0f, maxTotalHeal - totalHealed);
        totalHealed = maxTotalHeal;

        skRenderer.SetBlendShapeWeight(0, 100);

        if (remainingHeal > 0f)
            PData.Player_Stats.RestoreHealth(remainingHeal);
    }
}
