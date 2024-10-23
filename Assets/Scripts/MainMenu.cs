using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] Button single_player_button;
    [SerializeField] Button multi_player_button;
    [SerializeField] Button exit_button;

    void Awake()
    {
        single_player_button.onClick.AddListener(delegate{SinglePlayer();});
        multi_player_button.onClick.AddListener(delegate{MultiPlayer();});
        exit_button.onClick.AddListener(delegate{Exit();});
    }

    void SinglePlayer()
    {
        SceneManager.LoadScene("Singleplayer");
    }

    void MultiPlayer()
    {
        SceneManager.LoadScene("Multiplayer");
    }

    void Exit()
    {
        Application.Quit();
    }
}
