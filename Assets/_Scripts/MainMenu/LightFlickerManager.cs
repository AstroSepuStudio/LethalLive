using System.Collections;
using UnityEngine;

public class LightFlickerManager : MonoBehaviour
{
    public enum LightState
    {
        Normal, 
        LongOff, QuickOff, MultipleOff, Strobe, 
        Surge, Dying, Breathing, SOS, Random
    }

    [System.Serializable]
    private struct LightBatch
    {
        public LED_Light[] Lights;
        public bool batchActive;
        [HideInInspector] public LightState State;
        [HideInInspector] public float cooldown;
        [HideInInspector] public float timer;
    }

    [SerializeField] LightBatch[] LightBatches;
    [SerializeField] float minCD = 0.1f;
    [SerializeField] float maxCD = 60f;

    // (batch, light)
    private float[][] originalIntensities;
    private bool[] batchRunning;

    #region Lifecycle

    private void Start()
    {
        batchRunning = new bool[LightBatches.Length];

        System.Array lightStates = System.Enum.GetValues(typeof(LightState));
        originalIntensities = new float[LightBatches.Length][];

        for (int i = 0; i < LightBatches.Length; i++)
        {
            LightBatches[i].State = (LightState)lightStates.GetValue(Random.Range(0, lightStates.Length));

            var batch = LightBatches[i];
            originalIntensities[i] = new float[batch.Lights.Length];
            for (int j = 0; j < batch.Lights.Length; j++)
            {
                if (batch.Lights[j] != null)
                    originalIntensities[i][j] = batch.Lights[j].Intensity;
            }

            LightBatches[i].cooldown = Random.Range(minCD, maxCD);
            LightBatches[i].timer = 0f;
        }
    }

    private void OnEnable()
    {
        if (originalIntensities == null) return;

        System.Array lightStates = System.Enum.GetValues(typeof(LightState));

        for (int i = 0; i < LightBatches.Length; i++)
        {
            LightBatches[i].State = (LightState)lightStates.GetValue(Random.Range(0, lightStates.Length));
            LightBatches[i].cooldown = Random.Range(minCD, maxCD);
            LightBatches[i].timer = 0f;
            batchRunning[i] = false;
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        for (int i = 0; i < LightBatches.Length; i++)
        {
            batchRunning[i] = false;
            for (int j = 0; j < LightBatches[i].Lights.Length; j++)
            {
                var light = LightBatches[i].Lights[j];
                if (light == null) continue;
                light.Intensity = originalIntensities[i][j];
                light.SwitchLight(true);
            }
        }
    }

    private void Update()
    {
        for (int i = 0; i < LightBatches.Length; i++)
        {
            if (!LightBatches[i].batchActive) continue;
            if (batchRunning[i]) continue;

            LightBatches[i].timer += Time.deltaTime;

            if (LightBatches[i].timer >= LightBatches[i].cooldown)
            {
                LightBatches[i].timer = 0f;
                LightBatches[i].cooldown = Random.Range(minCD, maxCD);
                StartCoroutine(RunFlickerState(i));
            }
        }
    }

    private IEnumerator RunFlickerState(int batchIndex)
    {
        var batch = LightBatches[batchIndex];
        batchRunning[batchIndex] = true;

        switch (batch.State)
        {
            case LightState.Normal:
                yield break;
            case LightState.LongOff:
                yield return FlickerLongOff(batchIndex);
                break;
            case LightState.QuickOff:
                yield return FlickerQuickOff(batchIndex);
                break;
            case LightState.MultipleOff:
                yield return FlickerMultipleOff(batchIndex);
                break;
            case LightState.Strobe:
                yield return FlickerStrobe(batchIndex);
                break;
            case LightState.Surge:
                yield return FlickerSurge(batchIndex);
                break;
            case LightState.Dying:
                yield return FlickerDying(batchIndex);
                break;
            case LightState.Breathing:
                yield return FlickerBreathing(batchIndex);
                break;
            case LightState.SOS:
                yield return FlickerSOS(batchIndex);
                break;
            case LightState.Random:
                yield return FlickerRandom(batchIndex);
                break;
        }

        batchRunning[batchIndex] = false;
    }

    #endregion

    #region Helper

    private void SetBatchEnabled(int batchIndex, bool enabled)
    {
        foreach (var light in LightBatches[batchIndex].Lights)
            if (light != null) light.SwitchLight(enabled);
    }

    private void SetBatchIntensity(int batchIndex, float intensity)
    {
        foreach (var light in LightBatches[batchIndex].Lights)
            if (light != null) light.Intensity = intensity;
    }

    private void RestoreBatchIntensity(int batchIndex)
    {
        for (int j = 0; j < LightBatches[batchIndex].Lights.Length; j++)
        {
            var light = LightBatches[batchIndex].Lights[j];
            if (light != null)
                light.Intensity = originalIntensities[batchIndex][j];
        }
    }

    #endregion

    #region States

    private IEnumerator FlickerLongOff(int i)
    {
        SetBatchEnabled(i, false);
        yield return new WaitForSeconds(Random.Range(1f, 3f));
        SetBatchEnabled(i, true);
        yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));
    }

    private IEnumerator FlickerQuickOff(int i)
    {
        SetBatchEnabled(i, false);
        yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
        SetBatchEnabled(i, true);
    }

    private IEnumerator FlickerMultipleOff(int i)
    {
        int pulses = Random.Range(2, 6);
        for (int p = 0; p < pulses; p++)
        {
            SetBatchEnabled(i, false);
            yield return new WaitForSeconds(Random.Range(0.04f, 0.1f));
            SetBatchEnabled(i, true);
            yield return new WaitForSeconds(Random.Range(0.04f, 0.12f));
        }
    }

    private IEnumerator FlickerStrobe(int i)
    {
        float duration = Random.Range(0.3f, 0.7f);
        float elapsed = 0f;
        float interval = 0.05f;
        bool state = false;
        while (elapsed < duration)
        {
            state = !state;
            SetBatchEnabled(i, state);
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
        SetBatchEnabled(i, true);
    }

    private IEnumerator FlickerSurge(int i)
    {
        float surgeIntensity = originalIntensities[i][0] * Random.Range(1.5f, 2.5f);
        SetBatchIntensity(i, surgeIntensity);
        yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
        RestoreBatchIntensity(i);

        if (Random.value > 0.5f)
        {
            yield return new WaitForSeconds(0.05f);
            SetBatchEnabled(i, false);
            yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
            SetBatchEnabled(i, true);
        }
    }

    private IEnumerator FlickerDying(int i)
    {
        int flickers = Random.Range(4, 9);
        for (int f = 0; f < flickers; f++)
        {
            float dimmed = originalIntensities[i][0] * Random.Range(0.05f, 0.6f);
            SetBatchIntensity(i, dimmed);
            yield return new WaitForSeconds(Random.Range(0.03f, 0.12f));
            RestoreBatchIntensity(i);
            yield return new WaitForSeconds(Random.Range(0.02f, 0.1f));
        }
        RestoreBatchIntensity(i);
    }

    private IEnumerator FlickerBreathing(int i)
    {
        float duration = Random.Range(1.5f, 3f);
        float elapsed = 0f;
        float baseIntensity = originalIntensities[i][0];
        float minIntensity = baseIntensity * 0.2f;

        while (elapsed < duration)
        {
            float t = Mathf.Sin((elapsed / duration) * Mathf.PI);
            SetBatchIntensity(i, Mathf.Lerp(minIntensity, baseIntensity, t));
            elapsed += Time.deltaTime;
            yield return null;
        }
        RestoreBatchIntensity(i);
    }

    private IEnumerator FlickerSOS(int i)
    {
        float dot = 0.1f, dash = 0.3f, gap = 0.1f, letterGap = 0.3f;

        // S (...)
        for (int d = 0; d < 3; d++)
        {
            SetBatchEnabled(i, false); yield return new WaitForSeconds(dot);
            SetBatchEnabled(i, true); yield return new WaitForSeconds(gap);
        }
        yield return new WaitForSeconds(letterGap);

        // O (---)
        for (int d = 0; d < 3; d++)
        {
            SetBatchEnabled(i, false); yield return new WaitForSeconds(dash);
            SetBatchEnabled(i, true); yield return new WaitForSeconds(gap);
        }
        yield return new WaitForSeconds(letterGap);

        // S (...)
        for (int d = 0; d < 3; d++)
        {
            SetBatchEnabled(i, false); yield return new WaitForSeconds(dot);
            SetBatchEnabled(i, true); yield return new WaitForSeconds(gap);
        }
    }

    private IEnumerator FlickerRandom(int i)
    {
        int pulses = Random.Range(1, 7);
        for (int p = 0; p < pulses; p++)
        {
            SetBatchEnabled(i, false);
            yield return new WaitForSeconds(Random.Range(0.03f, 0.5f));
            SetBatchEnabled(i, true);
            yield return new WaitForSeconds(Random.Range(0.03f, 0.4f));
        }
    }

    #endregion
}

