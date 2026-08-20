using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    [SerializeField] private TMP_Text _healthText;
    [SerializeField] private GameObject _lethalPreviewIcon;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 2f, 0f);

    private EnemyHealth _enemyHealth;
    private RectTransform _rectTransform;
    private RectTransform _parent;
    private Camera _camera;
    private bool _hasPreview;
    private int _previewDamage;

    public Vector3 WorldOffset => _worldOffset;

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Initialize(EnemyHealth enemyHealth, RectTransform parent, Camera camera)
    {
        Unsubscribe();

        _enemyHealth = enemyHealth;
        _parent = parent;
        _camera = camera;
        _rectTransform = transform as RectTransform;

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        Subscribe();
        RefreshPosition();
    }

    private void LateUpdate()
    {
        RefreshPosition();
    }

    private void UpdateView(int health, int maxHealth)
    {
        int clampedHealth = Mathf.Clamp(health, 0, maxHealth);
        int previewHealth = _hasPreview ? Mathf.Max(0, clampedHealth - _previewDamage) : clampedHealth;
        bool isLethalPreview = _hasPreview && previewHealth <= 0;

        if (_slider != null)
        {
            _slider.maxValue = maxHealth;
            _slider.value = previewHealth;
        }

        if (_healthText != null)
        {
            _healthText.text = $"{CompactNumberFormatter.Format(clampedHealth)}/{CompactNumberFormatter.Format(maxHealth)}";
            _healthText.gameObject.SetActive(isLethalPreview == false);
        }

        if (_lethalPreviewIcon != null)
            _lethalPreviewIcon.SetActive(isLethalPreview);
    }

    public void SetDamagePreview(int damage)
    {
        _hasPreview = damage > 0;
        _previewDamage = Mathf.Max(0, damage);

        if (_enemyHealth != null)
            UpdateView(_enemyHealth.Health, _enemyHealth.MaxHealth);
    }

    public void ClearDamagePreview()
    {
        _hasPreview = false;
        _previewDamage = 0;

        if (_enemyHealth != null)
            UpdateView(_enemyHealth.Health, _enemyHealth.MaxHealth);
    }

    private void RefreshPosition()
    {
        if (_enemyHealth == null || _camera == null || _parent == null || _rectTransform == null)
            return;

        Vector3 viewportPosition = _camera.WorldToViewportPoint(_enemyHealth.transform.position + _worldOffset);
        bool isVisible = viewportPosition.z > 0f;
        SetVisible(isVisible);

        if (isVisible == false)
            return;

        Rect parentRect = _parent.rect;
        Vector2 canvasPosition = new Vector2(
            (viewportPosition.x - 0.5f) * parentRect.width,
            (viewportPosition.y - 0.5f) * parentRect.height);

        _rectTransform.anchoredPosition = canvasPosition;
    }

    private void Subscribe()
    {
        if (_enemyHealth == null)
            return;

        _enemyHealth.HealthChanged += UpdateView;
        UpdateView(_enemyHealth.Health, _enemyHealth.MaxHealth);
    }

    private void Unsubscribe()
    {
        if (_enemyHealth != null)
            _enemyHealth.HealthChanged -= UpdateView;
    }

    private void SetVisible(bool isVisible)
    {
        if (_canvasGroup == null)
            return;

        _canvasGroup.alpha = isVisible ? 1f : 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }
}
