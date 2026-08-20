using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyKillBonusView : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Vector3 _previewWorldOffset = new Vector3(0f, 2.35f, 0f);
    [SerializeField] private Vector3 _gameplayWorldOffset = new Vector3(0f, 2.35f, 0f);
    [SerializeField] private Vector2 _previewScreenOffset = new Vector2(0f, 60f);
    [SerializeField] private Vector2 _gameplayScreenOffset = new Vector2(0f, 60f);

    private EnemyHealth _enemyHealth;
    private DestructibleObject _destructibleObject;
    private RectTransform _rectTransform;
    private RectTransform _parent;
    private Camera _camera;
    private Vector3 _anchorWorldOffset;
    private Vector2 _anchorScreenOffset;
    private bool _hasAnchorWorldOffset;
    private bool _hasAnchorScreenOffset;

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
        Initialize(enemyHealth, parent, camera, Vector3.zero, false);
    }

    public void Initialize(EnemyHealth enemyHealth, RectTransform parent, Camera camera, Vector3 anchorWorldOffset)
    {
        Initialize(enemyHealth, parent, camera, anchorWorldOffset, true);
    }

    public void Initialize(DestructibleObject destructibleObject, RectTransform parent, Camera camera)
    {
        Initialize(destructibleObject, parent, camera, Vector3.zero, Vector2.zero, false);
    }

    public void Initialize(DestructibleObject destructibleObject, RectTransform parent, Camera camera, Vector3 anchorWorldOffset, Vector2 anchorScreenOffset)
    {
        Initialize(destructibleObject, parent, camera, anchorWorldOffset, anchorScreenOffset, true);
    }

    private void Initialize(DestructibleObject destructibleObject, RectTransform parent, Camera camera, Vector3 anchorWorldOffset, Vector2 anchorScreenOffset, bool hasAnchor)
    {
        Unsubscribe();

        _enemyHealth = null;
        _destructibleObject = destructibleObject;
        _parent = parent;
        _camera = camera;
        _rectTransform = transform as RectTransform;
        _anchorWorldOffset = anchorWorldOffset;
        _anchorScreenOffset = anchorScreenOffset;
        _hasAnchorWorldOffset = hasAnchor;
        _hasAnchorScreenOffset = hasAnchor;

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        UpdateView();
        RefreshPosition();
    }

    private void Initialize(EnemyHealth enemyHealth, RectTransform parent, Camera camera, Vector3 anchorWorldOffset, bool hasAnchorWorldOffset)
    {
        Unsubscribe();

        _enemyHealth = enemyHealth;
        _destructibleObject = null;
        _parent = parent;
        _camera = camera;
        _rectTransform = transform as RectTransform;
        _anchorWorldOffset = anchorWorldOffset;
        _anchorScreenOffset = Vector2.zero;
        _hasAnchorWorldOffset = hasAnchorWorldOffset;
        _hasAnchorScreenOffset = false;

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        Subscribe();
        UpdateView();
        RefreshPosition();
    }

    private void LateUpdate()
    {
        RefreshPosition();
    }

    private void UpdateView()
    {
        if (_text == null)
            return;

        _text.text = GetBonusText();

        if (_canvasGroup != null && string.IsNullOrEmpty(_text.text))
            _canvasGroup.alpha = 0f;
    }

    private void Subscribe()
    {
        if (_enemyHealth != null)
            _enemyHealth.BonusChanged += UpdateView;
    }

    private void Unsubscribe()
    {
        if (_enemyHealth != null)
            _enemyHealth.BonusChanged -= UpdateView;
    }

    private void RefreshPosition()
    {
        Transform target = GetTargetTransform();

        if (target == null || _camera == null || _parent == null || _rectTransform == null)
            return;

        Vector3 viewportPosition = _camera.WorldToViewportPoint(target.position + GetWorldOffset());
        bool isVisible = viewportPosition.z > 0f;
        if (string.IsNullOrEmpty(GetBonusText()))
            isVisible = false;

        SetVisible(isVisible);

        if (isVisible == false)
            return;

        Rect parentRect = _parent.rect;
        Vector2 canvasPosition = new Vector2(
            (viewportPosition.x - 0.5f) * parentRect.width,
            (viewportPosition.y - 0.5f) * parentRect.height);

        _rectTransform.anchoredPosition = canvasPosition + GetScreenOffset();
    }

    private void SetVisible(bool isVisible)
    {
        if (_canvasGroup == null)
            return;

        _canvasGroup.alpha = isVisible ? 1f : 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    private Vector3 GetWorldOffset()
    {
        if (_hasAnchorWorldOffset)
            return _anchorWorldOffset;

        GameplayLevelController levelController = Global.GameplayLevelController;

        if (levelController != null && levelController.IsGameplayStarted)
            return _gameplayWorldOffset;

        return _previewWorldOffset;
    }

    private Transform GetTargetTransform()
    {
        if (_enemyHealth != null)
            return _enemyHealth.transform;

        return _destructibleObject != null ? _destructibleObject.transform : null;
    }

    private string GetBonusText()
    {
        if (_enemyHealth != null)
            return _enemyHealth.KillBonusText;

        return _destructibleObject != null ? _destructibleObject.BonusText : string.Empty;
    }

    private Vector2 GetScreenOffset()
    {
        if (_hasAnchorScreenOffset)
            return _anchorScreenOffset;

        GameplayLevelController levelController = Global.GameplayLevelController;

        if (levelController != null && levelController.IsGameplayStarted)
            return _gameplayScreenOffset;

        return _previewScreenOffset;
    }
}
