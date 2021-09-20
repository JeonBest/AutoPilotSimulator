using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Zenject;
using UnityEngine.SceneManagement;

public class PausePopup : MonoBehaviour
{
    [SerializeField]
    private Button _restartButton;
    [SerializeField]
    private Button _resumeButton;
    [SerializeField]
    private Button _exitButton;


    Canvas _pausePopup;
    GraphicRaycaster _pausePopupRaycast;

    [Inject]
    public void Injected()
    {
        _pausePopup = GetComponent<Canvas>();
        _pausePopup.enabled = false;
        _pausePopupRaycast = GetComponent<GraphicRaycaster>();
        _pausePopupRaycast.enabled = false;

        _restartButton
            .OnClickAsObservable()
            .Subscribe(_ =>
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                Time.timeScale = 1;
            });

        _resumeButton
            .OnClickAsObservable()
            .Subscribe(_ =>
            {
                _pausePopup.enabled = false;
                _pausePopupRaycast.enabled = false;
                Time.timeScale = 1;
            });

        _exitButton
            .OnClickAsObservable()
            .Subscribe(_ =>
            {
                Application.Quit();
            });

    }

}
