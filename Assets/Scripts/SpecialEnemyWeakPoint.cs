using UnityEngine;

public class SpecialEnemyWeakPoint : MonoBehaviour, IDamageable
{
    [SerializeField] private SpecialWeakPointEnemy _owner;
    [SerializeField] private GameObject _activeObject;
    [SerializeField] private float _detachForce = 5f;
    [SerializeField] private float _detachTorque = 8f;
    [SerializeField] private float _upwardsModifier = 0.35f;
    [SerializeField] private Color _blinkColor = Color.red;
    [SerializeField] private float _blinkSpeed = 6f;

    private Renderer[] _renderers;
    private Collider[] _colliders;
    private Material[][] _rendererMaterials;
    private Color[][] _originalColors;
    private bool _isDestroyed;
    private bool _isTargetActive;

    public bool IsDestroyed => _isDestroyed;

    public void Initialize(SpecialWeakPointEnemy owner)
    {
        _owner = owner;
        _isDestroyed = false;
        CacheComponents();
        SetVisualActive(true);
        SetTargetActive(false);
    }

    private void Update()
    {
        if (_isTargetActive == false || _isDestroyed)
            return;

        ApplyBlinkColor();
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, DamageType.Normal, transform.position, Vector3.forward);
    }

    public void TakeDamage(int damage, DamageType damageType)
    {
        TakeDamage(damage, damageType, transform.position, Vector3.forward);
    }

    public void TakeDamage(int damage, DamageType damageType, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (_isDestroyed || DamageSourceContext.IsExplosionDamage)
            return;

        if (_owner == null)
            _owner = GetComponentInParent<SpecialWeakPointEnemy>();

        if (_owner == null)
            return;

        _owner.RegisterWeakPointHit(this, damage, damageType, hitPoint, hitDirection);
    }

    public void SetTargetActive(bool isActive)
    {
        if (_isDestroyed)
            return;

        _isTargetActive = isActive;
        SetCollidersActive(isActive);

        if (isActive == false)
            ClearBlinkColor();
    }

    public void DestroyPoint(Vector3 hitPoint, Vector3 hitDirection)
    {
        _isDestroyed = true;
        _isTargetActive = false;

        GameObject detachedObject = _activeObject != null ? _activeObject : gameObject;
        ClearBlinkColor();
        DisableSourceColliders(detachedObject);
        DetachObject(detachedObject, hitPoint, hitDirection);
    }

    private void CacheComponents()
    {
        GameObject target = _activeObject != null ? _activeObject : gameObject;
        _renderers = target.GetComponentsInChildren<Renderer>(true);
        _colliders = GetComponents<Collider>();
        CacheOriginalColors();
    }

    private void SetVisualActive(bool isActive)
    {
        if (_activeObject != null)
            _activeObject.SetActive(isActive);
        else
            gameObject.SetActive(isActive);
    }

    private void CacheOriginalColors()
    {
        if (_renderers == null)
            return;

        _rendererMaterials = new Material[_renderers.Length][];
        _originalColors = new Color[_renderers.Length][];

        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer targetRenderer = _renderers[i];

            if (targetRenderer == null)
                continue;

            Material[] materials = targetRenderer.materials;
            _rendererMaterials[i] = materials;
            _originalColors[i] = new Color[materials.Length];

            for (int j = 0; j < materials.Length; j++)
                _originalColors[i][j] = GetMaterialColor(materials[j]);
        }
    }

    private void SetCollidersActive(bool isActive)
    {
        if (_colliders == null)
            return;

        for (int i = 0; i < _colliders.Length; i++)
        {
            if (_colliders[i] != null)
                _colliders[i].enabled = isActive;
        }
    }

    private void ApplyBlinkColor()
    {
        if (_rendererMaterials == null || _originalColors == null)
            return;

        float t = Mathf.PingPong(Time.time * Mathf.Max(0.01f, _blinkSpeed), 1f);

        for (int i = 0; i < _rendererMaterials.Length; i++)
        {
            Material[] materials = _rendererMaterials[i];
            Color[] originalColors = _originalColors[i];

            if (materials == null || originalColors == null)
                continue;

            for (int j = 0; j < materials.Length; j++)
            {
                if (materials[j] == null)
                    continue;

                Color color = Color.Lerp(originalColors[j], _blinkColor, t);
                SetMaterialColor(materials[j], color);
            }
        }
    }

    private void ClearBlinkColor()
    {
        if (_renderers == null)
            return;

        if (_rendererMaterials == null || _originalColors == null)
            return;

        for (int i = 0; i < _rendererMaterials.Length; i++)
        {
            Material[] materials = _rendererMaterials[i];
            Color[] originalColors = _originalColors[i];

            if (materials == null || originalColors == null)
                continue;

            for (int j = 0; j < materials.Length; j++)
            {
                if (materials[j] != null)
                    SetMaterialColor(materials[j], originalColors[j]);
            }
        }
    }

    private Color GetMaterialColor(Material material)
    {
        if (material == null)
            return Color.white;

        if (material.HasProperty("_BaseColor"))
            return material.GetColor("_BaseColor");

        if (material.HasProperty("_Color"))
            return material.GetColor("_Color");

        return Color.white;
    }

    private void SetMaterialColor(Material material, Color color)
    {
        if (material == null)
            return;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }

    private void DisableSourceColliders(GameObject detachedObject)
    {
        if (detachedObject == gameObject)
            return;

        Collider[] colliders = GetComponents<Collider>();

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }
    }

    private void DetachObject(GameObject detachedObject, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (detachedObject == null)
            return;

        detachedObject.SetActive(true);
        detachedObject.transform.SetParent(null, true);

        Collider detachedCollider = detachedObject.GetComponent<Collider>();

        if (detachedCollider == null)
            detachedCollider = detachedObject.AddComponent<BoxCollider>();

        detachedCollider.enabled = true;

        Rigidbody detachedRigidbody = detachedObject.GetComponent<Rigidbody>();

        if (detachedRigidbody == null)
            detachedRigidbody = detachedObject.AddComponent<Rigidbody>();

        detachedRigidbody.isKinematic = false;
        detachedRigidbody.detectCollisions = true;

        Vector3 forceDirection = hitDirection.sqrMagnitude > 0.0001f ? hitDirection.normalized : detachedObject.transform.forward;
        Vector3 force = (forceDirection + Vector3.up * Mathf.Max(0f, _upwardsModifier)).normalized * Mathf.Max(0f, _detachForce);
        detachedRigidbody.AddForceAtPosition(force, hitPoint, ForceMode.Impulse);
        detachedRigidbody.AddTorque(Random.insideUnitSphere * Mathf.Max(0f, _detachTorque), ForceMode.Impulse);
    }
}
