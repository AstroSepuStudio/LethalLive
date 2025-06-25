using System;
using UnityEngine;

public class GameTick : MonoBehaviour
{
    public static event Action OnTick;

    [SerializeField] int _tps = 10;

    public static float TickRate { get; private set; } 
    float _nextTickTime;

    private void Awake() { TickRate = 1f / _tps; }

    private void Update()
    {
        if (Time.time >= _nextTickTime)
        {
            _nextTickTime = Time.time + TickRate;
            OnTick?.Invoke();
        }
    }

    public static void Subscribe(Action listener) { OnTick += listener; }

    public static void Unsubscribe(Action listener) { OnTick -= listener; }
}
