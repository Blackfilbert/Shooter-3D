using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerDamageView : MonoBehaviour
{
    [SerializeField] private PlayerWeapon _playerWeapon;
    [SerializeField] private TMP_Text _damageText;
    [SerializeField] private float _stepInterval = 0.03f;

    private Coroutine _animationCoroutine;
    private GameplayLevelController _levelController;
    private bool _isSubscribed;
    private int _displayedDamage;
    private int _pendingDamage;
    private bool _hasPendingDamage;

    private void OnEnable()
    {
        KillFeedbackView.DamageFeedbackArrived += OnDamageFeedbackArrived;
        Subscribe();
    }

    private void Start()
    {
        Subscribe();
    }

    private void Update()
    {
        if (_isSubscribed == false)
            Subscribe();

        if (_levelController == null)
            SubscribeLevelController();
    }

    private void OnDisable()
    {
        KillFeedbackView.DamageFeedbackArrived -= OnDamageFeedbackArrived;

        if (_playerWeapon != null && _isSubscribed)
            _playerWeapon.DamageChanged -= OnDamageChanged;

        UnsubscribeLevelController();
        StopAnimation();
        _isSubscribed = false;
        _hasPendingDamage = false;
    }

    private void Subscribe()
    {
        if (_isSubscribed)
            return;

        if (_playerWeapon == null)
            _playerWeapon = Global.PlayerWeapon;

        if (_playerWeapon == null)
            return;

        _playerWeapon.DamageChanged += OnDamageChanged;
        _isSubscribed = true;
        SubscribeLevelController();
        _displayedDamage = _playerWeapon.Damage;
        UpdateText(_displayedDamage);
        SetVisible(true);
    }

    private void OnDamageChanged(int damage)
    {
        if (KillFeedbackView.IsDamageFeedbackPending)
        {
            _pendingDamage = damage;
            _hasPendingDamage = true;
            return;
        }

        ShowDamage(damage);
    }

    private void OnDamageFeedbackArrived()
    {
        if (_hasPendingDamage == false)
            return;

        _hasPendingDamage = false;
        ShowDamage(_pendingDamage);
    }

    private void ShowDamage(int damage)
    {
        if (gameObject.activeInHierarchy == false)
        {
            _displayedDamage = damage;
            UpdateText(_displayedDamage);
            return;
        }

        StopAnimation();
        _animationCoroutine = StartCoroutine(AnimateDamage(damage));
    }

    private IEnumerator AnimateDamage(int targetDamage)
    {
        int direction = targetDamage >= _displayedDamage ? 1 : -1;
        float waitTime = Mathf.Max(0f, _stepInterval);

        while (_displayedDamage != targetDamage)
        {
            _displayedDamage += direction;
            UpdateText(_displayedDamage);

            if (waitTime > 0f)
                yield return new WaitForSeconds(waitTime);
            else
                yield return null;
        }

        _animationCoroutine = null;
    }

    private void StopAnimation()
    {
        if (_animationCoroutine == null)
            return;

        StopCoroutine(_animationCoroutine);
        _animationCoroutine = null;
    }

    private void UpdateText(int damage)
    {
        if (_damageText != null)
            _damageText.text = $"DMG {damage} <sprite=5>";
    }

    private void SubscribeLevelController()
    {
        if (_levelController != null || Global.GameplayLevelController == null)
            return;

        _levelController = Global.GameplayLevelController;
        _levelController.LevelWon += Hide;
        _levelController.LevelLost += Hide;
    }

    private void UnsubscribeLevelController()
    {
        if (_levelController == null)
            return;

        _levelController.LevelWon -= Hide;
        _levelController.LevelLost -= Hide;
        _levelController = null;
    }

    private void Hide()
    {
        StopAnimation();
        SetVisible(false);
    }

    private void SetVisible(bool isVisible)
    {
        if (_damageText != null)
            _damageText.gameObject.SetActive(isVisible);
    }
}
