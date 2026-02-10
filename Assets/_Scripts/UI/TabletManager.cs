using UnityEngine;

public class TabletManager : MonoBehaviour
{
    [SerializeField] GameObject tabletObj;

    public bool CanSwitchState => currentActivities == 0;

    int currentActivities = 0;
    public bool IsActive { get; private set; }

    public void AddActivity() => currentActivities++;
    public void RemoveActivity() => currentActivities--;

    public bool TrySwitchState()
    {
        if (!CanSwitchState) return false;

        IsActive = !IsActive;
        tabletObj.SetActive(IsActive);
        return true;
    }
}
