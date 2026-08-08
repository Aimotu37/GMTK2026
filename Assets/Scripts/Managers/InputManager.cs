using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : SingletonMono<InputManager>
{
    private bool _isInputEnabled = true;
    public bool IsInputEnabled => _isInputEnabled;

    private bool _isInitialized;
    public bool IsInitialized => _isInitialized;

    private bool _initFailed;

    public IEnumerator InitializeAsync()
    {
        if (_isInitialized)
        {
            yield break;
        }

        _initFailed = false;

        if (_initFailed)
        {
            _isInitialized = false;
            Debug.LogError(this.name + " Initialization Failed.");
        }
        else
        {
            _isInitialized = true;
            SetInputEnabled(false);
            Debug.Log(this.name + " Initialization Successful.");
        }
    }

    public void SetInputEnabled(bool enabled)
    {
        _isInputEnabled = enabled;
    }

    public bool GetKeyDown(KeyCode key)
    {
        return _isInputEnabled && Input.GetKeyDown(key);
    }

    public bool GetKey(KeyCode key)
    {
        return _isInputEnabled && Input.GetKey(key);
    }

    public bool GetKeyUp(KeyCode key)
    {
        return _isInputEnabled && Input.GetKeyUp(key);
    }

    public float GetAxis(string axisName)
    {
        return _isInputEnabled ? Input.GetAxis(axisName) : 0f;
    }

    public float GetAxisRaw(string axisName)
    {
        return _isInputEnabled ? Input.GetAxisRaw(axisName) : 0f;
    }

    public Vector2 GetMousePosition()
    {
        return _isInputEnabled ? Input.mousePosition : Vector2.zero;
    }

    public bool GetMouseDown(int button)
    {
        return _isInputEnabled ? Input.GetMouseButtonDown(button) : false;
    }

    public bool GetMouse(int button)
    {
        return _isInputEnabled ? Input.GetMouseButton(button) : false;
    }

    public bool GetMouseUp(int button)
    {
        return _isInputEnabled ? Input.GetMouseButtonUp(button) : false;
    }
}
