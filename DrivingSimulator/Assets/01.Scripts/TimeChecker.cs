using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TimeChecker : MonoBehaviour
{
    public float limitTime;
    public Text timerText;
    public bool isGoodScene;
    private void Awake()
    {
        limitTime *= 60;
    }
    // Update is called once per frame
    void Update()
    {
        limitTime -= Time.deltaTime;
        int mint = (int)(limitTime) / 60;
        int sec = (int)(limitTime) % 60;
        timerText.text = "½Ã°£ : " + mint + ":" + sec;

        if (limitTime < 0)
        {
            if (isGoodScene)
            {
                SceneManager.LoadScene("BadScene");
            }
            else
            {
                SceneManager.LoadScene("GoodScene");
            }

        }
    }
}
