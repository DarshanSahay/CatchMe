using UnityEngine;
using System.Collections.Generic;

public class MovementRecorder : MonoBehaviour
{
    [Header("Recording Settings")]
    public float recordDuration = 1.5f;
    List<Vector3> deltas = new List<Vector3>();
    Vector3 lastPosition;
    public IReadOnlyList<Vector3> MovementDeltas => deltas;

    public float recordTime = 2.0f; // T seconds
    [SerializeField] private List<Vector3> positions = new List<Vector3>();
    public IReadOnlyList<Vector3> RecordedPositions => positions;


    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        Vector3 currentPos = transform.position;
        Vector3 delta = currentPos - lastPosition;

        deltas.Add(delta);
        lastPosition = currentPos;

        int maxFrames = Mathf.RoundToInt(recordDuration / Time.deltaTime);

        if (deltas.Count > maxFrames)
        {
            deltas.RemoveAt(0);
        }
            
        positions.Add(transform.position);

        int totalFrames = Mathf.RoundToInt(recordTime / Time.deltaTime);

        if (positions.Count > totalFrames)
        {
            positions.RemoveAt(0);
        }
    }
}