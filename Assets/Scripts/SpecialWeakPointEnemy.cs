using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class SpecialWeakPointEnemy : MonoBehaviour
{
    [SerializeField] private EnemyHealth _enemyHealth;
    [SerializeField] private SpecialEnemyWeakPoint[] _weakPoints;
    [SerializeField] private bool _blockBodyDamage = true;
    [SerializeField] private bool _fallDownOnDeath = true;
    [SerializeField] private float _fallDownDistance = 4f;
    [SerializeField] private float _fallDownDuration = 0.6f;
    [SerializeField] private float _idleBobAmplitude = 0.15f;
    [SerializeField] private float _idleBobSpeed = 1.5f;
    [SerializeField] private float _idleCircleRadius = 0.25f;
    [SerializeField] private float _idleCircleSpeed = 0.75f;
    [SerializeField] private float _idleTiltAngle = 6f;
    [SerializeField] private float _hitShakeDuration = 0.16f;
    [SerializeField] private float _hitRotationAngle = 10f;
    [SerializeField] private float _hitDropDistance = 0.25f;
    [SerializeField] private float _hitDropDuration = 0.15f;

    private int _aliveWeakPoints;
    private int _currentWeakPointIndex;
    private Tween _fallTween;
    private Sequence _hitReactionSequence;
    private Vector3 _baseLocalPosition;
    private Vector3 _baseLocalEulerAngles;
    private bool _hasBaseLocalPosition;

    public bool CanTakeCurrentDamage()
    {
        return _blockBodyDamage == false || DamageSourceContext.IsWeakPointDamage;
    }

    private void Awake()
    {
        if (_enemyHealth == null)
            _enemyHealth = GetComponent<EnemyHealth>();

        InitializeWeakPoints();
    }

    private void OnEnable()
    {
        CaptureBaseLocalPosition();
        InitializeWeakPoints();
    }

    private void OnDestroy()
    {
        if (_fallTween != null)
            _fallTween.Kill();

        if (_hitReactionSequence != null)
            _hitReactionSequence.Kill();
    }

    private void Update()
    {
        UpdateIdleFlight();
    }

    public void RegisterWeakPointHit(SpecialEnemyWeakPoint weakPoint, int damage, DamageType damageType, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (_enemyHealth == null || _enemyHealth.IsDead || weakPoint == null || weakPoint.IsDestroyed)
            return;

        if (weakPoint != GetCurrentWeakPoint())
            return;

        weakPoint.DestroyPoint(hitPoint, hitDirection);
        _aliveWeakPoints = Mathf.Max(0, _aliveWeakPoints - 1);

        if (_aliveWeakPoints > 0)
        {
            PlayHitReaction();
            ActivateNextWeakPoint();
            return;
        }

        DamageSourceContext.BeginWeakPointDamage();

        try
        {
            _enemyHealth.TakeDamage(_enemyHealth.Health, damageType, hitPoint, hitDirection);
        }
        finally
        {
            DamageSourceContext.EndWeakPointDamage();
        }

        FallDown();
    }

    private void InitializeWeakPoints()
    {
        if (_weakPoints == null || _weakPoints.Length == 0)
            _weakPoints = GetComponentsInChildren<SpecialEnemyWeakPoint>(true);

        _aliveWeakPoints = 0;
        _currentWeakPointIndex = 0;

        for (int i = 0; i < _weakPoints.Length; i++)
        {
            if (_weakPoints[i] == null)
                continue;

            _weakPoints[i].Initialize(this);
            _aliveWeakPoints++;
        }

        RefreshWeakPointStates();
    }

    private SpecialEnemyWeakPoint GetCurrentWeakPoint()
    {
        if (_weakPoints == null)
            return null;

        for (int i = _currentWeakPointIndex; i < _weakPoints.Length; i++)
        {
            if (_weakPoints[i] != null && _weakPoints[i].IsDestroyed == false)
                return _weakPoints[i];
        }

        return null;
    }

    private void ActivateNextWeakPoint()
    {
        for (int i = _currentWeakPointIndex + 1; i < _weakPoints.Length; i++)
        {
            if (_weakPoints[i] == null || _weakPoints[i].IsDestroyed)
                continue;

            _currentWeakPointIndex = i;
            RefreshWeakPointStates();
            return;
        }

        RefreshWeakPointStates();
    }

    private void RefreshWeakPointStates()
    {
        SpecialEnemyWeakPoint currentWeakPoint = GetCurrentWeakPoint();

        if (_weakPoints == null)
            return;

        for (int i = 0; i < _weakPoints.Length; i++)
        {
            if (_weakPoints[i] != null)
                _weakPoints[i].SetTargetActive(_weakPoints[i] == currentWeakPoint);
        }
    }

    private void FallDown()
    {
        if (_fallDownOnDeath == false || _fallDownDistance <= 0f || _fallDownDuration <= 0f)
            return;

        StopHitReaction();

        if (_fallTween != null)
            _fallTween.Kill();

        Vector3 targetPosition = transform.position + Vector3.down * _fallDownDistance;
        _fallTween = transform.DOMove(targetPosition, _fallDownDuration).SetEase(Ease.InQuad);
    }

    private void CaptureBaseLocalPosition()
    {
        if (_hasBaseLocalPosition)
            return;

        _baseLocalPosition = transform.localPosition;
        _baseLocalEulerAngles = transform.localEulerAngles;
        _hasBaseLocalPosition = true;
    }

    private void UpdateIdleFlight()
    {
        if (_hasBaseLocalPosition == false || _enemyHealth == null || _enemyHealth.IsDead || _fallTween != null && _fallTween.active)
            return;

        if (_hitReactionSequence != null && _hitReactionSequence.active)
            return;

        float circleAngle = Time.time * Mathf.Max(0f, _idleCircleSpeed) * Mathf.PI * 2f;
        float circleRadius = Mathf.Max(0f, _idleCircleRadius);
        Vector3 circleOffset = new Vector3(Mathf.Cos(circleAngle), 0f, Mathf.Sin(circleAngle)) * circleRadius;
        Vector3 movementDirection = new Vector3(-Mathf.Sin(circleAngle), 0f, Mathf.Cos(circleAngle));
        Vector3 position = _baseLocalPosition + circleOffset;
        position.y += Mathf.Sin(Time.time * Mathf.Max(0f, _idleBobSpeed)) * Mathf.Max(0f, _idleBobAmplitude);
        transform.localPosition = position;

        float tiltAngle = Mathf.Max(0f, _idleTiltAngle);
        Vector3 rotation = _baseLocalEulerAngles;
        rotation.x += movementDirection.z * tiltAngle;
        rotation.z += -movementDirection.x * tiltAngle;
        transform.localEulerAngles = rotation;
    }

    private void PlayHitReaction()
    {
        if (_hasBaseLocalPosition == false || _hitShakeDuration <= 0f && _hitDropDuration <= 0f)
            return;

        StopHitReaction();

        Vector3 droppedPosition = _baseLocalPosition + Vector3.down * Mathf.Max(0f, _hitDropDistance);
        Vector3 negativeRotation = _baseLocalEulerAngles + Vector3.back * Mathf.Max(0f, _hitRotationAngle);
        Vector3 positiveRotation = _baseLocalEulerAngles + Vector3.forward * Mathf.Max(0f, _hitRotationAngle);
        _hitReactionSequence = DOTween.Sequence();
        _hitReactionSequence.Append(transform.DOLocalMove(droppedPosition, _hitDropDuration).SetEase(Ease.OutQuad));
        _hitReactionSequence.Append(transform.DOLocalRotate(negativeRotation, _hitShakeDuration * 0.5f).SetEase(Ease.OutQuad));
        _hitReactionSequence.Append(transform.DOLocalRotate(positiveRotation, _hitShakeDuration).SetEase(Ease.InOutQuad));
        _hitReactionSequence.Append(transform.DOLocalRotate(_baseLocalEulerAngles, _hitShakeDuration * 0.5f).SetEase(Ease.OutQuad));
        _hitReactionSequence.Append(transform.DOLocalMove(_baseLocalPosition, _hitDropDuration).SetEase(Ease.OutQuad));
        _hitReactionSequence.OnComplete(() => _hitReactionSequence = null);
    }

    private void StopHitReaction()
    {
        if (_hitReactionSequence == null)
            return;

        _hitReactionSequence.Kill();
        _hitReactionSequence = null;

        if (_hasBaseLocalPosition)
            transform.localEulerAngles = _baseLocalEulerAngles;
    }
}
