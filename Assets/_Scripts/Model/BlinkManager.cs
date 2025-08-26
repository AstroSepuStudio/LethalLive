using UnityEngine;

public class BlinkManager : MonoBehaviour
{
    [SerializeField] PlayerData pData;
    [SerializeField] float minTime = 0.3f;
    [SerializeField] float maxTime = 10f;

    float timer;

    private void Start()
    {
        GameTick.OnTick += OnTick;
    }

    private void OnDestroy()
    {
        GameTick.OnTick -= OnTick;
    }

    void OnTick()
    {
        if (pData == null) return;

        timer -= GameTick.TickRate;

        if (timer <= 0)
        {
            if (pData.Skin_Data.CharacterAnimator != null)
                pData.Skin_Data.CharacterAnimator.SetTrigger("Blink");
            timer = Random.Range(minTime, maxTime);
        }
    }
}
