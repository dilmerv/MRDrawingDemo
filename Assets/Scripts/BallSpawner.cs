using System;
using Meta.XR.ImmersiveDebugger;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Unity.Netcode.NetworkObject))]
public class BallSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject ballPrefab;

    [DebugMember(Category = "Paintball", Tweakable = true, Min = 0, Max = 256)]
    [SerializeField, Range(0, 128)] private int maximumNumberOfBalls;

    private DateTime _lastTimeCheckedBallCount;
    private readonly TimeSpan _checkBallCountInterval = TimeSpan.FromMilliseconds(500);

    private void Update()
    {
        if (Paintball.InstanceCount >= maximumNumberOfBalls)
        {
            // Do not spawn if limit reached
            return;
        }

        var currentTime = DateTime.Now;
        if (currentTime - _lastTimeCheckedBallCount < _checkBallCountInterval)
        {
            // Check cooldown
            return;
        }

        _lastTimeCheckedBallCount = currentTime;

        SpawnBall();
    }

    [DebugMember(Category = "Paintball")]
    private void SpawnBall()
    {
        if (!IsServer || !IsSpawned)
        {
            // Early returns if not allowed to spawn
            return;
        }

        SpawnNetworkObject(ballPrefab);
    }

    private static void SpawnNetworkObject(GameObject prefab)
    {
        var instance = Instantiate(prefab);
        var networkObj = instance.GetComponent<NetworkObject>();
        networkObj.Spawn();
    }
}
