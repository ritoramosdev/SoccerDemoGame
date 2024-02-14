using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerNamePrefab : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI scoreText;

    public void SetNameText(string name) {
        nameText.text = name;
    }

    public void SetScoreText(string score) {  
        scoreText.text = "| " + score;
    
    }
}
