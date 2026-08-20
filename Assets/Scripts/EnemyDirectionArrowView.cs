using UnityEngine;

public class EnemyDirectionArrowView : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private float _edgePadding = 80f;

    private EnemyHealth _enemyHealth;
    private RectTransform _rectTransform;
    private RectTransform _parent;
    private Camera _camera;

    public void Initialize(EnemyHealth enemyHealth, RectTransform parent, Camera camera)
    {
        _enemyHealth = enemyHealth;
        _parent = parent;
        _camera = camera;
        _rectTransform = transform as RectTransform;

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        SetVisible(false);
        Refresh();
    }

    private void LateUpdate()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (_enemyHealth == null || _enemyHealth.IsDead || _camera == null || _parent == null || _rectTransform == null)
        {
            SetVisible(false);
            return;
        }

        Vector3 worldPosition = _enemyHealth.transform.position + _worldOffset;
        Vector3 viewportPosition = _camera.WorldToViewportPoint(worldPosition);
        bool isInView = viewportPosition.z > 0f
            && viewportPosition.x >= 0f
            && viewportPosition.x <= 1f
            && viewportPosition.y >= 0f
            && viewportPosition.y <= 1f;

        if (isInView)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        UpdatePositionAndRotation(viewportPosition);
    }

    private void UpdatePositionAndRotation(Vector3 viewportPosition)
    {
        Rect parentRect = _parent.rect;
        Vector2 direction = new Vector2(viewportPosition.x - 0.5f, viewportPosition.y - 0.5f);

        if (viewportPosition.z < 0f)
            direction = -direction;

        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector2.up;

        direction.Normalize();

        Vector2 halfSize = new Vector2(parentRect.width * 0.5f, parentRect.height * 0.5f);
        Vector2 edge = direction * GetEdgeDistance(direction, halfSize);
        float padding = Mathf.Max(0f, _edgePadding);
        edge.x = Mathf.Clamp(edge.x, -halfSize.x + padding, halfSize.x - padding);
        edge.y = Mathf.Clamp(edge.y, -halfSize.y + padding, halfSize.y - padding);

        _rectTransform.anchoredPosition = edge;
        _rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
    }

    private float GetEdgeDistance(Vector2 direction, Vector2 halfSize)
    {
        float distanceX = Mathf.Abs(direction.x) > 0.0001f ? halfSize.x / Mathf.Abs(direction.x) : float.MaxValue;
        float distanceY = Mathf.Abs(direction.y) > 0.0001f ? halfSize.y / Mathf.Abs(direction.y) : float.MaxValue;
        return Mathf.Min(distanceX, distanceY);
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
