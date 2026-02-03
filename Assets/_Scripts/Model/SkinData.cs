using Mirror;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static CusElementUI;

public class SkinData : NetworkBehaviour
{
    public readonly static string CustomizationSaveDataLocation = "customization.json";

    public enum AccessoryType { None, Upper, Legs, Feet, Extra, Hair }
    [System.Serializable]
    public struct Accessory
    { 
        public AccessoryType type;
        public string name;
        public SkinnedMeshRenderer renderer;
        public SkinnedMeshRenderer[] bodyPartsDisabled;
        public bool active;
    }

    [Header("References")]
    public PlayerData pData;
    public Animator CharacterAnimator;
    public Transform RightHand;
    public Transform GrabPoint;
    public SkinnedMeshRenderer[] BodySkinRenderers;
    public SkinnedMeshRenderer[] FacialRenderers;
    public Accessory[] Accesories;
    public MatColPair defaultPair;

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

    [SyncVar(hook = nameof(OnSkinChanged))]
    private NetSkinData syncedSkin;

    bool skinLoaded;

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

            Accesories[i].renderer.material.SetColor("_MainColor", Color.white);
            Accesories[i].renderer.material.SetColor("_SecondColor", Color.lightBlue);
            Accesories[i].renderer.material.SetColor("_ThirdColor", Color.lightBlue);
        }
    }

    private void Start()
    {
        if (isLocalPlayer)
        {
            LoadFromJson(CustomizationSaveDataLocation);
            TrySyncSkin();
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

    public override void OnStartLocalPlayer()
    {
        TrySyncSkin();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        OnSkinChanged(syncedSkin, syncedSkin);
    }

    #region Blink/breath
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
    #endregion

    #region Customization

    public void SetBodyColor(string param, Color color) => BodyMaterial.SetColor(param, ClampColorNoFullChannels(color));

    public void SetFacialColor(string param, Color color) => FacialMaterial.SetColor(param, ClampColorNoFullChannels(color));

    public void SetFacialGlow(float intensity) => FacialMaterial.SetFloat("_GlowIntensity", intensity);

    public float GetFacialGlow() => FacialMaterial.GetFloat("_GlowIntensity");

    public string GetAccessoryName(AccessoryType accType)
    {
        if (accType == AccessoryType.None) return null;

        foreach (var accessory in Accesories)
        {
            if (accessory.type == accType &&
                accessory.renderer.enabled)
            {
                return accessory.name;
            }
        }

        return accType.ToString();
    }

    public void SetAccesoryColor(AccessoryType accType, string param, Color color)
    {
        int accesoryIndex = -1;

        for (int i = 0; i < Accesories.Length; i++)
        {
            if (Accesories[i].type == accType &&
                Accesories[i].renderer.enabled)
            {
                accesoryIndex = i;
                break;
            }
        }

        if (accesoryIndex == -1) return;

        color = ClampColorNoFullChannels(color);

        Accesories[accesoryIndex].renderer.material.SetColor(param, color);

        _accesoryColors[Accesories[accesoryIndex].type][param] = color;
    }

    public void SwitchAccesory(int newAccesoryIndex)
    {
        AccessoryType type = Accesories[newAccesoryIndex].type;

        int oldAccesoryIndex = -1;
        for (int i = 0; i < Accesories.Length; i++)
        {
            if (Accesories[i].type == type && 
                Accesories[i].renderer.enabled)
            {
                oldAccesoryIndex = i;
                break;
            }
        }

        for (int i = 0; i < BodySkinRenderers.Length; i++)
        {
            if (oldAccesoryIndex != -1 &&
                Accesories[oldAccesoryIndex].bodyPartsDisabled.Contains(BodySkinRenderers[i]))
                BodySkinRenderers[i].enabled = true;

            if (Accesories[newAccesoryIndex].bodyPartsDisabled.Contains(BodySkinRenderers[i]))
                BodySkinRenderers[i].enabled = false;
        }

        if (oldAccesoryIndex != -1)
            Accesories[oldAccesoryIndex].renderer.enabled = false;
        Accesories[newAccesoryIndex].renderer.enabled = true;

        Accesories[newAccesoryIndex].renderer.material.SetColor("_MainColor", 
            _accesoryColors[type]["_MainColor"]);

        Accesories[newAccesoryIndex].renderer.material.SetColor("_SecondColor", 
            _accesoryColors[type]["_SecondColor"]);

        Accesories[newAccesoryIndex].renderer.material.SetColor("_ThirdColor",
            _accesoryColors[type]["_ThirdColor"]);

        if (oldAccesoryIndex != -1)
            Accesories[oldAccesoryIndex].active = false;
        Accesories[newAccesoryIndex].active = true;
    }

    public string DisableAccesory(AccessoryType type)
    {
        int accesoryIndex = -1;

        for (int i = 0; i < Accesories.Length; i++)
        {
            if (Accesories[i].type == type &&
                Accesories[i].renderer.enabled)
            {
                accesoryIndex = i;
                break;
            }
        }

        if (accesoryIndex == -1) return "Null";

        for (int i = 0; i < BodySkinRenderers.Length; i++)
        {
            if (Accesories[accesoryIndex].bodyPartsDisabled.Contains(BodySkinRenderers[i]))
                BodySkinRenderers[i].enabled = true;
        }

        Accesories[accesoryIndex].renderer.enabled = false;
        Accesories[accesoryIndex].active = false;

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

    public Color GetColor(AccessoryType accType, ColorType colType, MaterialType matType)
    {
        if (accType != AccessoryType.None)
        {
            return colType switch
            {
                ColorType.Main => _accesoryColors[accType]["_MainColor"],
                ColorType.Secondary => _accesoryColors[accType]["_SecondColor"],
                ColorType.Tertiary => _accesoryColors[accType]["_ThirdColor"],
                _ => Color.white
            };
        }

        Material mat = matType switch
        {
            MaterialType.Body => BodyMaterial,
            MaterialType.Facial => FacialMaterial,
            _ => null
        };

        if (mat == null) return Color.purple;

        return colType switch
        {
            ColorType.Main => mat.GetColor("_MainColor"),
            ColorType.Secondary => mat.GetColor("_SecondColor"),
            ColorType.Tertiary => mat.GetColor("_ThirdColor"),
            ColorType.Mask => mat.GetColor("_MultiplyColor"),
            ColorType.Texture => mat.GetColor("_TextureColor"),
            _ => Color.white
        };
    }
    #endregion

    #region Save/Load
    public CharacterSkinSaveData BuildSaveData()
    {
        CharacterSkinSaveData data = new()
        {
            bodyMain = new SerializableColor(BodyMaterial.GetColor("_MainColor")),
            bodySecond = new SerializableColor(BodyMaterial.GetColor("_SecondColor")),
            bodyThird = new SerializableColor(BodyMaterial.GetColor("_ThirdColor")),

            facialMain = new SerializableColor(FacialMaterial.GetColor("_MainColor")),
            facialSecond = new SerializableColor(FacialMaterial.GetColor("_SecondColor")),
            facialThird = new SerializableColor(FacialMaterial.GetColor("_ThirdColor")),
            pupilColor = new SerializableColor(FacialMaterial.GetColor("_MultiplyColor")),
            facialGlow = FacialMaterial.GetFloat("_GlowIntensity")
        };

        for (int i = 0; i < Accesories.Length; i++)
        {
            var acc = Accesories[i];

            AccessorySaveData accData = new()
            {
                index = i,
                type = acc.type,
                active = acc.renderer.enabled,
                mainColor = new SerializableColor(acc.renderer.material.GetColor("_MainColor")),
                secondColor = new SerializableColor(acc.renderer.material.GetColor("_SecondColor")),
                thirdColor = new SerializableColor(acc.renderer.material.GetColor("_ThirdColor"))
            };

            data.accessories.Add(accData);
        }

        return data;
    }

    public void SaveToJson(string fileName)
    {
        if (!isLocalPlayer) return;

        var data = BuildSaveData();
        string json = JsonUtility.ToJson(data, true);

        string path = Path.Combine(Application.persistentDataPath, fileName);

        File.WriteAllText(path, json);
    }

    public void LoadFromJson(string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        if (!File.Exists(path))
        {
            for (int i = 0; i < Accesories.Length; i++)
            {
                Accesories[i].active = Accesories[i].renderer.enabled;
            }

            skinLoaded = true;
            return;
        }

        string json = File.ReadAllText(path);
        CharacterSkinSaveData data = JsonUtility.FromJson<CharacterSkinSaveData>(json);

        ApplySaveData(data);
        skinLoaded = true;
    }

    public void ApplySaveData(CharacterSkinSaveData data)
    {
        defaultPair.SetColor(data.facialMain.ToColor());

        SetBodyColor("_MainColor", data.bodyMain.ToColor());
        SetBodyColor("_SecondColor", data.bodySecond.ToColor());
        SetBodyColor("_ThirdColor", data.bodyThird.ToColor());

        SetFacialColor("_MainColor", data.facialMain.ToColor());
        SetFacialColor("_SecondColor", data.facialSecond.ToColor());
        SetFacialColor("_ThirdColor", data.facialThird.ToColor());
        SetFacialColor("_MultiplyColor", data.pupilColor.ToColor());
        SetFacialGlow(data.facialGlow);

        foreach (var accData in data.accessories)
        {
            Accesories[accData.index].renderer.material.SetColor("_MainColor", accData.mainColor.ToColor());
            Accesories[accData.index].renderer.material.SetColor("_SecondColor", accData.secondColor.ToColor());
            Accesories[accData.index].renderer.material.SetColor("_ThirdColor", accData.thirdColor.ToColor());

            _accesoryColors[Accesories[accData.index].type]["_MainColor"] = accData.mainColor.ToColor();
            _accesoryColors[Accesories[accData.index].type]["_SecondColor"] = accData.secondColor.ToColor();
            _accesoryColors[Accesories[accData.index].type]["_ThirdColor"] = accData.thirdColor.ToColor();

            if (accData.active)
            {
                Accesories[accData.index].renderer.enabled = true;
                Accesories[accData.index].active = true;

                foreach (var body in Accesories[accData.index].bodyPartsDisabled)
                    body.enabled = false;
            }
            else
            {
                Accesories[accData.index].renderer.enabled = false;
                Accesories[accData.index].active = false;
            }
        }
    }
    #endregion

    #region Sync
        #region Data
    [System.Serializable]
    public struct NetColor
    {
        public byte r, g, b, a;

        public NetColor(Color c)
        {
            r = (byte)(c.r * 255);
            g = (byte)(c.g * 255);
            b = (byte)(c.b * 255);
            a = (byte)(c.a * 255);
        }

        public Color ToColor()
        {
            return new Color32(r, g, b, a);
        }
    }
    [System.Serializable]
    public struct NetAccessoryData
    {
        public byte index;
        public byte type;
        public bool active;

        public NetColor main;
        public NetColor second;
        public NetColor third;
    }
    [System.Serializable]
    public struct NetSkinData
    {
        public bool valid;

        public NetColor bodyMain;
        public NetColor bodySecond;
        public NetColor bodyThird;

        public NetColor facialMain;
        public NetColor facialSecond;
        public NetColor facialThird;
        public NetColor pupilColor;

        public float facialGlow;

        public NetAccessoryData[] accessories;
    }
    #endregion

    void TrySyncSkin()
    {
        if (!isLocalPlayer) return;
        if (!skinLoaded) return;

        CmdSyncSkin(BuildNetSkinData());
    }


    public void SyncSkinData()
    {
        if (isLocalPlayer)
            CmdSyncSkin(BuildNetSkinData());
    }

    [Command]
    void CmdSyncSkin(NetSkinData data)
    {
        syncedSkin = data;
    }

    void OnSkinChanged(NetSkinData _, NetSkinData newData)
    {
        ApplyNetSkinData(newData);
    }

    public NetSkinData BuildNetSkinData()
    {
        var save = BuildSaveData();

        NetSkinData net = new()
        {
            valid = true,
            bodyMain = new NetColor(save.bodyMain.ToColor()),
            bodySecond = new NetColor(save.bodySecond.ToColor()),
            bodyThird = new NetColor(save.bodyThird.ToColor()),

            facialMain = new NetColor(save.facialMain.ToColor()),
            facialSecond = new NetColor(save.facialSecond.ToColor()),
            facialThird = new NetColor(save.facialThird.ToColor()),
            pupilColor = new NetColor(save.pupilColor.ToColor()),

            facialGlow = save.facialGlow,
            accessories = new NetAccessoryData[save.accessories.Count]
        };

        for (int i = 0; i < save.accessories.Count; i++)
        {
            var acc = save.accessories[i];

            net.accessories[i] = new NetAccessoryData
            {
                index = (byte)acc.index,
                type = (byte)acc.type,
                active = acc.active,
                main = new NetColor(acc.mainColor.ToColor()),
                second = new NetColor(acc.secondColor.ToColor()),
                third = new NetColor(acc.thirdColor.ToColor())
            };
        }

        return net;
    }

    public void ApplyNetSkinData(NetSkinData net)
    {
        if (!net.valid) return;
        if (net.accessories == null || net.accessories.Length == 0)
            return;

        SetBodyColor("_MainColor", net.bodyMain.ToColor());
        SetBodyColor("_SecondColor", net.bodySecond.ToColor());
        SetBodyColor("_ThirdColor", net.bodyThird.ToColor());

        SetFacialColor("_MainColor", net.facialMain.ToColor());
        SetFacialColor("_SecondColor", net.facialSecond.ToColor());
        SetFacialColor("_ThirdColor", net.facialThird.ToColor());
        SetFacialColor("_MultiplyColor", net.pupilColor.ToColor());
        SetFacialGlow(net.facialGlow);

        foreach (var acc in net.accessories) // line 620 here
        {
            var renderer = Accesories[acc.index].renderer;

            renderer.material.SetColor("_MainColor", acc.main.ToColor());
            renderer.material.SetColor("_SecondColor", acc.second.ToColor());
            renderer.material.SetColor("_ThirdColor", acc.third.ToColor());

            renderer.enabled = acc.active;
            Accesories[acc.index].active = acc.active;
        }
    }
    #endregion
}
