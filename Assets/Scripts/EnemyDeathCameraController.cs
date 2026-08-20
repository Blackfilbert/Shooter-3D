using System;
using System.Collections;
using UnityEngine;

public class EnemyDeathCameraController : MonoBehaviour
{
    private const float ProjectileMinimumFinishDelay = 1.25f;

    [SerializeField] private Camera _camera;
    [SerializeField] private GameObject _playerInterface;
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private float _duration = 2f;
    [SerializeField] private float _distance = 5f;
    [SerializeField] private float _sideOffset = 2f;
    [SerializeField] private float _viewAngle = 50f;
    [SerializeField] private float _targetHeight = 1f;
    [SerializeField] private float _focusTimeScale = 0.65f;
    [SerializeField] private float _focusFov = 30f;
    [SerializeField] private float _projectileFollowDistance = 1.5f;
    [SerializeField] private float _projectileSideOffset = 0.25f;
    [SerializeField] private float _projectileHeightOffset = 0.2f;
    [SerializeField] private float _projectileLookAhead = 2f;
    [SerializeField] private float _projectileFinishDelay = 0.35f;

    private Coroutine _focusRoutine;
    private float _startTimeScale = 1f;
    private float _startFov = 60f;
    private bool _hasStoredTimeScale;

    public bool IsFocusing => _focusRoutine != null;

    public event Action FocusCompleted;

    private void Awake()
    {
        Global.RegisterEnemyDeathCameraController(this);
    }

    private void OnDestroy()
    {
        RestoreTimeScale();
        Global.UnregisterEnemyDeathCameraController(this);
    }

    public bool Focus(Transform target)
    {
        if (target == null)
            return false;

        if (_camera == null && Global.PlayerWeapon != null)
            _camera = Global.PlayerWeapon.Camera;

        if (_playerMovement == null)
            _playerMovement = Global.PlayerMovement;

        if (_camera == null)
            return false;

        if (_focusRoutine != null)
            return false;

        _focusRoutine = StartCoroutine(FocusRoutine(target));
        return true;
    }

    public bool FocusProjectile(Transform projectile)
    {
        if (projectile == null)
            return false;

        if (_camera == null && Global.PlayerWeapon != null)
            _camera = Global.PlayerWeapon.Camera;

        if (_playerMovement == null)
            _playerMovement = Global.PlayerMovement;

        if (_camera == null)
            return false;

        if (_focusRoutine != null)
            return false;

        _focusRoutine = StartCoroutine(ProjectileFocusRoutine(projectile));
        return true;
    }

    public void CancelFocus()
    {
        if (_focusRoutine != null)
        {
            StopCoroutine(_focusRoutine);
            _focusRoutine = null;
        }

        RestoreTimeScale();

        if (_playerInterface != null)
            _playerInterface.SetActive(false);

        SetPreviewCameraObjectVisible(false);
    }

    private IEnumerator FocusRoutine(Transform target)
    {
        Transform cameraTransform = _camera.transform;
        Vector3 startPosition = cameraTransform.position;
        Quaternion startRotation = cameraTransform.rotation;
        _startFov = _camera.fieldOfView;
        bool wasInterfaceActive = _playerInterface != null && _playerInterface.activeSelf;
        bool wasPlayerMovementEnabled = _playerMovement != null && _playerMovement.enabled;
        _startTimeScale = Time.timeScale;
        _hasStoredTimeScale = true;

        if (_playerInterface != null)
            _playerInterface.SetActive(false);

        SetPreviewCameraObjectVisible(false);

        if (_playerMovement != null)
            _playerMovement.enabled = false;

        _camera.fieldOfView = Mathf.Max(1f, _focusFov);
        Time.timeScale = _focusTimeScale;

        Vector3 targetPosition = target.position + Vector3.up * _targetHeight;
        float angleRadians = _viewAngle * Mathf.Deg2Rad;
        float height = Mathf.Sin(angleRadians) * _distance;
        float backDistance = Mathf.Cos(angleRadians) * _distance;
        Vector3 cameraDirection = -target.forward * backDistance + target.right * _sideOffset + Vector3.up * height;

        cameraTransform.position = targetPosition + cameraDirection;
        cameraTransform.LookAt(targetPosition);

        yield return new WaitForSecondsRealtime(_duration);

        RestoreTimeScale();
        cameraTransform.position = startPosition;
        cameraTransform.rotation = startRotation;
        _camera.fieldOfView = GetRestoreFov();

        if (_playerInterface != null)
            _playerInterface.SetActive(wasInterfaceActive && ShouldRestoreGameplayView());

        SetPreviewCameraObjectVisible(ShouldRestorePreviewView());

        if (_playerMovement != null)
            _playerMovement.enabled = wasPlayerMovementEnabled;

        _focusRoutine = null;
        FocusCompleted?.Invoke();
    }

    private IEnumerator ProjectileFocusRoutine(Transform projectile)
    {
        Transform cameraTransform = _camera.transform;
        Vector3 startPosition = cameraTransform.position;
        Quaternion startRotation = cameraTransform.rotation;
        _startFov = _camera.fieldOfView;
        bool wasInterfaceActive = _playerInterface != null && _playerInterface.activeSelf;
        bool wasPlayerMovementEnabled = _playerMovement != null && _playerMovement.enabled;
        _startTimeScale = Time.timeScale;
        _hasStoredTimeScale = true;

        if (_playerInterface != null)
            _playerInterface.SetActive(false);

        SetPreviewCameraObjectVisible(false);

        if (_playerMovement != null)
            _playerMovement.enabled = false;

        _camera.fieldOfView = Mathf.Max(1f, _focusFov);
        Time.timeScale = _focusTimeScale;

        while (projectile != null)
        {
            Vector3 projectilePosition = projectile.position;
            Vector3 forward = projectile.forward;
            Vector3 right = projectile.right;
            Vector3 up = Vector3.up;

            cameraTransform.position = projectilePosition
                - forward * Mathf.Max(0f, _projectileFollowDistance)
                + right * _projectileSideOffset
                + up * _projectileHeightOffset;
            cameraTransform.LookAt(projectilePosition + forward * Mathf.Max(0.1f, _projectileLookAhead));
            yield return null;
        }

        float finishDelay = Mathf.Max(ProjectileMinimumFinishDelay, _projectileFinishDelay);

        if (finishDelay > 0f)
            yield return new WaitForSecondsRealtime(finishDelay);

        RestoreTimeScale();
        cameraTransform.position = startPosition;
        cameraTransform.rotation = startRotation;
        _camera.fieldOfView = GetRestoreFov();

        if (_playerInterface != null)
            _playerInterface.SetActive(wasInterfaceActive && ShouldRestoreGameplayView());

        SetPreviewCameraObjectVisible(ShouldRestorePreviewView());

        if (_playerMovement != null)
            _playerMovement.enabled = wasPlayerMovementEnabled;

        _focusRoutine = null;
        FocusCompleted?.Invoke();
    }

    private void RestoreTimeScale()
    {
        if (_hasStoredTimeScale == false)
            return;

        Time.timeScale = _startTimeScale;
        _hasStoredTimeScale = false;
    }

    private bool IsGameplayStarted()
    {
        return Global.GameplayLevelController == null || Global.GameplayLevelController.IsGameplayStarted;
    }

    private bool ShouldRestoreGameplayView()
    {
        return Global.GameplayLevelController == null
            || Global.GameplayLevelController.IsGameplayStarted
            && Global.GameplayLevelController.IsLevelFinished == false
            && Global.GameplayLevelController.IsWinScheduled == false
            && Global.GameplayLevelController.AliveEnemies.Count > 0;
    }

    private bool ShouldRestorePreviewView()
    {
        return Global.GameplayLevelController != null
            && Global.GameplayLevelController.IsGameplayStarted == false
            && Global.GameplayLevelController.IsLevelFinished == false
            && Global.GameplayLevelController.IsWinScheduled == false
            && Global.GameplayLevelController.AliveEnemies.Count > 0;
    }

    private float GetRestoreFov()
    {
        if (Global.GameplayLevelController != null && Global.GameplayLevelController.IsGameplayStarted == false)
            return Mathf.Max(1f, Global.GameplayLevelController.PreviewFov);

        return _startFov;
    }

    private void SetPreviewCameraObjectVisible(bool isVisible)
    {
        if (Global.GameplayLevelController != null)
            Global.GameplayLevelController.SetPreviewCameraObjectVisible(isVisible);
    }
}
