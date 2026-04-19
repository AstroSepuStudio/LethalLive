using System.Collections.Generic;
using UnityEngine;

public class AlertMessagerManager : MonoBehaviour
{
    public static AlertMessagerManager Instance;

    [SerializeField] Transform alertPanel;
    [SerializeField] Canvas alertCanvas;
    [SerializeField] AlertMessage[] messagesPool;
    [SerializeField] AudioSource alertSrc;
    [SerializeField] AudioSFX alertSFX;

    int activeCount = 0;

    struct MessageData
    {
        public string label;
        public string description;
        public AlertMessage.Severity severity;
    }

    readonly Queue<MessageData> messageQueue = new();
    readonly List<AlertMessage> available = new();

    void Awake()
    {
        Instance = this;

        foreach (var msg in messagesPool)
        {
            msg.gameObject.SetActive(false);
            available.Add(msg);
        }
    }

    public void SendAlert(string label, string description, AlertMessage.Severity severity)
    {
        MessageData data = new()
        {
            label = label,
            description = description,
            severity = severity
        };

        alertCanvas.enabled = true;

        if (available.Count > 0)
        {
            ShowMessage(data);
        }
        else
        {
            messageQueue.Enqueue(data);
        }
    }

    void ShowMessage(MessageData data)
    {
        AudioManager.Instance.PlayOneShot(alertSrc, alertSFX, gameObject, SoundLoudness.NoSound);

        var msg = available[0];
        available.RemoveAt(0);

        activeCount++;

        msg.transform.SetAsFirstSibling();
        msg.transform.SetParent(alertPanel, false);
        msg.SetAlertMessage(data.label, data.description, data.severity);
        msg.Play(OnMessageFinished);
    }

    void OnMessageFinished(AlertMessage msg)
    {
        available.Add(msg);
        activeCount--;

        if (messageQueue.Count > 0)
        {
            var next = messageQueue.Dequeue();
            ShowMessage(next);
        }

        if (activeCount == 0 && messageQueue.Count == 0)
        {
            alertCanvas.enabled = false;
        }
    }
}
