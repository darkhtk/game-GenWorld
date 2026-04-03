using System.Collections;
using UnityEngine;

public class DialogueCameraZoom : MonoBehaviour
{
    [SerializeField] float zoomInSize = 3f;
    [SerializeField] float zoomSpeed = 3f;

    Camera _cam;
    float _originalSize;
    bool _zooming;
    float _targetSize;

    void Awake()
    {
        _cam = Camera.main;
        if (_cam != null) _originalSize = _cam.orthographicSize;

        EventBus.On<DialogueStartEvent>(_ => ZoomIn());
        EventBus.On<DialogueEndEvent>(_ => ZoomOut());
    }

    void ZoomIn()
    {
        if (_cam == null) return;
        _originalSize = _cam.orthographicSize;
        _targetSize = zoomInSize;
        _zooming = true;
    }

    void ZoomOut()
    {
        if (_cam == null) return;
        _targetSize = _originalSize;
        _zooming = true;
    }

    void OnDisable()
    {
        if (_cam != null && _zooming)
        {
            _cam.orthographicSize = _originalSize;
            _zooming = false;
        }
    }

    void Update()
    {
        if (!_zooming || _cam == null) return;
        _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetSize, zoomSpeed * Time.deltaTime);
        if (Mathf.Abs(_cam.orthographicSize - _targetSize) < 0.01f)
        {
            _cam.orthographicSize = _targetSize;
            _zooming = false;
        }
    }
}
