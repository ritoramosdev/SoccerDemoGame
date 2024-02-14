using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Photon.Deterministic;

namespace Quantum {
    public unsafe class SpawnSystem : SystemSignalsOnly, ISignalOnPlayerDataSet, ISignalOnPlayerDisconnected {

        FPVector3[] SpawnPositions = new FPVector3[] {
            new FPVector3(-3, 0, -2),
            new FPVector3(-1, 0, -2),
            new FPVector3(1, 0, -2),
            new FPVector3(3, 0, -2),
        };

        public void OnPlayerDataSet(Frame f, PlayerRef player) {
            var data = f.GetPlayerData(player);
            var prototypeEntity = f.FindAsset<EntityPrototype>(data.characterPrototype.Id);
            var createdEntity = f.Create(prototypeEntity);

            if (f.Unsafe.TryGetPointer<PlayerLink>(createdEntity, out var playerLink)) {
                playerLink->Player = player;
            }

            if (f.Unsafe.TryGetPointer<Transform3D>(createdEntity, out var transform)) {
                transform->Position = GetSpawnpointPosition(player);
            }
        }

        public void OnPlayerDisconnected(Frame f, PlayerRef player) {
            foreach (var playerLink in f.GetComponentIterator<PlayerLink>()) {
                if (playerLink.Component.Player != player)
                    continue;

                f.Destroy(playerLink.Entity);
            }
        }

        FPVector3 GetSpawnpointPosition(int playerNumber) {
            return SpawnPositions[playerNumber];
        }
    }
}