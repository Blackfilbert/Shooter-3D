using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerTouchInput _touchInput;
    [SerializeField] private PlayerWeapon _weapon;
    [SerializeField] private Transform _cameraPivot;
    [SerializeField] private float _horizontalSpeed = 120f;
    [SerializeField] private float _verticalSpeed = 90f;
    [SerializeField] private float _previewSpeedMultiplier = 3f;
    [SerializeField] private float _minHorizontalAngle = -60f;
    [SerializeField] private float _maxHorizontalAngle = 60f;
    [SerializeField] private float _minVerticalAngle = -35f;
    [SerializeField] private float _maxVerticalAngle = 55f;
    [SerializeField] private float _swayHorizontalAngle = 1.5f;
    [SerializeField] private float _swayRollAngle = 0.4f;
    [SerializeField] private float _swaySpeed = 1.2f;
    [SerializeField] private float _recoilVerticalAngle = 2.5f;
    [SerializeField] private float _recoilHorizontalAngle = 1.2f;
    [SerializeField] private float _recoilReturnSpeed = 12f;

    private float _horizontalAngle;
    private float _verticalAngle;
    private Vector2 _recoilOffset;

    public PlayerTouchInput TouchInput => _touchInput;
    public PlayerWeapon Weapon => _weapon;
    public Transform CameraPivot => _cameraPivot;

    private void Awake()
    {
        if (_cameraPivot == null)
            _cameraPivot = transform;

        Vector3 angles = _cameraPivot.localEulerAngles;
        _horizontalAngle = NormalizeAngle(angles.y);
        _verticalAngle = NormalizeAngle(angles.x);

        Global.RegisterPlayerMovement(this);
    }

    private void OnEnable()
    {
        if (_touchInput != null)
            _touchInput.Released += Shoot;
    }

    private void OnDisable()
    {
        if (_touchInput != null)
            _touchInput.Released -= Shoot;
    }

    private void OnDestroy()
    {
        Global.UnregisterPlayerMovement(this);
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        if (_touchInput != null && _touchInput.IsPressed)
        {
            Vector2 input = _touchInput.Direction;
            float speedMultiplier = IsGameplayStarted() ? 1f : Mathf.Max(0f, _previewSpeedMultiplier);

            _horizontalAngle += input.x * _horizontalSpeed * speedMultiplier * deltaTime;
            _horizontalAngle = Mathf.Clamp(_horizontalAngle, _minHorizontalAngle, _maxHorizontalAngle);

            _verticalAngle -= input.y * _verticalSpeed * speedMultiplier * deltaTime;
            _verticalAngle = Mathf.Clamp(_verticalAngle, _minVerticalAngle, _maxVerticalAngle);
        }

        UpdateRecoil(deltaTime);
        ApplyCameraRotation();
    }

    private void Shoot()
    {
        if (_weapon != null && _weapon.Shoot())
            ApplyRecoil();
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }

    private void ApplyCameraRotation()
    {
        float sway = IsGameplayStarted() ? Mathf.Sin(Time.time * _swaySpeed) : 0f;
        float horizontalAngle = Mathf.Clamp(_horizontalAngle + _recoilOffset.x + sway * _swayHorizontalAngle, _minHorizontalAngle, _maxHorizontalAngle);
        float verticalAngle = Mathf.Clamp(_verticalAngle + _recoilOffset.y, _minVerticalAngle, _maxVerticalAngle);
        float rollAngle = sway * _swayRollAngle;

        _cameraPivot.localRotation = Quaternion.Euler(verticalAngle, horizontalAngle, rollAngle);
    }

    private void ApplyRecoil()
    {
        float horizontalDirection = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        _recoilOffset.x += _recoilHorizontalAngle * horizontalDirection;
        _recoilOffset.y -= _recoilVerticalAngle;
    }

    private void UpdateRecoil(float deltaTime)
    {
        if (IsGameplayStarted() == false)
        {
            _recoilOffset = Vector2.zero;
            return;
        }

        _recoilOffset = Vector2.Lerp(_recoilOffset, Vector2.zero, 1f - Mathf.Exp(-_recoilReturnSpeed * deltaTime));
    }

    private bool IsGameplayStarted()
    {
        return Global.GameplayLevelController == null || Global.GameplayLevelController.IsGameplayStarted;
    }
}
