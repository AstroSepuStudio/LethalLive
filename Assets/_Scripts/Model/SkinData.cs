using UnityEngine;

public class SkinData : MonoBehaviour
{
    public PlayerData pData;
    public Animator CharacterAnimator;
    [HideInInspector] public Material SkinMaterial;
    public SkinnedMeshRenderer SkinRenderer;
    public Transform RightHand;
    public RagdollManager Ragdoll_Manager; 
    public RiggingManager Rigging_Manager;

    private void Awake()
    {
        SkinMaterial = SkinRenderer.material;
        SkinMaterial = new(SkinMaterial);
        SkinRenderer.material = SkinMaterial;
    }
}
