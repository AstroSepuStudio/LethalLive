using Mirror;
using System.Collections;
using TMPro;
using UnityEngine;

public class ItemBase : InteractableObject
{
    public ItemSO ItemData;

    [SerializeField] protected NetworkIdentity identity;
    [SerializeField] protected Rigidbody rb;
    [SerializeField] protected Collider coll;
    [SerializeField] protected Renderer itemRenderer;

    [SerializeField] TextMeshProUGUI itemNameTxt;
    [SerializeField] TextMeshProUGUI itemPriceTxt;

    public uint ID => identity.netId;

    [SyncVar] public int ItemValue;
    [SyncVar] public bool InUse = false;
    [SyncVar] public bool HasOwner = false;

    public PlayerData pData { get; private set; }
    public PlayerData lastPlayer { get; private set; }

    public AttackStat primaryAtkStats = new();
    public AttackStat secondaryAtkStats = new();

    Color _gizmoColor;

    protected virtual void Awake()
    {
        _gizmoColor = new Color(Random.value, Random.value, Random.value);
    }

    protected virtual void Start()
    {
        if (ItemData == null)
        {
            Debug.LogError($"[ItemBase] ItemData is not assigned on '{gameObject.name}'!", this);
            return;
        }

        if (isServer && ItemData.isSellable)
            ItemValue = Random.Range(ItemData.minValue, ItemData.maxValue);
    }

    #region Interaction

    [Server]
    public override void OnInteract(PlayerData sourceData)
    {
        if (!ItemData.pickable) return;

        if (sourceData.PlayerInventory.AddItem(this))
        {
            pData = sourceData;
            RpcGetPlayerData(sourceData.netId);
        }
    }

    [ClientRpc]
    protected void RpcGetPlayerData(uint playerID)
    {
        if (isServer) return;

        NetworkClient.spawned.TryGetValue(playerID, out NetworkIdentity netIdentity);
        if (netIdentity == null) return;
        pData = netIdentity.GetComponent<PlayerData>();
    }

    #endregion

    #region Lifecycle

    public virtual void OnPickUp()
    {
        rb.isKinematic = true;
        coll.enabled = false;
        canvas.DisableCanvas();
    }

    public virtual void OnEquip(ItemInventory handler)
    {
        gameObject.SetActive(true);
        canvas.DisableCanvas();
    }

    public virtual void OnUnequip(ItemInventory handler)
    {
        gameObject.SetActive(false);
    }

    public virtual void OnDrop(ItemInventory handler)
    {
        gameObject.SetActive(true);
        coll.enabled = true;
        transform.SetParent(null);
        rb.isKinematic = false;

        lastPlayer = pData;
        pData = null;
    }

    #endregion

    #region Visual / Misc

    public override void EnableCanvas()
    {
        canvas.EnableCanvas();

        if (itemNameTxt != null)
            itemNameTxt.SetText(ItemData.itemName);

        if (ItemData.isSellable && itemPriceTxt != null)
            itemPriceTxt.SetText($"${ItemValue}");
    }

    public void SetRender(bool render)
    {
        if (itemRenderer == null)
        {
            Debug.LogWarning($"[ItemBase] itemRenderer not assigned on '{gameObject.name}'", this);
            return;
        }

        itemRenderer.enabled = render;
    }

    public void SetCollider(bool enabled)
    {
        coll.enabled = enabled;
    }

    public void DisableColliderTemporarily(float time)
    {
        StartCoroutine(EnableColliderAfterDelay(time));
    }

    public void AddForce(Vector3 force, ForceMode mode)
    {
        rb.AddForce(force, mode);
    }

    IEnumerator EnableColliderAfterDelay(float time)
    {
        coll.enabled = false;

        float timer = 0;
        while (timer < time)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        coll.enabled = true;
    }

    #endregion

    #region Item Actions

    public virtual void PrimaryAction() { }
    public virtual void SecondaryAction() { }
    public virtual void CancelPrimaryAction() { }
    public virtual void CancelSecondaryAction() { }
    public virtual void PrimaryAnimationTrigger() { }
    public virtual void PrimaryAnimationFinish() { }
    public virtual void SecondaryAnimationTrigger() { }
    public virtual void SecondaryAnimationFinish() { }

    #endregion

    #region Gizmos

    private void OnDrawGizmos()
    {
        if (GameManager.Instance == null || !GameManager.Instance.debug) return;

        Gizmos.color = _gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }

    #endregion
}
