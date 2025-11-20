using PurrNet;
using PurrNet.Transports;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPowerHandler : NetworkBehaviour, IPushable
{
    [SerializeField] float dashSpeed = 20f;
    [SerializeField] float dashDuration = 0.15f;
    [SerializeField] private CharacterController controller;
    private bool isDashing = false;
    public GameObject dashEffect;


    public float shockRadius = 5f;
    public float shockForce = 20f;
    public LayerMask affectedLayers;
    public LayerMask runnerLayer;
    public LayerMask catcherLayer;
    public GameObject shockwaveEffect;

    private Vector3 externalForce;
    private float externalForceTimer;

    [SerializeField] private GameObject particle;
    private bool enabledPoisonGas = false;
    [SerializeField] float poisonGasDuration = 5f;

    [SerializeField] private GameObject playerClone;
    [SerializeField] private MovementRecorder recorder;

    [Tooltip("Base seconds to rewind when rewindSpeed = 1")]
    public float rewindTime = 0.75f;

    [Tooltip("Multiplier: >1 = faster, <1 = slower")]
    public float rewindSpeed = 1f;

    //public float rewindDuration = 0.75f; // how fast we rewind
    private bool isRewinding = false;
    private int index;


    protected override void OnSpawned()
    {
        base.OnSpawned();

        if (isOwner)
        {
            gameObject.layer = 7;
        }
        else
        {
            gameObject.layer = 8;
        }

        enabled = isOwner;

        SetPoisonGasDuration();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L) && !isDashing)
        {
            StartCoroutine(Dash());
        }

        if (Input.GetKeyUp(KeyCode.K))
        {
            ActivateShockwave();
        }

        if (Input.GetKeyUp(KeyCode.J) && !enabledPoisonGas)
        {
            ActivatePoisonGas(owner.Value);
        }

        if (Input.GetKeyUp(KeyCode.H))
        {
            ActivateClone(owner.Value);
        }

        if (Input.GetKeyDown(KeyCode.G) && !isRewinding)
        {
            ActivateRewind_RPC(owner.Value);
        }

        if (externalForceTimer > 0)
        {
            controller.Move(externalForce * Time.deltaTime);
            externalForceTimer -= Time.deltaTime;
        }
    }

    private IEnumerator Dash()
    {
        isDashing = true;

        dashEffect.SetActive(true);
        Vector3 dashDirection = transform.forward;
        float timer = 0f;

        while (timer < dashDuration)
        {
            controller.Move(dashDirection * dashSpeed * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        isDashing = false;

        yield return new WaitForSeconds(0.2f);
        dashEffect.SetActive(false);
    }

    public void ActivateShockwave()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, shockRadius, affectedLayers);
        shockwaveEffect.SetActive(true);

        foreach (Collider hit in hits)
        {
            PlayerID hitPlayerId;

            Debug.Log("Shockwave hit: " + hit.name);
            IPushable pushable = hit.GetComponent<IPushable>();

            hitPlayerId = hit.GetComponent<PlayerMovement>().owner.Value;


            if (pushable != null)
            {
                Vector3 direction = (hit.transform.position - transform.position).normalized;
                pushable.ApplyExternalForce(hitPlayerId, direction * shockForce, 0.2f);
            }
        }
    }


    [TargetRpc(Channel.ReliableUnordered)]
    public void ApplyExternalForce(PlayerID target, Vector3 force, float duration)
    {
        if (target != owner)  //if not the intended target, ignore
            return;

        externalForce = force;
        externalForceTimer = duration;
    }

    private void SetPoisonGasDuration()
    {
        ParticleSystem poisonGas = particle.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule module = poisonGas.main;
        module.duration = poisonGasDuration;
    }


    [ObserversRpc(Channel.ReliableUnordered)]
    public void ActivatePoisonGas(PlayerID target)
    {
        StartCoroutine(ActivatePoisonGas_RPC(target));
    }


    public IEnumerator ActivatePoisonGas_RPC(PlayerID target)
    {
        if (target != owner)  //if not the intended target, ignore
            yield return null;

        enabledPoisonGas = true;
        particle.SetActive(true);

        ParticleSystem poisonGas = particle.GetComponent<ParticleSystem>();
        poisonGas.Play();

        yield return new WaitUntil(() => !poisonGas.isPlaying);

        enabledPoisonGas = false;
        particle.SetActive(false);
    }

    [ObserversRpc(Channel.ReliableUnordered)]
    public void ActivateClone(PlayerID target)
    {
        StartCoroutine(ActivateClone_RPC(target));
    }

    public IEnumerator ActivateClone_RPC(PlayerID target)
    {
        if (target != owner)  //if not the intended target, ignore
            yield return null;

        GameObject clone = Instantiate(playerClone, transform.position + transform.forward * 2, transform.rotation);
        clone.name = "PlayerClone" + target.ToString();

        yield return new WaitForSeconds(0.05f);

        var replay = clone.GetComponent<CloneMovement>();

        var deltas = recorder.MovementDeltas;

        replay.InitReplay(new List<Vector3>(deltas));

        yield return new WaitForSeconds(5f);

        Destroy(clone);
    }

    [ObserversRpc(Channel.ReliableUnordered)]
    public void ActivateRewind_RPC(PlayerID target)
    {
        StartCoroutine(ActivateRewind(target));
    }

    ////IEnumerator ActivateRewind(PlayerID id)
    ////{
    ////    if (id != owner)  //if not the intended target, ignore
    ////        yield return null;

    ////    isRewinding = true;

    ////    List<Vector3> frames = new List<Vector3>(recorder.RecordedPositions);

    ////    if (frames.Count < 2)
    ////    {
    ////        isRewinding = false;
    ////        yield break;
    ////    }

    ////    float totalFrames = frames.Count;
    ////    float timePerFrame = (rewindDuration / totalFrames);

    ////    // Move backwards through the recorded frames
    ////    for (int i = frames.Count - 1; i >= 0; i--)
    ////    {
    ////        Vector3 start = transform.position;
    ////        Vector3 end = frames[i];

    ////        float t = 0f;

    ////        // Smooth lerp between current pos and the rewind frame
    ////        while (t < 1f)
    ////        {
    ////            transform.position = Vector3.Lerp(start, end, t);
    ////            t += (Time.deltaTime / timePerFrame);
    ////            Debug.Log(t);
    ////            yield return null;
    ////        }

    ////        transform.position = end;
    ////    }

    ////    isRewinding = false;
    ////}
    ///

    IEnumerator ActivateRewind(PlayerID id)
    {
        if (id != owner)  //if not the intended target, ignore
            yield return null;

        var frames = new List<Vector3>(recorder.RecordedPositions);
        if (frames == null || frames.Count < 2)
            yield break;

        isRewinding = true;

        // Effective duration is shorter when rewindSpeed > 1
        float effectiveDuration = Mathf.Max(0.0001f, rewindTime / Mathf.Max(0.0001f, rewindSpeed));
        float elapsed = 0f;

        // We sample the recorded path by normalized t (0..1).
        // normalized = 0 -> earliest recorded frame; normalized = 1 -> latest (current)
        // We want to move from current -> earlier frames, therefore sample at (1 - normalized).
        while (elapsed < effectiveDuration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / effectiveDuration); // 0 -> 1
            float sample = 1f - normalized; // 1 -> 0

            // Get world position on the recorded path at 'sample'
            Vector3 pos = SamplePositionOnPath(frames, sample);

            transform.position = pos;
            yield return null;
        }

        // Ensure final position is the earliest recorded position
        transform.position = frames[0];

        isRewinding = false;
    }

    // Sample position along the recorded list (0..1)
    // 0 -> frames[0], 1 -> frames[last]
    Vector3 SamplePositionOnPath(List<Vector3> frames, float normalized)
    {
        if (frames.Count == 0)
            return transform.position;

        float fIndex = normalized * (frames.Count - 1);
        int i0 = Mathf.FloorToInt(fIndex);
        int i1 = Mathf.Min(i0 + 1, frames.Count - 1);
        float frac = fIndex - i0;

        // Linear interpolation between two nearest recorded frames
        return Vector3.Lerp(frames[i0], frames[i1], frac);
    }
}
