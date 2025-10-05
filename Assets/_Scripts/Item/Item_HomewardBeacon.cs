using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Item_HomewardBeacon : ItemBase
{
    [SerializeField] GameObject placedCanvas;
    [SerializeField] Image higlightImgPlaced;
    [SerializeField] float interactionDuration = 1f;
    [SerializeField] bool _startActivated = false;

    float interactTime;
    Coroutine CheckIntTime;

    bool _placing = false;
    bool _placed = false;

    protected override void Start()
    {
        base.Start();

        if (_startActivated)
        {
            PlaceBeacon();
        }
    }

    [Server]
    public void SetPosition(Vector3 pos)
    {
        rb.isKinematic = true;
        rb.position = pos + Vector3.forward * 5;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = false;
    }

    public override void EnableCanvas()
    {
        if (_placed)
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
        higlightImgPlaced.sprite = canvas.highlightedSprite;
    }

    public override void DeselectClosest()
    {
        canvas.DeselectClosest();
        higlightImgPlaced.sprite = canvas.lowlightedSprite;
    }

    public override void OnInteract(PlayerData sourceData)
    {
        if (_placed)
        {
            Debug.Log("Homeward beacon (placed) on interaction");
            interactTime = Time.time;
            CheckIntTime = StartCoroutine(CheckInteractionTime(sourceData));
        }
        else
        {
            Debug.Log("Homeward beacon on interaction");
            base.OnInteract(sourceData);
        }
    }

    public override void OnStopInteract(PlayerData sourceData)
    {
        if (_placed)
        {
            Debug.Log("Homeward beacon (placed) on stop interaction");
            if (CheckIntTime != null) StopCoroutine(CheckIntTime);

            if ((Time.time - interactTime) < 0.2f)
            {
                _placed = false;
                sourceData.PlayerInventory.AddItem(this);
                RpcGetPlayerData(sourceData.netId);
            }
        }
        else
        {
            Debug.Log("Homeward beacon on stop interaction");
            base.OnStopInteract(sourceData);
        }
    }

    IEnumerator CheckInteractionTime(PlayerData sourceData)
    {
        while ((Time.time - interactTime) < interactionDuration)
            yield return null;

        GoBackToOffice(sourceData);

        CheckIntTime = null;
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

    public override void PrimaryAction()
    {
        // Check if there is enough space to place beacon

        // Activate animation of placing the beacon on the floor
        pData.Skin_Data.CharacterAnimator.SetBool("PlaceBeacon", true);

        pData._LockPlayer = true;
        _placing = true;
    }

    public override void CancelPrimaryAction()
    {
        if (!_placing) return;

        pData.Skin_Data.CharacterAnimator.SetBool("PlaceBeacon", false);

        pData._LockPlayer = false;
        _placing = false;
    }

    public override void PrimaryAnimationFinish()
    {
        pData.Skin_Data.CharacterAnimator.SetBool("PlaceBeacon", false);
        pData.PlayerInventory.RemoveCurrentItem();
        pData._LockPlayer = false;
        PlaceBeacon();
    }

    void PlaceBeacon()
    {
        gameObject.SetActive(true);
        transform.SetParent(null);
        rb.isKinematic = true;
        transform.up = Vector3.up;
        coll.enabled = true;
        _placing = false;
        _placed = true;
    }
}
