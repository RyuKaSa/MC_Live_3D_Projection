using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using WebSocketSharp;

public class GridManager : MonoBehaviour
{
    public GameObject detectorPrefab;  // The sensor prefab
    public int gridSize = 10;          // 10x10x10 grid (adjustable)
    public float cubeSize = 1.0f;      // Size of each imagined cube (1x1x1 by default)
    public float checkInterval = 1.0f; // Interval to check sensor states (in seconds)

    private GameObject[,,] grid;       // Array to hold the grid of detectors
    private HashSet<Vector3> previousOccupiedSensors = new HashSet<Vector3>(); // To track previously occupied sensors
    private HashSet<Vector3> currentOccupiedSensors = new HashSet<Vector3>();  // To track currently occupied sensors
    private WebSocket ws;              // WebSocket for communication with the Minecraft server
    private StringBuilder outlineCommands;  // To store commands for creating the outline
    private StringBuilder undoOutlineCommands;  // To store commands for removing the outline

    void Start()
    {
        CreateGrid();  // Initialize grid creation on start
        ConnectToWebSocket();  // Connect WebSocket once at the start
        ClearGrid();  // Clear the grid by setting all positions to air
        CreateGridOutline();  // Create the outline in Minecraft with sea lanterns
        SendCommand(outlineCommands.ToString());  // Send the outline creation commands
        SendCommand("Command /say Starting loop in afevoid dimension");  // Send a debug message to indicate the loop is starting in afevoid
        StartCoroutine(CheckSensorStates());  // Start checking sensor states every second
    }

    // Connect to the WebSocket server (Minecraft server with WebSocket integration)
    void ConnectToWebSocket()
    {
        ws = new WebSocket("ws://localhost:8887");  // WebSocket server address
        ws.OnOpen += (sender, e) => Debug.Log("WebSocket connected!");
        ws.OnMessage += (sender, e) => Debug.Log("Message from server: " + e.Data);
        ws.OnError += (sender, e) => Debug.LogError("WebSocket error: " + e.Message);
        ws.OnClose += (sender, e) => Debug.Log("WebSocket closed with reason: " + e.Reason);

        ws.Connect();
    }

    // Function to send a command via WebSocket
    void SendCommand(string command)
    {
        if (ws != null && ws.IsAlive)
        {
            ws.Send(command);
            Debug.Log($"Sent: {command}");
        }
        else
        {
            Debug.LogError("WebSocket is not connected.");
        }
    }

    void ClearGrid()
    {
        StringBuilder commandBuilder = new StringBuilder();
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    int worldY = Mathf.RoundToInt(y) + 70;  // Add 70 to Y-coordinate
                    // Replace /setblock with /execute in afevoid run setblock
                    commandBuilder.Append($"Command /execute in afevoid run setblock {x} {worldY} {z} minecraft:air\n");
                }
            }
        }
        SendCommand(commandBuilder.ToString());
    }

    // Coroutine to check sensor states every second
    IEnumerator CheckSensorStates()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);

            currentOccupiedSensors.Clear();
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    for (int z = 0; z < gridSize; z++)
                    {
                        GridDetector detector = grid[x, y, z].GetComponent<GridDetector>();
                        if (detector.IsOccupied())
                        {
                            Vector3 sensorPosition = new Vector3(x, y, z);
                            currentOccupiedSensors.Add(sensorPosition);
                        }
                    }
                }
            }

            // Compare previousOccupiedSensors and currentOccupiedSensors to find changes
            HashSet<Vector3> newlyOccupied = new HashSet<Vector3>(currentOccupiedSensors);
            newlyOccupied.ExceptWith(previousOccupiedSensors);

            HashSet<Vector3> newlyUnoccupied = new HashSet<Vector3>(previousOccupiedSensors);
            newlyUnoccupied.ExceptWith(currentOccupiedSensors);

            // Build a single string for WebSocket packet
            StringBuilder commandBuilder = new StringBuilder();
            string firstCommand = null;

            // Add newly occupied commands (minecraft:green_wool)
            if (newlyOccupied.Count > 0)
            {
                foreach (Vector3 pos in newlyOccupied)
                {
                    int x = Mathf.RoundToInt(pos.x);
                    int y = Mathf.RoundToInt(pos.y) + 70;  // Add 70 to the Y-coordinate
                    int z = Mathf.RoundToInt(pos.z);
                    // Use /execute in afevoid run setblock
                    string command = $"Command /execute in afevoid run setblock {x} {y} {z} minecraft:green_wool\n";
                    if (firstCommand == null) firstCommand = command;
                    commandBuilder.Append(command);
                }
            }

            // Add newly unoccupied commands (minecraft:air)
            if (newlyUnoccupied.Count > 0)
            {
                foreach (Vector3 pos in newlyUnoccupied)
                {
                    int x = Mathf.RoundToInt(pos.x);
                    int y = Mathf.RoundToInt(pos.y) + 70;  // Add 70 to the Y-coordinate
                    int z = Mathf.RoundToInt(pos.z);
                    string command = $"Command /execute in afevoid run setblock {x} {y} {z} minecraft:air\n";
                    if (firstCommand == null) firstCommand = command;
                    commandBuilder.Append(command);
                }
            }

            // Append the first command to the end
            if (!string.IsNullOrEmpty(firstCommand))
            {
                commandBuilder.Append(firstCommand);
            }

            // If there are commands, send them via WebSocket
            if (commandBuilder.Length > 0)
            {
                SendCommand(commandBuilder.ToString());
            }

            // Update the previousOccupiedSensors for the next check
            previousOccupiedSensors = new HashSet<Vector3>(currentOccupiedSensors);
        }
    }

    // Function to create the grid of detectors
    void CreateGrid()
    {
        grid = new GameObject[gridSize, gridSize, gridSize];

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    // Calculate the position for each sensor in the center of the imaginary grid cube
                    float posX = (x * cubeSize) + (cubeSize / 2.0f);
                    float posY = (y * cubeSize) + (cubeSize / 2.0f);
                    float posZ = (z * cubeSize) + (cubeSize / 2.0f);

                    Vector3 position = new Vector3(posX, posY, posZ);

                    // Instantiate the sensor at the calculated position
                    GameObject detector = Instantiate(detectorPrefab, position, Quaternion.identity, transform);

                    // Optionally rename for debugging
                    detector.name = $"Detector ({x}, {y}, {z})";

                    // Store the detector in the grid array
                    grid[x, y, z] = detector;
                }
            }
        }
    }

    // Function to create a vertex-only outline around the grid in Minecraft
    void CreateGridOutline()
    {
        outlineCommands = new StringBuilder();
        undoOutlineCommands = new StringBuilder();

        // Define the corners of the grid
        Vector3[] vertices = {
            new Vector3(-1, -1, -1),
            new Vector3(-1, -1, gridSize),
            new Vector3(-1, gridSize, -1),
            new Vector3(-1, gridSize, gridSize),
            new Vector3(gridSize, -1, -1),
            new Vector3(gridSize, -1, gridSize),
            new Vector3(gridSize, gridSize, -1),
            new Vector3(gridSize, gridSize, gridSize)
        };

        // Loop through the vertices and create sea lanterns
        foreach (Vector3 vertex in vertices)
        {
            int worldX = Mathf.RoundToInt(vertex.x);
            int worldY = Mathf.RoundToInt(vertex.y) + 70;  // Add 70 to Y-coordinate
            int worldZ = Mathf.RoundToInt(vertex.z);

            // Create the sea lantern at the vertex using /execute in afevoid run
            outlineCommands.Append($"Command /execute in afevoid run setblock {worldX} {worldY} {worldZ} minecraft:sea_lantern\n");

            // Create the undo command to remove the sea lantern
            undoOutlineCommands.Append($"Command /execute in afevoid run setblock {worldX} {worldY} {worldZ} minecraft:air\n");
        }
    }

    // Coroutine to destroy the grid outline in Minecraft
    IEnumerator DestroyGridOutline()
    {
        SendCommand(undoOutlineCommands.ToString());  // Send the undo commands to remove the outline
        yield return null;
    }

    // On destroy, ensure WebSocket is closed and the grid outline is removed
    void OnDestroy()
    {
        if (ws != null && ws.IsAlive)
        {
            SendCommand(undoOutlineCommands.ToString());  // Remove the outline when the Unity script is stopped
            ws.Close();
        }
    }

    // Draw Gizmos to visualize the grid edges when meshes are disabled
    void OnDrawGizmos()
    {
        // Draw a wireframe cube around the grid area
        Gizmos.color = Color.green;  // Set outline color to green

        // Define the center and size of the wireframe box
        Vector3 gridCenter = new Vector3(gridSize * cubeSize / 2.0f, gridSize * cubeSize / 2.0f, gridSize * cubeSize / 2.0f);
        Vector3 gridSizeVector = new Vector3(gridSize * cubeSize, gridSize * cubeSize, gridSize * cubeSize);

        // Draw the wireframe box
        Gizmos.DrawWireCube(transform.position + gridCenter, gridSizeVector);
    }
}
