using UnityEngine;

public class RandomColorFlicker : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private float transitionSpeed = 1f;

    private Material _material;
    private Color _currentColor;
    private Color _targetColor;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        _material = new Material(targetRenderer.sharedMaterial);
        targetRenderer.material = _material;

        _currentColor = _material.color;
        _targetColor = RandomColor(_currentColor.a);
    }

    private void Update()
    {
        _currentColor = Color.Lerp(_currentColor, _targetColor, Time.deltaTime * transitionSpeed);
        _material.color = _currentColor;

        if (ColorDistance(_currentColor, _targetColor) < 0.01f)
            _targetColor = RandomColor(_currentColor.a);
    }

    private void OnDestroy()
    {
        if (_material != null)
            Destroy(_material);
    }

    private Color RandomColor(float alpha) =>
        new(Random.value, Random.value, Random.value, alpha);

    private float ColorDistance(Color a, Color b) =>
        Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
}
