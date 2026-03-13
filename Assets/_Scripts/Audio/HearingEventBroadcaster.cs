using System.Collections.Generic;
using UnityEngine;

public class HearingEventBroadcaster : MonoBehaviour
{
    public static HearingEventBroadcaster Instance { get; private set; }

    private readonly List<IHearingListener> _listeners = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AddListener(IHearingListener listener)
    {
        if (!_listeners.Contains(listener))
            _listeners.Add(listener);
    }

    public void RemoveListener(IHearingListener listener)
    {
        _listeners.Remove(listener);
    }

    public void Broadcast(AudioSoundEvent soundEvent)
    {
        if (_listeners.Count == 0) return;

        float radius = soundEvent.GetRadius();

        for (int i = _listeners.Count - 1; i >= 0; i--)
        {
            if (_listeners[i] == null)
            {
                _listeners.RemoveAt(i);
                continue;
            }

            if (_listeners[i] is MonoBehaviour mb && mb != null)
            {
                float dist = Vector3.Distance(soundEvent.position, mb.transform.position);
                if (dist <= radius)
                    _listeners[i].OnSoundHeard(soundEvent);
            }
        }
    }
}
