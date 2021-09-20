using System.Collections;
using System.Collections.Generic;
using NWH.VehiclePhysics2;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class IdleCanvas : MonoBehaviour
{
    [SerializeField]
    private GameObject _pausePopup;
    [SerializeField]
    private VehicleController _playerVehicle;
    [SerializeField]
    private Button _pauseButton;
    [SerializeField]
    private Text _velocityText;

    Canvas _pauseCanvas;
    GraphicRaycaster _pauseRaycast;

    [Inject]
    public void Injected()
    {
        _pauseCanvas = _pausePopup.GetComponent<Canvas>();
        _pauseRaycast = _pausePopup.GetComponent<GraphicRaycaster>();

        _pauseButton.OnClickAsObservable().Subscribe(_ =>
        {
            _pauseCanvas.enabled = true;
            _pauseRaycast.enabled = true;
            Time.timeScale = 0;
        });
    }

    void Update()
    {
        _velocityText.text = Mathf.Round(_playerVehicle.LocalForwardVelocity * 3.6f).ToString();
        //_remainText.text = Mathf.Round(Time.time).ToString();
    }

}
