using UnityEngine;

public class SkinData : MonoBehaviour
{
    [Header("References")]
    public PlayerData pData;
    public Animator CharacterAnimator;
    public Transform RightHand;
    public Transform GrabPoint;
    public SkinnedMeshRenderer SkinRenderer;
    [HideInInspector] public Material SkinMaterial;

    [Header("Mechanics")]
    public RagdollManager Ragdoll_Manager; 
    public RiggingManager Rigging_Manager;

    [Header("Breathing")]
    [SerializeField] Transform chestT;
    [SerializeField] Transform[] breathShrinkers;
    [SerializeField] Vector3 chestTS = new (1.05f, 1.05f, 1.05f);
    [SerializeField] Vector3 breathShrTS = new(0.95f, 0.95f, 0.95f);
    [SerializeField] float breathSpd = 1f;

    float breathTimer;

    [Header("Blinking")]
    [SerializeField] int blinkBSIndex;
    [SerializeField] float blkMinTime = 0.3f;
    [SerializeField] float blkMaxTime = 10f;
    [SerializeField] float blkStopDelay = 0.3f;

    int currentBlkWeight;
    float blkTimer;
    float stopBlkTimer;

    private void Awake()
    {
        SkinMaterial = SkinRenderer.material;
        SkinMaterial = new(SkinMaterial);
        SkinRenderer.material = SkinMaterial;
    }

    private void OnEnable()
    {
        GameTick.OnTick += OnTick;
    }

    private void OnDisable()
    {
        GameTick.OnTick -= OnTick;
    }

    private void OnTick()
    {
        HandleBreathing();
        HandleBlinking();
    }

    private void HandleBreathing()
    {
        breathTimer += GameTick.TickRate * breathSpd;

        float scaleFactor = (Mathf.Sin(breathTimer * Mathf.PI) + 1f) * 0.5f;

        chestT.localScale = Vector3.Lerp(Vector3.one, chestTS, scaleFactor);

        foreach (Transform t in breathShrinkers)
            t.localScale = Vector3.Lerp(Vector3.one, breathShrTS, scaleFactor);
    }

    private void HandleBlinking()
    {
        blkTimer -= GameTick.TickRate;

        if (currentBlkWeight != 0)
        {
            stopBlkTimer -= GameTick.TickRate;

            if (stopBlkTimer <= 0)
            {
                currentBlkWeight = 0;
                SkinRenderer.SetBlendShapeWeight(blinkBSIndex, 0);
            }
        }

        if (blkTimer <= 0 && currentBlkWeight != 100)
        {
            currentBlkWeight = 100;
            SkinRenderer.SetBlendShapeWeight(blinkBSIndex, 100);

            stopBlkTimer = blkStopDelay;
            blkTimer = Random.Range(blkMinTime, blkMaxTime);
        }
    }
}
