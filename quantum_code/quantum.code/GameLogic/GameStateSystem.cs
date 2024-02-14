using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Photon.Deterministic;

namespace Quantum {
    public unsafe class GameStateSystem : SystemMainThreadFilter<GameStateSystem.Filter> {

        public struct Filter {
            public EntityRef entity;
            public GameSession* gameSession;
        }

        public override void OnInit(Frame f) {
            Log.Debug("Quantum GameStateSystem ONINIT");

            GameSession* gamesession = f.Unsafe.GetPointerSingleton<GameSession>();

            if (gamesession == null) {
                return;
            }

            gamesession->State = GameState.FreeRoam;
        }

        public override void Update(Frame f, ref Filter filter) {
            GameSession* gamesession = f.Unsafe.GetPointerSingleton<GameSession>();

            if(gamesession == null) {
                return;
            }

            gamesession->GoalCountdownTimer = gamesession->GoalCountdownTimer - f.DeltaTime;
            gamesession->GameOverCountdownTimer = gamesession->GameOverCountdownTimer - f.DeltaTime;

            if (gamesession->GoalCountdownTimer < 1 && gamesession->State == GameState.GoalCountdown) {
                gamesession->State = GameState.Restart;
            }
            else if (gamesession->GameOverCountdownTimer < 1 && gamesession->State == GameState.GameOver) {
                gamesession->State = GameState.ReturnToLobby;
            }
            else if (gamesession->State == GameState.Restart) {

                foreach (var pair in f.GetComponentIterator<PlayerStat>()) {
                    var component = pair.Component;
                    if (component.ScoreCount >= 3) {
                        gamesession->GameOverCountdownTimer = 3;
                        gamesession->State = GameState.GameOver;
                        return;
                    }
                }

                gamesession->State = GameState.Playing;
                gamesession->hasGoalScored = false;
            }
        }
    }
}