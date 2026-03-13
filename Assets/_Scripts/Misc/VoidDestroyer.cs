using Mirror;
using System.Linq;
using UnityEngine;

public class VoidDestroyer : MonoBehaviour
{
    [SerializeField] string playerTag = "Player";
    [SerializeField] string[] tagsToDestroy = new string[]{
            "Item",
            "Furniture"
        };

    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkServer.active) return;

        if (tagsToDestroy.Contains(other.gameObject.tag))
            NetworkServer.Destroy(other.gameObject);

        if (other.gameObject.CompareTag(playerTag))
        {
            if (other.TryGetComponent(out PlayerStats pStats))
                pStats.ExecutePlayer();
        }
    }
}
