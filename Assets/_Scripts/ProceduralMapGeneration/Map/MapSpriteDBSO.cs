using System;
using UnityEngine;

[CreateAssetMenu(menuName = "LethalLive/Map Sprite Database")]
public class MapSpriteDBSO : ScriptableObject
{
    public enum PartType
    {
        Room,
        Corner,
        Hallway,
        Edge,
        Custom
    }

    public enum LayerDir
    {
        None,
        Up,
        Down
    }

    [Serializable]
    public struct MapPart
    {
        public Sprite partSprite;
        public PartType partType;

        [Tooltip("Directions that have a PORT (opening to outside / another room).")]
        public Direction[] openSides;

        [Tooltip("Directions that connect INTO the room interior (shared footprint neighbours). " +
                 "Combined with openSides this makes every tile orientation unique. " +
                 "E.g. a top-left corner with a West port: openSides=[West], connectedSides=[East, South].")]
        public Direction[] connectedSides;

        [Tooltip("None = flat floor. Up/Down = staircase tile going to that Y layer.")]
        public LayerDir layerDir;
    }

    [SerializeField] public MapPart[] mapParts = Array.Empty<MapPart>();

    // ---------------------------------------------------------------
    //  Runtime lookup — matches on openSides + connectedSides + layerDir.
    //  Score = number of exact side matches; higher = better fit.
    // ---------------------------------------------------------------
    public bool TryGetSprite(Direction[] openSides, Direction[] connectedSides,
                             LayerDir layerDir, out Sprite sprite)
    {
        sprite = null;
        int bestScore = -1;

        foreach (var part in mapParts)
        {
            if (part.partSprite == null) continue;
            if (part.layerDir != layerDir) continue;

            int os = ScoreMatch(part.openSides, openSides); if (os < 0) continue;
            int cs = ScoreMatch(part.connectedSides, connectedSides); if (cs < 0) continue;

            int total = os + cs;
            if (total > bestScore) { bestScore = total; sprite = part.partSprite; }
        }

        return sprite != null;
    }

    // All sides declared on the part must appear in the query.
    // Returns the match count, or -1 if any declared side is missing.
    private static int ScoreMatch(Direction[] partSides, Direction[] querySides)
    {
        if (partSides == null || partSides.Length == 0) return 0;
        if (querySides == null) return -1;

        int score = 0;
        foreach (var s in partSides)
        {
            bool found = false;
            foreach (var q in querySides) if (q == s) { found = true; break; }
            if (!found) return -1;
            score++;
        }
        return score;
    }

#if UNITY_EDITOR
    public void AddFromSprites(Sprite[] sprites)
    {
        int before = mapParts.Length;
        Array.Resize(ref mapParts, before + sprites.Length);
        for (int i = 0; i < sprites.Length; i++)
        {
            mapParts[before + i] = new MapPart
            {
                partSprite = sprites[i],
                partType = PartType.Room,
                openSides = Array.Empty<Direction>(),
                connectedSides = Array.Empty<Direction>(),
                layerDir = LayerDir.None
            };
        }
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
