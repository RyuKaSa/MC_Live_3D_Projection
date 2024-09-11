using UnityEngine;

public class GridDetector : MonoBehaviour
{
    private bool isOccupied = false;  // Tracks the sensor's current state

    // When an object enters the trigger collider
    void OnTriggerEnter(Collider other)
    {
        if (!isOccupied)
        {
            isOccupied = true;
            // No need to call OnStateChanged; GridManager handles state checks
        }
    }

    // When an object exits the trigger collider
    void OnTriggerExit(Collider other)
    {
        if (isOccupied)
        {
            isOccupied = false;
            // No need to call OnStateChanged; GridManager handles state checks
        }
    }

    // Getter to check the current state
    public bool IsOccupied()
    {
        return isOccupied;
    }
}
