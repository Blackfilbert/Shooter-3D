using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyCounterView : MonoBehaviour
{
    [SerializeField] private GameplayLevelController _levelController;
    [SerializeField] private RectTransform _parent;
    [SerializeField] private Image _enemyImagePrefab;
    [SerializeField] private Sprite _aliveSprite;
    [SerializeField] private Sprite _deadSprite;

    private readonly Dictionary<EnemyHealth, Image> _itemsByEnemy = new Dictionary<EnemyHealth, Image>();
    private readonly List<Image> _items = new List<Image>();
    private bool _isSubscribed;

    private void OnEnable()
    {
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
    }

    private void OnDestroy()
    {
        Unsubscribe();
        Clear();
    }

    private void Subscribe()
    {
        if (_isSubscribed)
            return;

        if (_levelController == null)
            _levelController = Global.GameplayLevelController;

        if (_levelController == null)
            return;

        _levelController.LevelLoaded += OnLevelLoaded;
        _levelController.EnemyRegistered += OnEnemyRegistered;
        _levelController.EnemyKilled += OnEnemyKilled;
        _isSubscribed = true;
        Rebuild();
    }

    private void Unsubscribe()
    {
        if (_levelController != null && _isSubscribed)
        {
            _levelController.LevelLoaded -= OnLevelLoaded;
            _levelController.EnemyRegistered -= OnEnemyRegistered;
            _levelController.EnemyKilled -= OnEnemyKilled;
        }

        _isSubscribed = false;
    }

    private void OnLevelLoaded(int levelIndex)
    {
        Clear();
    }

    private void OnEnemyRegistered(EnemyHealth enemyHealth)
    {
        AddEnemy(enemyHealth);
    }

    private void OnEnemyKilled(EnemyHealth enemyHealth)
    {
        if (enemyHealth != null && _itemsByEnemy.TryGetValue(enemyHealth, out Image item))
        {
            SetDead(item);
            return;
        }

        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] != null && _items[i].sprite != _deadSprite)
            {
                SetDead(_items[i]);
                return;
            }
        }
    }

    private void Rebuild()
    {
        Clear();

        if (_levelController == null)
            return;

        IReadOnlyList<EnemyHealth> aliveEnemies = _levelController.AliveEnemies;

        for (int i = 0; i < aliveEnemies.Count; i++)
            AddEnemy(aliveEnemies[i]);
    }

    private void AddEnemy(EnemyHealth enemyHealth)
    {
        if (enemyHealth == null || _itemsByEnemy.ContainsKey(enemyHealth))
            return;

        if (_parent == null || _enemyImagePrefab == null)
            return;

        Image item = Instantiate(_enemyImagePrefab, _parent);
        SetAlive(item);
        _itemsByEnemy.Add(enemyHealth, item);
        _items.Add(item);
    }

    private void SetAlive(Image item)
    {
        if (item != null && _aliveSprite != null)
            item.sprite = _aliveSprite;
    }

    private void SetDead(Image item)
    {
        if (item != null && _deadSprite != null)
            item.sprite = _deadSprite;
    }

    private void Clear()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] != null)
                Destroy(_items[i].gameObject);
        }

        _items.Clear();
        _itemsByEnemy.Clear();
    }
}
