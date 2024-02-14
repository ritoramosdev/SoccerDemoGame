using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Photon.Deterministic;

namespace Quantum {
    public unsafe class MovementSystem : SystemMainThreadFilter<MovementSystem.Filter> {

        FPVector3[] SpawnPositions = new FPVector3[] {
            new FPVector3(-3, 0, -2),
            new FPVector3(-1, 0, -2),
            new FPVector3(1, 0, -2),
            new FPVector3(3, 0, -2),
        };

        public struct Filter {
            public EntityRef entity;
            public CharacterController3D* characterController;
            public Transform3D* transform;
            public PlayerStat* playerStat;
            public PlayerLink* playerLink;
        }

        public override void Update(Frame f, ref Filter filter) {
            GameSession* gamesession = f.Unsafe.GetPointerSingleton<GameSession>();

            if (gamesession == null) return;
            if (gamesession->State == GameState.Restart) {
                filter.playerStat->cantBlock = false;
                filter.transform->Position = GetSpawnpointPosition(filter.playerLink->Player);
                //Log.Debug("Entity: " + filter.playerLink->Player + " Score: " + filter.playerStat->ScoreCount);

            }

            if (gamesession->State == GameState.GameOver || 
                gamesession->State == GameState.GoalCountdown ||
                gamesession->State == GameState.Restart) 
                return;

            var input = f.GetPlayerInput(filter.playerLink->Player);
            var inputVector = new FPVector2((FP)input->InputVectorX / 10, (FP)input->InputVectorY / 10);
            if (inputVector.SqrMagnitude > 1) {
                inputVector = inputVector.Normalized;
            }
                

            filter.characterController->Move(f, filter.entity, inputVector.XOY);

            if (!filter.playerStat->cantBlock) {
                if (input->Block.WasPressed) {
                    // Do ball physics here.

                    foreach (var pair in f.GetComponentIterator<PhysicsBody3D>()) {
                        var component = pair.Component;
                        component.AddLinearImpulse(new FPVector3(0, 1 / 2, -1));
                        f.Set(pair.Entity, component);
                    }

                    filter.playerStat->cantBlock = true;
                }
            }
        }

        public void ResetPosition() { 
        
        }

        FPVector3 GetSpawnpointPosition(int playerNumber) {
            return SpawnPositions[playerNumber];
        }
    }
}