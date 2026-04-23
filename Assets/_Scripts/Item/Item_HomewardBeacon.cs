using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Item_HomewardBeacon : ItemBase
{
    [SerializeField] GameObject placedCanvas;
    [SerializeField] Image highlightImgPlaced;
    [SerializeField] float interactionDuration = 1f;
    [SerializeField] bool startActivated = false;

    float interactTime;
    Coroutine checkIntTime;

    bool placed = false;

    protected void Start()
    {
        if (startActivated)
            PlaceBeacon();
    }

    [Server]
    public void SetPosition(Vector3 pos)
    {
        rb.isKinematic = true;
        rb.position = pos + Vector3.forward * 5;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = false;
    }

    #region Canvas Overrides

    public override void EnableCanvas()
    {
        if (placed)
            placedCanvas.SetActive(true);
        else
            canvas.EnableCanvas();
    }

    public override void DisableCanvas()
    {
        placedCanvas.SetActive(false);
        canvas.DisableCanvas();
    }

    public override void SelectClosest()
    {
        canvas.SelectClosest();
        //highlightImgPlaced.sprite = canvas.highlightedSprite;
    }

    public override void DeselectClosest()
    {
        canvas.DeselectClosest();
        //highlightImgPlaced.sprite = canvas.lowlightedSprite;
    }

    #endregion

    #region Interaction Overrides

    [Server]
    public override void OnInteract(PlayerData sourceData)
    {
        if (placed)
        {
            interactTime = Time.time;
            checkIntTime = StartCoroutine(CheckInteractionTime(sourceData));
        }
        else
        {
            base.OnInteract(sourceData);
        }
    }

    [Server]
    public override void OnStopInteract(PlayerData sourceData)
    {
        if (placed)
        {
            if (checkIntTime != null) StopCoroutine(checkIntTime);

            if ((Time.time - interactTime) < 0.2f)
            {
                placed = false;
                sourceData.PlayerInventory.AddItem(this);
                RpcGetPlayerData(sourceData.netId);
            }
        }
        else
        {
            base.OnStopInteract(sourceData);
        }
    }

    IEnumerator CheckInteractionTime(PlayerData sourceData)
    {
        while ((Time.time - interactTime) < interactionDuration)
            yield return null;

        GoBackToOffice(sourceData);
        checkIntTime = null;
    }

    [Server]
    void GoBackToOffice(PlayerData sourceData)
    {
        sourceData.Character_Controller.enabled = false;
        sourceData.Character_Controller.transform.position = GameManager.Instance.transform.position;
        sourceData.Character_Controller.enabled = true;

        DisableCanvas();
        AudioManager.Instance.StopMusic();
    }

    #endregion

    #region Placement

    public void PlaceBeacon()
    {
        gameObject.SetActive(true);
        transform.SetParent(null);
        rb.isKinematic = true;
        transform.up = Vector3.up;
        coll.enabled = true;
        placed = true;
    }

    #endregion
}
