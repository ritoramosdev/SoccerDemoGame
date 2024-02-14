using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quantum;
using Photon.Realtime;
using TMPro;

public class InGameUIHandler : MonoBehaviour
{
    [SerializeField] public GameObject goalPanel;
    [SerializeField] public GameObject gameoverPanel;
    [SerializeField] public GameObject scoreBoardParent;
    [SerializeField] public GameObject playerNamePrefab;
    [SerializeField] public TextMeshProUGUI winText;

    private QuickMatchHandler quickMatchHandler;
    private bool winAwarded;

    private void Start() {
        quickMatchHandler = FindAnyObjectByType<QuickMatchHandler>();
    }

    private void Update() {
        if (Utils.TryGetQuantumFrame(out Frame frame)) {
            if (frame.TryGetSingletonEntityRef<GameSession>(out var entity) == false) {
                return;
            }
        }

        if (frame == null) return;
        var gameSession = frame.GetSingleton<GameSession>();
        
        //if (gameSession == null) return;

        int goalCountDown = (int)gameSession.GoalCountdownTimer;

        switch (gameSession.State) {
            case GameState.ReturnToLobby:
                if (winAwarded) return;
                GameOver();
                break;

            case GameState.GameOver:
                gameoverPanel.SetActive(true);
                break;

            case GameState.Restart:
                CheckForWinner();
                break;

            case GameState.GoalCountdown:
                goalPanel.SetActive(true);
                break;

            case GameState.Playing:
                goalPanel.SetActive(false);
                break;
        }
    }

    public void CheckForWinner() {
        QuantumGame game = QuantumRunner.Default.Game;

        if (Utils.TryGetQuantumFrame(out Frame frame)) {

            foreach (var stat in frame.GetComponentIterator<PlayerStat>()) {
                if (frame.TryGet(stat.Entity, out PlayerLink playerLink)) {
                    quickMatchHandler.quantumPlayerList.Remove(playerLink.Player);
                    //quickMatchHandler.quantumUsernameList.Remove(playerLink.Player);

                    quickMatchHandler.quantumPlayerList.Add(playerLink.Player, stat.Component);
                    //quickMatchHandler.quantumUsernameList.Add(playerLink.Player, quickMatchHandler.GetUsername());
                }
            }
        }

        UpdatePlayerList(quickMatchHandler.quantumPlayerList, quickMatchHandler.quantumUsernameList);
    }

    public void GameOver() {
        QuantumGame game = QuantumRunner.Default.Game;

        if (Utils.TryGetQuantumFrame(out Frame frame)) {

            foreach (var ball in frame.GetComponentIterator<SoccerBall>()) {
                foreach (var stat in frame.GetComponentIterator<PlayerStat>()) {
                    if (frame.TryGet(stat.Entity, out PlayerLink playerLink)) {
                        if (ball.Component.WinningPlayer == playerLink.Player) {
                            if (game.PlayerIsLocal(playerLink.Player)) {
                                Debug.Log("I Won!");
                                quickMatchHandler.AwardWin();

                                winAwarded = true;
                            }
                        }
                    }
                }
            }
        }

        QuantumRunner.ShutdownAll();
        quickMatchHandler.ShowUI();
    }

    public void UpdatePlayerList(Dictionary<PlayerRef, PlayerStat> quantumPlayerList, Dictionary<PlayerRef, string> quantumUsernameList) {
        ClearParent();

        //for (int i = 0; i < quantumPlayerList.Count; i++) {
        //    InstantiatePlayerOnScoreBoard(quantumUsernameList[i], quantumPlayerList[i].ScoreCount.ToString());
        //}

        foreach (var quantumPlayer in quantumPlayerList) {
            InstantiatePlayerOnScoreBoard("Player " + quantumPlayer.Key._index, quantumPlayer.Value.ScoreCount.ToString());
        }
    }

    public void ClearParent() {
        foreach(Transform child in  scoreBoardParent.transform) {
            Destroy(child.gameObject);
        }
    }

    public void InstantiatePlayerOnScoreBoard(string name, string score) {
        GameObject nameObj = Instantiate(playerNamePrefab, scoreBoardParent.transform);
        PlayerNamePrefab namePrefab = nameObj.GetComponent<PlayerNamePrefab>();
        namePrefab.SetNameText(name);
        namePrefab.SetScoreText(score);
    }
}
