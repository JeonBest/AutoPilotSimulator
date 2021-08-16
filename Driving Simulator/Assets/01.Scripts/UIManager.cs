using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    // Start is called before the first frame update
    public void buttonExit()
    {
        Application.Quit();
    }

    public void buttonReset()
    {
        SceneManager.LoadScene("Main");
    }
}
