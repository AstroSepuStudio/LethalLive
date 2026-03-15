using System;
using UnityEngine;

public class ItemSpawnPosition : MonoBehaviour
{
    public Vector3 maxOffset = Vector3.one * 0.25f;
    public float chance = 100f;
    public int tries = 1;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.95f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(transform.position, maxOffset * 2f);

        Gizmos.color = new Color(1f, 0.95f, 0.2f, 0.4f);
        Gizmos.DrawSphere(transform.position, 0.08f);
    }
#endif
}
