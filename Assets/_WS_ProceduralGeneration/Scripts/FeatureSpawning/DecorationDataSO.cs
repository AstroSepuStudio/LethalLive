using UnityEngine;
using static LL_Tier;
using static ThemeDataSO;

[CreateAssetMenu(menuName = "LethalLive/Decoration")]
public class DecorationDataSO : ScriptableObject
{
    public Tier Tier;
    public SpawnableSize Size;
    public GameObject Prefab;
}
