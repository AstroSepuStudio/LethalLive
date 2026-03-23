using UnityEngine;
using UnityEngine.Events;

public class AIModule_Patience : AIModule
{
    public enum Personality { Passive, Cautious, Neutral, Irritated, Hostile }

    [Header("Decay")]
    [SerializeField] float patienceDecayDistance = 4f;
    [SerializeField] float patienceDecay = 2f;
    [SerializeField] float patienceDecayOnBackAway = 5f;
    [SerializeField] float patienceDecayOnItemStolen = 25f;

    public Personality CurrentPersonality { get; private set; }
    public float Patience => patience;
    public float MaxPatience => maxPatience;
    public bool IsExhausted => patience <= 0f;

    float patience;
    float maxPatience;

    public UnityEvent OnPatienceExhausted;

    public override void OnModuleInit(AIBrain brain)
    {
        if (!brain.isServer) return;
        AssignPersonality();
    }

    public override void OnModuleTick(AIBrain brain)
    {
        if (patience <= 0f) return;
        Tick(brain);
    }

    public void Drain(float amount)
    {
        patience -= amount;
        if (patience <= 0f)
        {
            patience = 0f;
            OnPatienceExhausted?.Invoke();
        }
    }

    public void DrainOnBackAway() => Drain(patienceDecayOnBackAway);
    public void DrainOnItemStolen() => Drain(patienceDecayOnItemStolen);

    public void Restore(float fraction = 1f)
    {
        patience = maxPatience * Mathf.Clamp01(fraction);
    }

    public void SuppressTick(bool suppressed) => tickSuppressed = suppressed;

    bool tickSuppressed;

    void Tick(AIBrain brain)
    {
        if (tickSuppressed) return;

        if (!brain.TryGetModule<AIModule_Senses>(out var senses)) return;

        PlayerData closest = senses.GetClosestSeenPlayer(brain);

        if (closest == null) return;

        float dist = Vector3.Distance(brain.transform.position, closest.transform.position);
        float proximity = 1f - Mathf.Clamp01(dist / patienceDecayDistance);
        patience -= patienceDecay * proximity * Time.deltaTime;

        if (patience <= 0f)
        {
            patience = 0f;
            OnPatienceExhausted?.Invoke();
        }
    }

    void AssignPersonality()
    {
        AssignPersonalityFromValue(Random.Range(0, 100));
    }

    public void AssignPersonalityFromValue(int value)
    {
        if (value < 15) CurrentPersonality = Personality.Passive;
        else if (value < 35) CurrentPersonality = Personality.Cautious;
        else if (value < 60) CurrentPersonality = Personality.Neutral;
        else if (value < 80) CurrentPersonality = Personality.Irritated;
        else CurrentPersonality = Personality.Hostile;

        maxPatience = CurrentPersonality switch
        {
            Personality.Passive  => Random.Range(120f, 180f),
            Personality.Cautious => Random.Range(70f, 120f),
            Personality.Neutral  => Random.Range(35f, 70f),
            Personality.Irritated => Random.Range(12f, 35f),
            Personality.Hostile  => Random.Range(2f, 12f),
            _ => 60f
        };

        patience = maxPatience;
    }
}
