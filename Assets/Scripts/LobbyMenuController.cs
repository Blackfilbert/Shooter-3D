using System;
using UnityEngine;

public class LobbyMenuController : MonoBehaviour
{
    [SerializeField] private LobbyButtonView _defaultButton;
    [SerializeField] private LobbyMenuItem[] _items;
    [SerializeField] private GameObject _inventoryPage;
    [SerializeField] private GameObject _inventoryPageActiveObject;

    private LobbyMenuItem _activeItem;

    public LobbyButtonView ActiveButton => _activeItem.Button;
    public GameObject ActiveMenu => _activeItem.Menu;

    public event Action<LobbyButtonView> ActiveButtonChanged;

    private void Awake()
    {
        Global.RegisterLobbyMenuController(this);
    }

    private void OnEnable()
    {
        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i].Button != null)
                _items[i].Button.Clicked += Open;
        }
    }

    private void Start()
    {
        LobbyButtonView button = _defaultButton != null ? _defaultButton : GetFirstButton();
        Open(button);
    }

    private void OnDisable()
    {
        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i].Button != null)
                _items[i].Button.Clicked -= Open;
        }
    }

    private void OnDestroy()
    {
        Global.UnregisterLobbyMenuController(this);
    }

    public void Open(LobbyButtonView button)
    {
        if (button == null)
            return;

        bool hasButton = false;

        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i].Button == button)
            {
                hasButton = true;
                break;
            }
        }

        if (hasButton == false)
            return;

        for (int i = 0; i < _items.Length; i++)
        {
            bool isActive = _items[i].Button == button;
            ApplyState(_items[i], isActive);

            if (isActive)
                _activeItem = _items[i];
        }

        UpdateInventoryPageActiveObject();
        ActiveButtonChanged?.Invoke(button);
    }

    private void UpdateInventoryPageActiveObject()
    {
        if (_inventoryPageActiveObject != null)
            _inventoryPageActiveObject.SetActive(_activeItem.Menu == _inventoryPage);
    }

    private void ApplyState(LobbyMenuItem item, bool isActive)
    {
        if (item.Button != null)
            item.Button.SetActiveState(isActive);

        if (item.Menu != null)
            item.Menu.SetActive(isActive);
    }

    private LobbyButtonView GetFirstButton()
    {
        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i].Button != null)
                return _items[i].Button;
        }

        return null;
    }

    [Serializable]
    private struct LobbyMenuItem
    {
        [SerializeField] private LobbyButtonView _button;
        [SerializeField] private GameObject _menu;

        public LobbyButtonView Button => _button;
        public GameObject Menu => _menu;
    }
}
