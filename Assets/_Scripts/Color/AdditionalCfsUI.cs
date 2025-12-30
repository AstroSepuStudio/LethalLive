using System;
using UnityEngine;

public class AdditionalCfsUI : MonoBehaviour
{
    [SerializeField] GameObject window;
    [SerializeField] GameObject[] cfs;

    int ci = 0;

    private void Start()
    {
        for (int i = 0; i < cfs.Length; i++)
        {
            cfs[i].SetActive(false);
        }
    }

    public void OpenConfigurations(int index)
    {
        if (index >= cfs.Length) 
        { Debug.LogWarning("Invalid Index"); return; }

        window.SetActive(true);
        cfs[index].SetActive(true);

        ci = index;
    }

    public void CloseConfigurations()
    {
        window.SetActive(false);
        cfs[ci].SetActive(false);
    }
}
