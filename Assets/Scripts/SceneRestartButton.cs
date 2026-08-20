using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneRestartButton : MonoBehaviour
{
    [SerializeField] private Button _button;

    private void Awake()
    {
        if (_button == null)
            _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (_button != null)
            _button.onClick.AddListener(Restart);
    }

    private void OnDisable()
    {
        if (_button != null)
            _button.onClick.RemoveListener(Restart);
    }

    private void Restart()
    {
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }
}
