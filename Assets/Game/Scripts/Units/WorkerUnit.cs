using UnityEngine;

public class WorkerUnit : HumanoidUnit
{
    protected override void UpdateBehaviour()
    {
        CheckForCloseObjects();
    }
    private void CheckForCloseObjects()
    {
        var hits = RunProximityObjectDetection();
        foreach (var hit in hits)
        {
            Debug.Log("Detected object: " + hit.gameObject.name);
        }
    }
}