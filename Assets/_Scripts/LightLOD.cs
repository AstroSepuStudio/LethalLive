using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightLOD : MonoBehaviour
{
    [SerializeField] private Light _light;

    [Header("LOD Settings")]
    [SerializeField] float disableDistance = 60f;

    private void Start()
    {
        GameTick.OnSecond += OnSecond;
    }

    void OnSecond()
    {
        if (GameManager.Instance.LocalPlayer == null) return;

        float distance = Vector3.Distance(transform.position, GameManager.Instance.LocalPlayer.transform.position);
        if (distance >= disableDistance)
            _light.enabled = false;
        else
            _light.enabled = true;
    }
}
