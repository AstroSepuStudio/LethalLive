using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class NetworkFBIBoard : NetworkBehaviour
{
    public enum SpriteType { Default, Meme, Extra}

    [System.Serializable]
    public struct BoardSprite
    {
        public int Index;
        public Sprite Sprite;
        public SpriteType Type;
        public int Quantity;
        public float DefaultScale;
        public bool Important;
    }

    [Header("References")]
    [SerializeField] private BoardElement[] boardElementPool;
    [SerializeField] private Transform spriteParent;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private BoardSprite[] availableSprites;

    [Header("Spawn")]
    [SerializeField] private Vector3 spawnAreaCenter = Vector3.zero;
    [SerializeField] private Vector2 spawnAreaSize = new(8f, 5f);

    [SerializeField, Min(1)] private int minElements = 3;

    [Header("Spread")]
    [SerializeField, Min(1)] private int gridColumns = 4;
    [SerializeField, Min(1)] private int gridRows = 3;
    [SerializeField, Range(0f, 1f)] private float cellJitter = 0.75f;

    Coroutine drlr;
    readonly WaitForSeconds DebounceDelay = new(0.05f);

    public BoardSprite? GetSprite(int index)
    {
        foreach (var sprite in availableSprites)
        {
            if (sprite.Index == index) return sprite;
        }

        return null;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        GenerateBoard();
    }

    [Server]
    public void GenerateBoard()
    {
        int count = Random.Range(minElements, boardElementPool.Length);
        List<Vector3> cells = GetShuffledCellCentres();

        List<int> spritePool = BuildImportantPool();
        List<int> regularPool = BuildRegularPool();

        foreach (int idx in regularPool)
        {
            if (spritePool.Count >= Mathf.Max(count, spritePool.Count)) break;
            spritePool.Add(idx);
        }

        int cellIndex = 0;

        for (int i = 0; i < boardElementPool.Length; i++)
        {
            if (i >= spritePool.Count)
            {
                boardElementPool[i].SetIndex(-1);
                continue;
            }

            int idx = spritePool[i];

            Vector3 cellCentre = cells[cellIndex % cells.Count];
            cellIndex++;

            Vector3 pos = ApplyJitter(cellCentre);
            float rot = Random.Range(-25f, 25f);
            float scale = Random.Range(0.9f, 1.1f) * GetSprite(idx).Value.DefaultScale;

            boardElementPool[i].transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, 0f, rot));
            boardElementPool[i].transform.localScale = new Vector3(scale, scale, scale);
            boardElementPool[i].SetIndex(idx);
        }
    }

    public void RefreshLineRenderer()
    {
        if (drlr != null) StopCoroutine(drlr);

        drlr = StartCoroutine(DelayedRefresh());
    }

    private IEnumerator DelayedRefresh()
    {
        yield return DebounceDelay;

        if (lineRenderer == null) yield break;

        List<int> valid = new();
        for (int i = 0; i < boardElementPool.Length; i++)
            if (boardElementPool[i].SpriteRenderer.enabled)
                valid.Add(i);

        lineRenderer.positionCount = valid.Count;
        for (int i = 0; i < valid.Count; i++)
            lineRenderer.SetPosition(i, boardElementPool[valid[i]].transform.position);

        drlr = null;
    }

    private List<int> BuildImportantPool()
    {
        Shuffle(availableSprites);

        List<int> candidates = new();
        for (int i = 0; i < availableSprites.Length; i++)
        {
            if (!availableSprites[i].Important) continue;
            candidates.Add(availableSprites[i].Index);
        }

        int qy = Random.Range(2, candidates.Count);

        if (qy < candidates.Count)
            candidates.RemoveRange(qy, candidates.Count - qy);

        return candidates;
    }

    private List<int> BuildRegularPool()
    {
        Shuffle(availableSprites);
        List<int> pool = new();
        int memeSlots = 2;

        for (int i = 0; i < availableSprites.Length; i++)
        {
            if (availableSprites[i].Important) continue;

            int qty = availableSprites[i].Quantity;

            if (availableSprites[i].Type == SpriteType.Meme)
            {
                qty = Mathf.Min(qty, memeSlots);
                memeSlots -= qty;
            }

            for (int q = 0; q < qty; q++)
                pool.Add(availableSprites[i].Index);
        }

        return pool;
    }

    void Shuffle<T>(T[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    private List<Vector3> GetShuffledCellCentres()
    {
        float cellW = spawnAreaSize.x / gridColumns;
        float cellH = spawnAreaSize.y / gridRows;

        Vector3 origin = transform.position + spawnAreaCenter
                         - new Vector3(spawnAreaSize.x * 0.5f, spawnAreaSize.y * 0.5f, 0f);

        List<Vector3> centres = new(gridColumns * gridRows);

        for (int row = 0; row < gridRows; row++)
            for (int col = 0; col < gridColumns; col++)
                centres.Add(origin + new Vector3(
                    (col + 0.5f) * cellW,
                    (row + 0.5f) * cellH,
                    spawnAreaCenter.z));

        for (int i = centres.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (centres[i], centres[j]) = (centres[j], centres[i]);
        }

        return centres;
    }

    private Vector3 ApplyJitter(Vector3 cellCentre)
    {
        float halfW = spawnAreaSize.x / gridColumns * 0.5f * cellJitter;
        float halfH = spawnAreaSize.y / gridRows * 0.5f * cellJitter;

        return cellCentre + new Vector3(
            Random.Range(-halfW, halfW),
            Random.Range(-halfH, halfH),
            0f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        Gizmos.DrawCube(spawnAreaCenter + transform.position, new Vector3(spawnAreaSize.x, spawnAreaSize.y, 0.01f));

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 1f);
        Gizmos.DrawWireCube(spawnAreaCenter + transform.position, new Vector3(spawnAreaSize.x, spawnAreaSize.y, 0.01f));

        Gizmos.color = Color.yellow;
        float crossSize = 0.15f;
        Gizmos.DrawLine(
            spawnAreaCenter + transform.position + Vector3.left * crossSize,
            spawnAreaCenter + transform.position + Vector3.right * crossSize);
        Gizmos.DrawLine(
            spawnAreaCenter + transform.position + Vector3.down * crossSize,
            spawnAreaCenter + transform.position + Vector3.up * crossSize);
    }
#endif
}
