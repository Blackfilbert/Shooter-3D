using UnityEngine;

public class SkyboxRotator : MonoBehaviour
{
    private const string RotationProperty = "_Rotation";

    [SerializeField] private Material _skyboxMaterial;
    [SerializeField] private float _rotationSpeed = 1f;
    [SerializeField] private bool _useRenderSettingsSkybox = true;

    private Material _targetMaterial;
    private float _startRotation;

    private void Awake()
    {
        _targetMaterial = _useRenderSettingsSkybox ? RenderSettings.skybox : _skyboxMaterial;

        if (_targetMaterial != null && _targetMaterial.HasProperty(RotationProperty))
            _startRotation = _targetMaterial.GetFloat(RotationProperty);
    }

    private void Update()
    {
        if (_targetMaterial == null || _targetMaterial.HasProperty(RotationProperty) == false)
            return;

        float rotation = _targetMaterial.GetFloat(RotationProperty);
        rotation = Mathf.Repeat(rotation + _rotationSpeed * Time.deltaTime, 360f);
        _targetMaterial.SetFloat(RotationProperty, rotation);
    }

    private void OnDestroy()
    {
        if (_targetMaterial != null && _targetMaterial.HasProperty(RotationProperty))
            _targetMaterial.SetFloat(RotationProperty, _startRotation);
    }
}
