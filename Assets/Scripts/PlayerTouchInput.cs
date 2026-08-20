using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerTouchInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private DynamicJoystick _joystick;

    private bool _isPressed;
    private bool _isExternalPress;

    public event Action Pressed;
    public event Action Released;

    public bool IsPressed => _isPressed;
    public Vector2 Direction => _joystick != null ? _joystick.Direction : Vector2.zero;

    private void Awake()
    {
        Global.RegisterPlayerTouchInput(this);
    }

    private void OnDestroy()
    {
        Global.UnregisterPlayerTouchInput(this);
    }

    private void Update()
    {
        if (_isExternalPress == false)
            return;

        if (TryGetPressedPointerPosition(out Vector2 position) == false)
        {
            EndPress(null);
            return;
        }

        UpdateJoystick(position);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        BeginPress(eventData, false);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        EndPress(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isExternalPress == false && _joystick != null)
            _joystick.OnDrag(eventData);
    }

    public void BeginExternalPress(PointerEventData eventData)
    {
        if (IsPointerPressed() == false)
            return;

        BeginPress(eventData, true);
    }

    public void DragExternalPress(PointerEventData eventData)
    {
        if (_isExternalPress && _joystick != null)
            _joystick.OnDrag(eventData);
    }

    public void EndExternalPress(PointerEventData eventData)
    {
        if (_isExternalPress)
            EndPress(eventData);
    }

    private void BeginPress(PointerEventData eventData, bool isExternal)
    {
        if (_isPressed)
            return;

        _isPressed = true;
        _isExternalPress = isExternal;

        if (_joystick != null && eventData != null)
            _joystick.OnPointerDown(eventData);

        Pressed?.Invoke();
    }

    private void EndPress(PointerEventData eventData)
    {
        if (_isPressed == false)
            return;

        if (_joystick != null)
            _joystick.OnPointerUp(eventData);

        _isPressed = false;
        _isExternalPress = false;
        Released?.Invoke();
    }

    private void UpdateJoystick(Vector2 position)
    {
        if (_joystick == null)
            return;

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = position
        };

        _joystick.OnDrag(eventData);
    }

    private bool TryGetPressedPointerPosition(out Vector2 position)
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            position = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }

        position = Vector2.zero;
        return false;
    }

    private bool IsPointerPressed()
    {
        return (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            || (Mouse.current != null && Mouse.current.leftButton.isPressed);
    }
}
