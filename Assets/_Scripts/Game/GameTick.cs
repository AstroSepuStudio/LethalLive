using System;
using UnityEngine;

public class GameTick : MonoBehaviour
{
    public static event Action OnTick;
    public static event Action OnSecond;

    [SerializeField] int _tps = 10;

    public static float TickRate { get; private set; } 
    float _nextTickTime;
    float _nextSecond;

    private void Awake() { TickRate = 1f / _tps; }

    private void Update()
    {
        if (Time.time >= _nextTickTime)
        {
            _nextTickTime = Time.time + TickRate;
            OnTick?.Invoke();
        }

        if (Time.time > _nextSecond)
        {
            _nextSecond = Time.time + 1;
            OnSecond?.Invoke();
        }
    }
}
