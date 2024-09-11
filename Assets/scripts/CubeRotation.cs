using UnityEngine;

public class CubeRotation : MonoBehaviour
{
    public GameObject rotatingCubePrefab;  // The cube prefab
    public Vector3 cubePosition = new Vector3(0, 0, 0);  // Manually set the cube position
    public float cubeSize = 5.0f;          // Scale of the cube (set to 5 as requested)
    public float rotationSpeed = 30.0f;    // Speed of rotation (degrees per second)

    private GameObject rotatingCube;       // Reference to the rotating cube instance
    private Vector3 diagonalAxis;          // Diagonal axis for the rotation

    void Start()
    {
        // Instantiate the cube at the manually set position with an initial 45-degree rotation on X, Y, and Z axes
        rotatingCube = Instantiate(rotatingCubePrefab, cubePosition, Quaternion.Euler(45, 45, 45), transform);

        // Scale the cube
        rotatingCube.transform.localScale = new Vector3(cubeSize, cubeSize, cubeSize);

        // Define the diagonal axis for rotation (this is the axis going through two opposite corners of the cube)
        diagonalAxis = new Vector3(1, 1, 1).normalized;  // Diagonal in the XYZ space
    }

    void Update()
    {
        // Rotate the cube around the diagonal axis
        rotatingCube.transform.RotateAround(rotatingCube.transform.position, diagonalAxis, rotationSpeed * Time.deltaTime);
    }
}
