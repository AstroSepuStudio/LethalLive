using System.Collections;
using Mirror;
using UnityEngine;

public class Item_Trampoline : ItemBase
{
    [Header("Trampoline")]
    [SerializeField] SkinnedMeshRenderer trampolineRenderer;
    [SerializeField] Collider[] deployedColliders;
    [SerializeField] int undeployedKey;
    [SerializeField] int bounceKey;
    [SerializeField] TriggerEventCaster triggerCaster;

    [Header("Bounce")]
    [SerializeField] AudioSFX bounceSFX;
    [SerializeField] float minBounceForce = 2.4f;
    [SerializeField] float maxBounceForce = 3f;
    [SerializeField] float bounceAnimDuration = 0.15f;

    [Header("Canvas")]
    [SerializeField] Vector3 deployedCanvasPos = new(-0.5f, 0.4f, 0);

    [SyncVar(hook = nameof(OnDeployedChanged))]
    [SerializeField] bool isDeployed;

    public void OnPlayerEnter(Collider other)
    {
        if (!isServer || !isDeployed) return;
        if (!other.CompareTag("Player")) return;

        coll.enabled = false;

        PlayerData player = GameManager.Instance.playMod.GetPlayerGameobject(other.gameObject);
        if (player == null)
            if (!other.TryGetComponent(out player)) return;
        if (player.Player_Stats.dead || player.Player_Stats.knocked) return;

        float verticalSpd = player.Player_Movement.GetVelocity().y;
        float bounceSpd = Mathf.Abs(verticalSpd) * Random.Range(minBounceForce, maxBounceForce);

        Debug.Log(bounceSpd);
        player.Player_Movement.ForceJump(bounceSpd);

        SoundLoudness loudness;
        float multiplier;
        if (bounceSpd < 5)
        {
            loudness = SoundLoudness.Quiet;
            multiplier = 0.2f;
        }
        else if (bounceSpd < 8)
        {
            loudness = SoundLoudness.Moderate;
            multiplier = 0.4f;
        }
        else if (bounceSpd < 12)
        {
            loudness = SoundLoudness.Average;
            multiplier = 0.7f;
        }
        else
        {
            loudness = SoundLoudness.Loud;
            multiplier = 1;
        }

        RpcPlayBounceAnim(player.netId, multiplier, loudness);
    }

    public void Deploy()
    {
        if (!isServer) return;
        if (isDeployed) return;

        PData.PlayerInventory.RemoveCurrentItem();
        isDeployed = true;

        foreach (var coll in deployedColliders)
        {
            coll.enabled = true;
        }
        coll.enabled = false;

        triggerCaster.EnableTrigger(true);

        RpcDeploy();
    }

    [ClientRpc]
    void RpcDeploy()
    {
        ApplyDeployVisuals(true);
    }

    public override void OnPickUp()
    {
        base.OnPickUp();

        if (isServer)
        {
            isDeployed = false;
            triggerCaster.EnableTrigger(false);
        }

        foreach (var coll in deployedColliders)
        {
            coll.enabled = false;
        }

        ApplyDeployVisuals(false);
    }

    void OnDeployedChanged(bool oldVal, bool newVal)
    {
        ApplyDeployVisuals(newVal);
    }

    void ApplyDeployVisuals(bool deployed)
    {
        trampolineRenderer.SetBlendShapeWeight(undeployedKey, deployed ? 0f : 100f);

        if (deployed)
            canvas.transform.localPosition = deployedCanvasPos;
        else
            canvas.transform.localPosition = Vector3.zero;
    }

    [ClientRpc]
    void RpcPlayBounceAnim(uint netId, float multiplier, SoundLoudness loudness)
    {
        PlayerData player = GameManager.Instance.playMod.GetPlayerByNetId(netId);
        AudioManager.Instance.PlayOneShot(itemAudioSrc, bounceSFX, multiplier, player.gameObject, loudness);
        StopAllCoroutines();
        StartCoroutine(BounceAnim());
    }

    IEnumerator BounceAnim()
    {
        float half = bounceAnimDuration * 0.5f;
        float timer = 0f;

        while (timer < half)
        {
            timer += Time.deltaTime;
            trampolineRenderer.SetBlendShapeWeight(bounceKey, Mathf.Lerp(0f, 100f, timer / half));
            yield return null;
        }

        timer = 0f;
        while (timer < half)
        {
            timer += Time.deltaTime;
            trampolineRenderer.SetBlendShapeWeight(bounceKey, Mathf.Lerp(100f, 0f, timer / half));
            yield return null;
        }

        trampolineRenderer.SetBlendShapeWeight(bounceKey, 0f);
    }
}
