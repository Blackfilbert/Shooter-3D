using UnityEngine;

public class UIController : MonoBehaviour
{
    [SerializeField] private RectTransform _popupParent;
    [SerializeField] private UIPopup[] _popupPrefabs;

    private UIPopup[] _spawnedPopups = new UIPopup[0];

    private void Awake()
    {
        if (_popupParent == null)
            _popupParent = transform as RectTransform;

        Global.RegisterUIController(this);
    }

    private void OnDestroy()
    {
        Global.UnregisterUIController(this);
    }

    public T Show<T>() where T : UIPopup
    {
        T popup = GetOrCreatePopup<T>();

        if (popup == null)
            return null;

        popup.Show();
        return popup;
    }

    private T GetOrCreatePopup<T>() where T : UIPopup
    {
        for (int i = 0; i < _spawnedPopups.Length; i++)
        {
            if (_spawnedPopups[i] is T popup)
                return popup;
        }

        T prefab = GetPopupPrefab<T>();

        if (prefab == null || _popupParent == null)
            return null;

        T spawnedPopup = Instantiate(prefab, _popupParent);
        AddSpawnedPopup(spawnedPopup);
        return spawnedPopup;
    }

    private T GetPopupPrefab<T>() where T : UIPopup
    {
        if (_popupPrefabs == null)
            return null;

        for (int i = 0; i < _popupPrefabs.Length; i++)
        {
            if (_popupPrefabs[i] is T popup)
                return popup;
        }

        return null;
    }

    private void AddSpawnedPopup(UIPopup popup)
    {
        int length = _spawnedPopups.Length;
        System.Array.Resize(ref _spawnedPopups, length + 1);
        _spawnedPopups[length] = popup;
    }
}
