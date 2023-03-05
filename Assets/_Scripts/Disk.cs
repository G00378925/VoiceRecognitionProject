using UnityEngine;

public class Disk : MonoBehaviour
{
    // Position of screen bottom, relative to the Disk
    private const float SCREEN_BOTTOM = -4.60f;

    // Disk instance variables
    private Color _colour;
    private int _diskIndex, _diskSize;
    private Rod _rod;

    // Getters and Setters for the variables above
    public Color GetColor()
    {
        return this._colour;
    }

    public int GetSize()
    {
        return this._diskSize;
    }

    public void SetColour(Color colour)
    {
        // Sets the rods material colour
        this.GetComponent<Renderer>().material.color = colour;
        this._colour = colour;
    }

    public void SetIndex(int index)
    {
        this._diskIndex = index;
    }

    public void SetRod(Rod rod)
    {
        this._rod = rod;
    }

    public void SetSize(int size)
    {
        this._diskSize = size;
    }

    // Called once per a frame to draw the Disk
    void Update()
    {
        // Fetch the GameController singleton
        GameController gc = GameController.GetInstance();
        float distanceBetweenRods = gc.GetDistanceBetweenRods() / 2, diskCount = gc.GetDiskCount();

        // Calculate the size of Disk
        Vector3 diskScale = transform.localScale;
        // Calculating the width using the diskSize
        diskScale.x = (distanceBetweenRods * 0.6f) * (1 - (this._diskSize / diskCount));
        // If there is more disks then, the Disks can be bigger in height
        diskScale.y = _rod.transform.localScale.y / gc.GetDiskCount();
        transform.localScale = diskScale;

        // Calculating the position of the Disk
        float diskYPosition = Screen.height / 2;
        // Calculate where to place the Disk using the current screen size
        transform.position = Camera.main.ScreenToWorldPoint(new Vector3(_rod.GetRodXPosition(), diskYPosition, 10));
        // Add the SCREEN_BOTTOM and position in Rod deltas
        transform.position += new Vector3(0, SCREEN_BOTTOM, 0);
        transform.position += new Vector3(0, diskScale.y * this._diskIndex, 0);
    }
}
