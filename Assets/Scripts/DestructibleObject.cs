using System;
using UnityEngine;

public class DestructibleObject : MonoBehaviour, IDamageable
{
    private const int MaxExplosionTargets = 32;

    [SerializeField] private int _maxHealth = 1;
    [SerializeField] private bool _dealsDamage = true;
    [SerializeField] private float _damageMultiplier = 2f;
    [SerializeField] private float _damageRadius = 2f;
    [SerializeField] private LayerMask _damageMask = ~0;
    [SerializeField] private GameObject _explosionVfx;

    private readonly Collider[] _explosionHits = new Collider[MaxExplosionTargets];
    private readonly IDamageable[] _damagedObjects = new IDamageable[MaxExplosionTargets];
    private int _health;
    private int _lastDamage;
    private DamageType _lastDamageType;
    private Vector3 _lastHitPoint;
    private bool _isDestroyed;

    public int MaxHealth => _maxHealth;
    public int Health => _health;
    public bool DealsDamage => _dealsDamage;
    public float DamageMultiplier => _damageMultiplier;
    public float DamageRadius => _damageRadius;
    public bool IsDestroyed => _isDestroyed;
    public string BonusText => _dealsDamage ? $"x{GetMultiplierText()}" : string.Empty;

    public event Action<DestructibleObject> Destroyed;
    public event Action<Vector3, int> ExplosionDamageDealt;

    private void Awake()
    {
        _health = _maxHealth;
        _lastDamageType = DamageType.Normal;
        _lastHitPoint = transform.position;
    }

    private void OnEnable()
    {
        if (Global.GameplayLevelController != null)
            Global.GameplayLevelController.RegisterDestructibleObject(this);

        if (Global.HUDManager != null)
            Global.HUDManager.RegisterDestructibleObject(this);

        if (Global.GameplayTutorialManager != null)
            Global.GameplayTutorialManager.RegisterDestructibleObject(this);
    }

    private void OnDisable()
    {
        if (Global.GameplayLevelController != null)
            Global.GameplayLevelController.UnregisterDestructibleObject(this);

        if (Global.HUDManager != null)
            Global.HUDManager.UnregisterDestructibleObject(this);

        if (Global.GameplayTutorialManager != null)
            Global.GameplayTutorialManager.UnregisterDestructibleObject(this);
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
        if (_isDestroyed || damage <= 0)
            return;

        if (Global.GameplayLevelController != null && Global.GameplayLevelController.CanDamageDestructibleObject(this) == false)
            return;

        _lastDamage = damage;
        _lastDamageType = damageType;
        _lastHitPoint = hitPoint;
        _health -= damage;

        if (Global.AudioManager != null)
            Global.AudioManager.PlaySound(AudioSfxType.DestructibleHit);

        if (_health <= 0)
            DestroyObject();
    }

    private void DestroyObject()
    {
        _isDestroyed = true;

        if (_dealsDamage)
        {
            if (Global.AudioManager != null)
                Global.AudioManager.PlaySound(AudioSfxType.Explosion);

            ActivateExplosionVfx();
            DealExplosionDamage();
        }

        Destroyed?.Invoke(this);
        Destroy(gameObject);
    }

    private void DealExplosionDamage()
    {
        int hitsCount = Physics.OverlapSphereNonAlloc(transform.position, _damageRadius, _explosionHits, _damageMask, QueryTriggerInteraction.Ignore);
        int damage = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(1, _lastDamage) * Mathf.Max(0f, _damageMultiplier)));
        int damagedObjectsCount = 0;

        for (int i = 0; i < hitsCount; i++)
        {
            Collider hit = _explosionHits[i];

            if (hit == null)
                continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable == this)
                continue;

            if (damageable == null)
                continue;

            if (HasDamagedObject(damageable, damagedObjectsCount))
                continue;

            _damagedObjects[damagedObjectsCount] = damageable;
            damagedObjectsCount++;

            MonoBehaviour damageableBehaviour = damageable as MonoBehaviour;
            Vector3 targetPosition = damageableBehaviour != null ? damageableBehaviour.transform.position : hit.transform.position;
            Vector3 damagePopupPosition = hit.bounds.center;
            Vector3 hitDirection = targetPosition - transform.position;
            int previousHealth = GetHealth(damageable);
            DamageSourceContext.BeginExplosionDamage();

            try
            {
                damageable.TakeDamage(damage, _lastDamageType, transform.position, hitDirection);
            }
            finally
            {
                DamageSourceContext.EndExplosionDamage();
            }

            int appliedDamage = GetAppliedDamage(damageable, previousHealth, damage);

            if (appliedDamage > 0)
                ExplosionDamageDealt?.Invoke(damagePopupPosition, appliedDamage);
        }

        for (int i = 0; i < damagedObjectsCount; i++)
            _damagedObjects[i] = null;
    }

    private bool HasDamagedObject(IDamageable damageable, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (_damagedObjects[i] == damageable)
                return true;
        }

        return false;
    }

    private int GetHealth(IDamageable damageable)
    {
        if (damageable is EnemyHealth enemyHealth)
            return enemyHealth.Health;

        if (damageable is DestructibleObject destructibleObject)
            return destructibleObject.Health;

        if (damageable is PlayerHealth playerHealth)
            return playerHealth.Health;

        return -1;
    }

    private int GetAppliedDamage(IDamageable damageable, int previousHealth, int fallbackDamage)
    {
        if (previousHealth < 0)
            return Mathf.Max(0, fallbackDamage);

        return Mathf.Max(0, previousHealth - GetHealth(damageable));
    }

    private void ActivateExplosionVfx()
    {
        if (_explosionVfx == null)
            return;

        GameObject vfx = _explosionVfx.scene.IsValid() ? _explosionVfx : Instantiate(_explosionVfx);
        vfx.transform.SetParent(null, true);
        vfx.transform.position = _lastHitPoint;
        vfx.SetActive(true);
    }

    private string GetMultiplierText()
    {
        float multiplier = Mathf.Max(0f, _damageMultiplier);
        string format = Mathf.Approximately(multiplier, Mathf.Round(multiplier)) ? "0" : "0.#";
        return multiplier.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
    }

    private void OnDrawGizmosSelected()
    {
        if (_dealsDamage == false)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _damageRadius);
    }
}
