using PurrNet;
using PurrNet.Modules;
using PurrNet.Packing;
using UnityEngine;

public class GameManager : NetworkIdentity
{
    [SerializeField] private CameraFollow m_Camera;
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public CameraFollow GetCamera()
    {
        return m_Camera;
    }
}
