using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Photon.Deterministic;

namespace Quantum {
    public unsafe class SoccerBallPhysics : SystemMainThreadFilter<SoccerBallPhysics.Filter> {
        public struct Filter {
            public EntityRef entity;
            public PhysicsBody3D* body3D;
            public SoccerBall* soccerBall;
            public Transform3D* transform;
        }

        EntityRef lastTouchEntity;
        bool hasBallSpawned;

        public override void Update(Frame f, ref Filter filter) {
            GameSession* gamesession = f.Unsafe.GetPointerSingleton<GameSession>();
            if (gamesession == null) return;

            if (!hasBallSpawned) {
                var playerCount = 0;
                foreach (var pair in f.GetComponentIterator<PlayerStat>()) {
                    playerCount++;
                }

                if (playerCount > 1) {
                    filter.transform->Position = ResetPosition();
                    filter.body3D->Velocity = FPVector3.Zero;
                    filter.body3D->AngularVelocity = FPVector3.Zero;

                    hasBallSpawned = true;
                    gamesession->State = GameState.Playing;
                }
            }

            if (gamesession->State == GameState.Restart) {
                filter.transform->Position = ResetPosition();
                filter.body3D->Velocity = FPVector3.Zero;
                filter.body3D->AngularVelocity = FPVector3.Zero;

                var playerRef = f.Get<PlayerLink>(lastTouchEntity);
                filter.soccerBall->WinningPlayer = playerRef.Player;

                //Log.Debug("Entity: " + lastTouchEntity.Index);
            }
            Physics3D.HitCollection3D hitCollection3D = f.Physics3D.OverlapShape(filter.transform->Position, FPQuaternion.Identity, Shape3D.CreateSphere((FP)1 / 4));
            
            for (int i = 0; i < hitCollection3D.Count; i++) {
                if (hitCollection3D[i].IsTrigger) {
                    gamesession->State = GameState.GoalCountdown;
                    gamesession->GoalCountdownTimer = 1;

                    if (!gamesession->hasGoalScored) {
                        foreach (var pair in f.GetComponentIterator<PlayerStat>()) {
                            if (lastTouchEntity == pair.Entity) {
                                var component = pair.Component;
                                component.ScoreCount = component.ScoreCount + 1;
                                f.Set(pair.Entity, component);
                            }
                        }

                        gamesession->hasGoalScored = true;
                    }
                }
                else if (f.Has<CharacterController3D>(hitCollection3D[i].Entity)) {
                    var hitObj = f.Get<Transform3D>(hitCollection3D[i].Entity);
                    filter.body3D->AddLinearImpulse((filter.transform->Position - hitObj.Position).Normalized / 10);

                    lastTouchEntity = hitCollection3D[i].Entity;
                }
            }            
        }

        FPVector3 ResetPosition() {
            Random r = new Random();
            int rInt = r.Next(-2, 2);

            return new FPVector3(0, 3, 0);
        }
    }
}