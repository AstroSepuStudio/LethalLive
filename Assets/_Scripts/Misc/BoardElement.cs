using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class BoardElement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] int boardIndex;
    [SerializeField] private NetworkFBIBoard board;
    [SerializeField] private SpriteRenderer spriteRenderer;

    public UnityEvent OnSetNewSprite;
    public SpriteRenderer SpriteRenderer => spriteRenderer;

    [SyncVar(hook = nameof(OnIndexChanged))] int spriteIndex = -1;

    [Server] public void SetIndex(int index) => spriteIndex = index;

    private void OnIndexChanged(int oldValue, int newValue)
    {
        SetSprite(newValue);
    }

    public void SetSprite(int index)
    {
        if (index == -1)
        {
            ClearSprite();
            return;
        }

        if (spriteRenderer == null)
        {
            Debug.LogError($"[BoardElement] SpriteRenderer is null on {name}. Assign it in the Inspector.");
            return;
        }

        spriteRenderer.enabled = true;
        spriteRenderer.sprite = board.GetSprite(index)?.Sprite;
        spriteRenderer.sortingOrder = index;
        OnSetNewSprite?.Invoke();
    }

    public void ClearSprite()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = null;
            spriteRenderer.enabled = false;
        }
    }
}
