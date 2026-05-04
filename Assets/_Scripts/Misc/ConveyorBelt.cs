using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class ConveyorBelt : NetworkBehaviour
{
    [SerializeField] Renderer conveyorRenderer;

    [Header("Conveyor Settings")]
    [SerializeField] float pushForce = 10f;
    [SerializeField] float beltSpeed = 3f;
    [SerializeField] Vector3 beltDirection = Vector3.forward;

    Material maMaterial;

    readonly Vector2 offsetDir = new(0f, 1f);
    readonly System.Collections.Generic.HashSet<ItemBase> objectsOnBelt = new();
    readonly List<ItemBase> itemsToRemove = new();

    void Start()
    {
        maMaterial = new Material(conveyorRenderer.material);
        conveyorRenderer.material = maMaterial;
    }

    void Update()
    {
        maMaterial.mainTextureOffset += offsetDir * (beltSpeed * Time.deltaTime);
    }

    void FixedUpdate()
    {
        if (!isServer) return;
        foreach (var item in objectsOnBelt)
        {
            if (item == null) continue;
            if (item.HasOwner)
            {
                itemsToRemove.Add(item);
                continue;
            }

            ApplyBeltForce(item);
        }

        if (itemsToRemove.Count > 0)
        {
            foreach (var item in itemsToRemove)
                if (objectsOnBelt.Contains(item))
                    objectsOnBelt.Remove(item);

            itemsToRemove.Clear();
        }
    }

    void ApplyBeltForce(ItemBase item)
    {
        Vector3 worldDir = transform.TransformDirection(beltDirection.normalized);
        Vector3 targetVelocity = worldDir * pushForce;
        item.SetVelocity(targetVelocity);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isServer) return;

        if (collision.gameObject.TryGetComponent(out ItemBase item))
            objectsOnBelt.Add(item);
    }
}
