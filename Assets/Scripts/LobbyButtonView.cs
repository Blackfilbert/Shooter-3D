using System;
using UnityEngine;
using UnityEngine.UI;

public class LobbyButtonView : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private GameObject _normalView;
    [SerializeField] private GameObject _selectedView;

    public event Action<LobbyButtonView> Clicked;

    private void Awake()
    {
        if (_button == null)
            _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (_button != null)
            _button.onClick.AddListener(OnClick);
    }

    private void OnDisable()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnClick);
    }

    public void SetActiveState(bool isActive)
    {
        if (_normalView != null)
            _normalView.SetActive(isActive == false);

        if (_selectedView != null)
            _selectedView.SetActive(isActive);

        if (_button != null && isActive)
            _button.Select();
    }

    private void OnClick()
    {
        Clicked?.Invoke(this);
    }
}
