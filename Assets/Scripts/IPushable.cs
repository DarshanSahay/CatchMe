using PurrNet;
using UnityEngine;

public interface IPushable
{
    void ApplyExternalForce(PlayerID target, Vector3 force, float duration);
}
