using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.SocialPlatforms.Impl;


public class ResultsScreen : MonoBehaviour
{
    [SerializeField] private Button exit_button;
    [SerializeField] private TextMeshProUGUI p1_score;
    [SerializeField] private TextMeshProUGUI p2_score;

    void Awake()
    {
        exit_button.onClick.AddListener(delegate{Exit();});
    }

    void Start()
    {
        print(ScoreManager.p1_score);
        p1_score.text = Convert.ToString(ScoreManager.p1_score);
        p2_score.text = Convert.ToString(ScoreManager.p2_score);
    }

    void Exit()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
