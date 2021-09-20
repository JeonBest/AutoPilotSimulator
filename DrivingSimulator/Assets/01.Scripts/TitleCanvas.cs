using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleCanvas : MonoBehaviour
{
    [SerializeField]
    private Button _toGoodButton;
    [SerializeField]
    private Button _toBadButton;
    [SerializeField]
    private Button _exitButton;

    // Start is called before the first frame update
    void Start()
    {
        _toGoodButton.OnClickAsObservable().Subscribe(_ =>
        {
            SceneManager.LoadScene("GoodScene");
        });
        _toBadButton.OnClickAsObservable().Subscribe(_ =>
        {
            SceneManager.LoadScene("BadScene");
        });
        _exitButton.OnClickAsObservable().Subscribe(_ =>
        {
            Application.Quit();
        });
    }

}
