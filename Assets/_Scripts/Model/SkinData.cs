using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkinData : MonoBehaviour
{
    public enum AccessoryType { None, Upper, Legs, Feet, Extra }
    [System.Serializable]
    public struct Accessory
    { 
        public AccessoryType type;
        public SkinnedMeshRenderer renderer;
        public SkinnedMeshRenderer[] bodyPartsDisabled;
    }

    [Header("References")]
    public PlayerData pData;
    public Animator CharacterAnimator;
    public Transform RightHand;
    public Transform GrabPoint;
    public SkinnedMeshRenderer[] BodySkinRenderers;
    public SkinnedMeshRenderer[] FacialRenderers;
    public Accessory[] Accesories;

    Material BodyMaterial;
    Material FacialMaterial;
    Dictionary<string, Color> _accesoryColors = new();

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
    [SerializeField] SkinnedMeshRenderer blinkMesh;
    [SerializeField] float halfCloseTime = 0.03f;
    [SerializeField] float fullCloseTime = 0.05f;
    [SerializeField] float halfOpenTime = 0.03f;
    [SerializeField] float blkMinTime = 0.2f;
    [SerializeField] float blkMaxTime = 10f;

    private void Awake()
    {
        BodyMaterial = new Material(BodySkinRenderers[0].material);
        FacialMaterial = new Material(FacialRenderers[0].material);

        for (int i = 0; i < BodySkinRenderers.Length; i++)
        {
            BodySkinRenderers[i].material = BodyMaterial;
        }

        for (int i = 0; i < FacialRenderers.Length; i++)
        {
            FacialRenderers[i].material = FacialMaterial;
        }

        for (int i = 0; i < Accesories.Length; i++)
        {
            Accesories[i].renderer.material = new(Accesories[i].renderer.material);
        }

        _accesoryColors.Add("_MainColor", Color.white);
        _accesoryColors.Add("_SecondColor", Color.lightBlue);
        _accesoryColors.Add("_ThirdColor", Color.lightBlue);
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

    enum BlinkState { None, HalfClose, FullClose, HalfOpen, Open }
    BlinkState blinkState = BlinkState.None;
    float blinkStepTimer;
    float blkTimer;

    private void HandleBlinking()
    {
        if (blinkState == BlinkState.None)
        {
            blkTimer -= GameTick.TickRate;

            if (blkTimer <= 0)
            {
                blinkState = BlinkState.HalfClose;
                blinkStepTimer = halfCloseTime;

                blinkMesh.SetBlendShapeWeight(0, 100);
                blinkMesh.SetBlendShapeWeight(1, 0);

                blkTimer = Random.Range(blkMinTime, blkMaxTime);
            }

            return;
        }

        blinkStepTimer -= GameTick.TickRate;

        if (blinkStepTimer > 0)
            return;

        switch (blinkState)
        {
            case BlinkState.HalfClose:
                blinkState = BlinkState.FullClose;
                blinkStepTimer = fullCloseTime;

                blinkMesh.SetBlendShapeWeight(0, 0);
                blinkMesh.SetBlendShapeWeight(1, 100);
                break;

            case BlinkState.FullClose:
                blinkState = BlinkState.HalfOpen;
                blinkStepTimer = halfOpenTime;

                blinkMesh.SetBlendShapeWeight(1, 0);
                blinkMesh.SetBlendShapeWeight(0, 100);
                break;

            case BlinkState.HalfOpen:
                blinkState = BlinkState.Open;

                blinkMesh.SetBlendShapeWeight(0, 0);
                blinkMesh.SetBlendShapeWeight(1, 0);
                break;

            case BlinkState.Open:
                blinkState = BlinkState.None;
                break;
        }
    }

    public void SetBodyColor(string param, Color color) => BodyMaterial.SetColor(param, ClampColorNoFullChannels(color));

    public void SetFacialColor(string param, Color color) => FacialMaterial.SetColor(param, ClampColorNoFullChannels(color));

    public void SetAccesoryColor(int index, string param, Color color)
    {
        color = ClampColorNoFullChannels(color);

        Accesories[index].renderer.material.SetColor(param, color);

        if (_accesoryColors.ContainsKey(param))
            _accesoryColors[param] = color;
        else
            _accesoryColors.Add(param, color);
    }

    public void SwitchAccesory(int accesoryIndex)
    {
        for (int i = 0; i < BodySkinRenderers.Length; i++)
        {
            if (Accesories[accesoryIndex].bodyPartsDisabled.Contains(BodySkinRenderers[i]))
                BodySkinRenderers[i].enabled = false;
            else
                BodySkinRenderers[i].enabled = true;
        }

        for (int i = 0; i < Accesories.Length; i++)
        {
            if (Accesories[accesoryIndex].type != Accesories[i].type) continue;

            if (i != accesoryIndex)
                Accesories[i].renderer.enabled = false;
            else
            {
                Accesories[i].renderer.enabled = true;
                Accesories[i].renderer.material.SetColor("_MainColor", _accesoryColors["_MainColor"]);
                Accesories[i].renderer.material.SetColor("_SecondColor", _accesoryColors["_SecondColor"]);
                Accesories[i].renderer.material.SetColor("_ThirdColor", _accesoryColors["_ThirdColor"]);
            }
        }
    }

    public Color ClampColorNoFullChannels(Color color, float maxChannel = 0.99f)
    {
        color.r = Mathf.Min(color.r, maxChannel);
        color.g = Mathf.Min(color.g, maxChannel);
        color.b = Mathf.Min(color.b, maxChannel);
        return color;
    }
}
