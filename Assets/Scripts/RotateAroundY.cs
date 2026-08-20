using UnityEngine;

public class RotateAroundY : MonoBehaviour
{
    [SerializeField] private float _degreesPerSecond = 180f;
    [SerializeField] private bool _useUnscaledTime;

    private void Update()
    {
        float deltaTime = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        transform.Rotate(0f, _degreesPerSecond * deltaTime, 0f, Space.Self);
    }
}
