using PurrNet;
using System;
using System.Collections.Generic;
using UnityEngine;

public class CloneMovement : NetworkBehaviour
{
    private List<Vector3> deltas;
    private int index = 0;

    public float smoothness = 0.2f;
    public float rotationSmoothness = 0.2f;

    public void InitReplay(List<Vector3> movementDeltas)
    {
        deltas = new List<Vector3>(movementDeltas);
    }

    void Update()
    {
        if (deltas == null)
            return;

        if (index >= deltas.Count)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPos = transform.position + deltas[index];

        transform.position = Vector3.Lerp(transform.position, targetPos, smoothness);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(deltas[index]), rotationSmoothness);
        index++;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Chaser"))
        {
            
        }
    }
}
