using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quantum;
using Cinemachine;
using TMPro;
using Photon.Realtime;
using ExitGames.Client.Photon.StructWrapping;
using Photon.Deterministic;

public class PlayerHandler : MonoBehaviour {
    [SerializeField] private EntityView entityView;
    [SerializeField] private GameObject playerInfoObj;
    [SerializeField] private TextMeshProUGUI playerNameText;
    public void OnEntityInstantiated() {
        Debug.Log("Player Character Spawned");

        QuantumGame game = QuantumRunner.Default.Game;
        Frame frame = game.Frames.Verified;

        if (frame.TryGet(entityView.EntityRef, out PlayerLink playerLink)) {
            if (game.PlayerIsLocal(playerLink.Player)) {
                playerInfoObj.SetActive(true);

                CinemachineVirtualCamera playerVirtualCam = FindAnyObjectByType<CinemachineVirtualCamera>();
                playerVirtualCam.m_Follow = transform;

                QuickMatchHandler quickMatchHandler = FindObjectOfType<QuickMatchHandler>();
                InGameUIHandler inGameUIHandler = FindObjectOfType<InGameUIHandler>();

                playerNameText.text = "Player " + playerLink.Player._index;
                quickMatchHandler.SetIngameUsername();

                //if(frame.TryGet(entityView.EntityRef, out PlayerStat playerStat))
                //    quickMatchHandler.quantumPlayerList.Add(playerLink.Player, playerStat);

                foreach (var stat in frame.GetComponentIterator<PlayerStat>()) {
                    if (frame.TryGet(stat.Entity, out PlayerLink link)) {
                        quickMatchHandler.quantumPlayerList.Add(link.Player, stat.Component);
                        quickMatchHandler.quantumUsernameList.Add(link.Player, quickMatchHandler.UsernameText.text);
                    }
                }

                inGameUIHandler.UpdatePlayerList(quickMatchHandler.quantumPlayerList, quickMatchHandler.quantumUsernameList);
            }
            else
                playerInfoObj.SetActive(false);
        }
    }
}
