using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static CusElementUI;

public class SkinData : MonoBehaviour
{
    public enum AccessoryType { None, Upper, Legs, Feet, Extra, Hair }
    [System.Serializable]
    public struct Accessory
    { 
        public AccessoryType type;
        public string name;
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
    Dictionary<AccessoryType, Dictionary<string, Color>> _accesoryColors = new();

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
        Dictionary<string, Color> colorDict = new()
        {
            { "_MainColor", Color.white },
            { "_SecondColor", Color.lightBlue },
            { "_ThirdColor", Color.lightBlue }
        };

        _accesoryColors.Add(AccessoryType.Upper, colorDict);
        _accesoryColors.Add(AccessoryType.Legs, new(colorDict));
        _accesoryColors.Add(AccessoryType.Feet, new(colorDict));
        _accesoryColors.Add(AccessoryType.Hair, new(colorDict));
        _accesoryColors.Add(AccessoryType.Extra, new(colorDict));

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

            // Recover color from saved data if it exists
            //_accesoryColors[Accesories[i].type]["_MainColor"] = Accesories[i].renderer.material.GetColor("_MainColor");
            //_accesoryColors[Accesories[i].type]["_SecondColor"] = Accesories[i].renderer.material.GetColor("_MainColor");
            //_accesoryColors[Accesories[i].type]["_ThirdColor"] = Accesories[i].renderer.material.GetColor("_MainColor");

            Accesories[i].renderer.material.SetColor("_MainColor", Color.white);
            Accesories[i].renderer.material.SetColor("_SecondColor", Color.lightBlue);
            Accesories[i].renderer.material.SetColor("_ThirdColor", Color.lightBlue);
        }
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

    public void SetFacialGlow(float intensity) => FacialMaterial.SetFloat("_GlowIntensity", intensity);

    public void SetAccesoryColor(int index, string param, Color color)
    {
        color = ClampColorNoFullChannels(color);

        Accesories[index].renderer.material.SetColor(param, color);

        _accesoryColors[Accesories[index].type][param] = color;
    }

    public void SwitchAccesory(int oldAccesoryIndex, int newAccesoryIndex)
    {
        for (int i = 0; i < BodySkinRenderers.Length; i++)
        {
            if (Accesories[oldAccesoryIndex].bodyPartsDisabled.Contains(BodySkinRenderers[i]))
                BodySkinRenderers[i].enabled = true;

            if (Accesories[newAccesoryIndex].bodyPartsDisabled.Contains(BodySkinRenderers[i]))
                BodySkinRenderers[i].enabled = false;
        }

        Accesories[oldAccesoryIndex].renderer.enabled = false;
        Accesories[newAccesoryIndex].renderer.enabled = true;

        Accesories[newAccesoryIndex].renderer.material.SetColor("_MainColor", 
            _accesoryColors[Accesories[newAccesoryIndex].type]["_MainColor"]);

        Accesories[newAccesoryIndex].renderer.material.SetColor("_SecondColor", 
            _accesoryColors[Accesories[newAccesoryIndex].type]["_SecondColor"]);

        Accesories[newAccesoryIndex].renderer.material.SetColor("_ThirdColor",
            _accesoryColors[Accesories[newAccesoryIndex].type]["_ThirdColor"]);
    }

    public string DisableAccesory(int accesoryIndex)
    {
        for (int i = 0; i < BodySkinRenderers.Length; i++)
        {
            if (Accesories[accesoryIndex].bodyPartsDisabled.Contains(BodySkinRenderers[i]))
                BodySkinRenderers[i].enabled = true;
        }

        Accesories[accesoryIndex].renderer.enabled = false;

        return Accesories[accesoryIndex].type switch
        {
            AccessoryType.None => "Null",
            AccessoryType.Upper => "Upper Body",
            AccessoryType.Legs => "Legs",
            AccessoryType.Feet => "Feet",
            AccessoryType.Extra => "Extras",
            _ => "Null",
        };
    }

    public Color ClampColorNoFullChannels(Color color, float maxChannel = 0.99f)
    {
        color.r = Mathf.Min(color.r, maxChannel);
        color.g = Mathf.Min(color.g, maxChannel);
        color.b = Mathf.Min(color.b, maxChannel);
        return color;
    }

    public Color GetColor(ColorType type, MaterialType matType, int index)
    {
        Material mat = matType switch
        {
            MaterialType.Body => BodyMaterial,
            MaterialType.Facial => FacialMaterial,
            MaterialType.Accesory => Accesories[index].renderer.material,
            _ => null
        };

        return type switch
        {
            ColorType.Main => mat.GetColor("_MainColor"),
            ColorType.Secondary => mat.GetColor("_SecondColor"),
            ColorType.Tertiary => mat.GetColor("_ThirdColor"),
            ColorType.Mask => mat.GetColor("_MultiplyColor"),
            ColorType.Texture => mat.GetColor("_TextureColor"),
            _ => Color.white
        };
    }
}
