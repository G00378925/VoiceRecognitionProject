using System.Collections.Generic;
using UnityEngine;

public class Rod : MonoBehaviour
{
    // Position of screen bottom relative to the Rod
    private const float SCREEN_BOTTOM = -2.75f;

    // Rod instance variables
    private Stack<Disk> _diskStack = new Stack<Disk>();
    private int _rodIndex;

    // Getters and Setters for the variables above
    public int GetDiskCount() {
        return this._diskStack.Count;
    }

    public Stack<Disk> GetDiskStack() { // Get the stack for the Rod
        return this._diskStack;
    }

    public int GetIndex() {
        return this._rodIndex;
    }

    public float GetRodXPosition() {
        // Calculate the Rod x position, this is dynamic allowing resize of game window
        float xPosition = Screen.width / (GameController.GetInstance().GetRodCount() + 1);
        xPosition *= this._rodIndex + 1;
        return xPosition;
    }

    public void SetColour(Color colour) { // Assign the colour
        this.GetComponent<Renderer>().material.color = colour;
    }

    public void SetIndex(int index) {
        this._rodIndex = index;
    }

    public void AddDisk(Disk disk) {
        this._diskStack.Push(disk); // Add a new Disk
    }

    public void MoveTo(Rod rod, bool silenceError = false) {
        GameController gc = GameController.GetInstance();
        if (this._diskStack.Count == 0) return; // Can't move from empty Rod

        if (rod.GetDiskCount() > 0) {
            int bottomSize = rod.GetDiskStack().Peek().GetSize();
            int topSize = this._diskStack.Peek().GetSize();

            // Enforce rules of the game
            if (topSize < bottomSize) {
                if (!silenceError)
                    gc.WriteOutputText("Cannot move a larger disk onto a smaller disk!", Color.red);
                return;
            }
        }

        Disk disk = this._diskStack.Pop(); // Pop from other rod
        disk.SetIndex(rod.GetDiskCount()); // Set it to next index
        disk.SetRod(rod); // Assign its Rod
        rod.AddDisk(disk); // Add disk to Rod obj
    }

    // Update is called once per frame
    void Update()
    {
        GameController gc = GameController.GetInstance(); // Fetch singleton

        float rodYPosition = Screen.height / 2; // Put Rod at half of screen height
        // Position Rod relative to screen dimensions and screen bottom constant
        transform.position = Camera.main.ScreenToWorldPoint(new Vector3(GetRodXPosition(), rodYPosition, 10));
        transform.position += new Vector3(0, SCREEN_BOTTOM, 0);
        transform.position = new Vector3(transform.position.x, transform.position.y, 0);
    }
}
