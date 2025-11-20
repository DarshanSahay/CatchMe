using PurrNet;
using PurrNet.Modules;
using PurrNet.Packing;
using PurrNet.Transports;
using System;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private CameraFollow m_Camera;
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        //Invoke(nameof(ChangeTimeScale), 5f);
    }

    public CameraFollow GetCamera()
    {
        return m_Camera;
    }

    [ServerRpc(requireOwnership:false)]
    public void ChangeTimeScale()
    {
        if (!isServer) return;  // Safety

        // Server logic: Validate (e.g., only host? Check sender via RPCInfo)
        // RPCInfo info = default;  // Uncomment & add param for sender check

        // Broadcast to ALL clients (ObserversRpc)
        RpcSetTimeScale(0.3f);
    }

    [ObserversRpc(Channel.ReliableOrdered, bufferLast: true, runLocally: true)]  // Reliable, buffers for late joiners, runs on caller too
    private void RpcSetTimeScale(float newScale)
    {
        Time.timeScale = newScale;

        // IMPORTANT: Scale fixedDeltaTime for consistent physics
        //Time.fixedDeltaTime = 0.02f * newScale;  // Assuming default 50Hz; adjust to your project's fixed timestep

        Debug.Log($"TimeScale changed to {newScale} on {(isServer ? "Server" : "Client")}");
    }
}
