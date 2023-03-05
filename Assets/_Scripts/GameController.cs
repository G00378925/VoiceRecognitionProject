using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;
using System.IO;

public class GameController : MonoBehaviour
{
    // Adjustable game settings
    private const int DISK_COUNT = 8, INITIAL_ROD_COUNT = 3, MAX_ROD_COUNT = 8;
    private const string GRAMMAR_FILE_LOCATION = "Grammar.xml";

    private static GameController _instance; // Static variable that holds singleton

    // Search the list of colour strings, to find the corresponding colour object
    // See GetColourPosition below
    private static List<string> COLOUR_NAMES = new List<string>() {
        "RED", "GREY", "GREEN", "YELLOW",
        "CYAN", "BLACK", "MAGENTA", "WHITE"
    };
    private static List<Color> COLOURS = new List<Color>() {
        Color.red, Color.grey, Color.green, Color.yellow,
        Color.cyan, Color.black, Color.magenta, Color.white
    };

    [SerializeField] private GameObject _diskPrefab, _rodPrefab;

    private GrammarRecognizer _gr; // Grammar object
    private int _moveCount = 0; // Used to keep track of the users moves

    // Master lists of all the Rod and Disk objects in the game
    private List<Rod> _rodList = new List<Rod>();
    private List<Disk> _diskList = new List<Disk>();

    // Used to fetch the singleton for this class
    public static GameController GetInstance() {
        return _instance;
    }

    private int GetColourPosition(string colourName)
    {
        // Loop through colours and find a match, return index
        for (int i = 0; i < COLOUR_NAMES.Count; i++)
            if (COLOUR_NAMES[i] == colourName) return i;
        return 0;
    }

    public int GetDiskCount() {
        // You can't add and remove disks, so this will return the constant
        return DISK_COUNT;
    }

    public float GetDistanceBetweenRods() {
        // Used to calculate the amount of space between the rod objects
        return Camera.main.ScreenToWorldPoint(new Vector3(1 / (GetRodCount() + 1), 0, 0)).x;
    }

    public int GetRodCount() {
        return _rodList.Count;
    }

    private void AddDiskToRod(Rod rod, Disk disk, int diskIndex, int diskSize)
    {
        // Used during setup, adds each Disk to the Rod
        disk.SetIndex(diskIndex);
        disk.SetSize(diskSize); // Set the variables for the Disk

        disk.SetRod(rod);
        rod.AddDisk(disk);
    }

    private bool CheckIfValidRodIndex(string rodColour, int rodIndex)
    {
        // If a Rod hasn't been place yet, present an error
        if (rodIndex >= this.GetRodCount())
        {
            WriteOutputText(string.Format("The rod colour \"{0}\" doesn't exist!", rodColour), Color.red);
            return false;
        }
        return true;
    }

    private void CheckWinCondition()
    {
        if (this.GetRodCount() == 1) return; // No cheating allowed

        // If all disks on right, present yellow win message
        if (this._rodList[this._rodList.Count - 1].GetDiskStack().Count == DISK_COUNT)
            WriteOutputText("You won!", Color.yellow);
    }

    private void SetupGrammar()
    {
        // Setup the grammar obj
        _gr = new GrammarRecognizer(Path.Combine(Application.streamingAssetsPath, GRAMMAR_FILE_LOCATION), ConfidenceLevel.Low);
        _gr.OnPhraseRecognized += GR_OnPhraseRecognised;
        _gr.Start();

        if (_gr.IsRunning) Debug.Log("Grammar Recogniser running: " + _gr.GrammarFilePath);
    }

    private void SetupRods()
    {
        for (int i = 0; i < INITIAL_ROD_COUNT; i++)
            AddRod();

        // Create the disks
        for (int i = 0; i < DISK_COUNT; i++)
        {
            // Spawn disk objects, giving each one a name
            GameObject diskGO = Instantiate(_diskPrefab, Vector3.zero, Quaternion.identity);
            diskGO.name = "Disk" + i;
            Disk diskObj = diskGO.GetComponent<Disk>();
            diskObj.SetColour(COLOURS[i]); // Assign it a colour, depending on its index

            AddDiskToRod(_rodList[0], diskObj, i, i); // Add it to the Rod
            _diskList.Add(diskObj); // Add to master list above
        }
    }

    // Write text with colour
    public void WriteOutputText(string outputText, Color colour)
    {
        GameObject otObj = GameObject.Find("OutputText");
        otObj.GetComponent<Text>().text = outputText;
        otObj.GetComponent<Text>().color = colour;
    }

    // // // // // RULE HANDLERS BEGIN // // // // //
    private void AddRod()
    {
        if (GetRodCount() < MAX_ROD_COUNT)
        {
            // Spawn a new Rod
            GameObject rodGO = Instantiate(_rodPrefab, Vector3.zero, Quaternion.identity);
            rodGO.name = "Rod" + GetRodCount();

            // Set index and colour depending on its index
            Rod rodObj = rodGO.GetComponent<Rod>();
            rodObj.SetIndex(GetRodCount());
            rodObj.SetColour(COLOURS[GetRodCount()]);
            _rodList.Add(rodObj);
        }
        else
        {
            // More than 8 Rods already existing?, Error
            WriteOutputText("Cannot add more than " + MAX_ROD_COUNT + " rods!", Color.red);
        }
    }

    private void DestroyRod()
    {
        // Can't destory rods with disks
        if (GetRodCount() > 0 && _rodList[GetRodCount() - 1].GetDiskCount() == 0)
        {
            Destroy(_rodList[GetRodCount() - 1].gameObject);
            _rodList.Remove(_rodList[GetRodCount() - 1]);
        }
        else
        {
            // Present error
            WriteOutputText("Cannot remove rod with disks on it!", Color.red);
        }
    }

    private void MoveDiskByColour(string srcRodColour, string dstRodColour)
    {
        this._moveCount++; // Increment the move count
        int srcRodIndex = -1, dstRodIndex = GetColourPosition(dstRodColour);

        for (int i = 0; i < this.GetRodCount(); i++)
        { // Loop through all rods, try and find a colour match
            Rod rod = this._rodList[i];
            if (rod.GetDiskStack().Count == 0) continue;

            // If top disk matches the src colour
            Disk diskTopOfStack = rod.GetDiskStack().Peek();
            if (COLOURS[GetColourPosition(srcRodColour)] == diskTopOfStack.GetColor())
                srcRodIndex = i;
        }

        // If all is well, perform the move
        if (srcRodIndex != -1 && CheckIfValidRodIndex(dstRodColour, dstRodIndex))
            this._rodList[srcRodIndex].MoveTo(this._rodList[dstRodIndex]);
    }

    private void MoveFromRodToRod(string srcRodColour, string dstRodColour)
    {
        this._moveCount++; // Inc the move count
        // Get indexes of src and dst colours
        int srcRodIndex = GetColourPosition(srcRodColour), dstRodIndex = GetColourPosition(dstRodColour);

        // Check they are valid rod indexes, if alls well, perform the move
        if (CheckIfValidRodIndex(srcRodColour, srcRodIndex) && CheckIfValidRodIndex(dstRodColour, dstRodIndex))
            this._rodList[srcRodIndex].MoveTo(this._rodList[dstRodIndex]);
    }

    private void Shuffle()
    {
        for (int i = 0; i < 1_000; i++)
        { // 1000 iterations of mixing
            int randomRodSrcIndex = Random.Range(0, GetRodCount());
            int randomRodDestIndex = Random.Range(0, GetRodCount());

            // true parameter silences the errors
            _rodList[randomRodSrcIndex].MoveTo(_rodList[randomRodDestIndex], true);
        }
    }

    private void ShowMoveCount()
    {
        // Shows the current amount of moves
        WriteOutputText("Current move count is: " + this._moveCount, Color.green);
    }

    private void Reset() {
        // Resets the game to its original state
        this.WriteOutputText("Welcome to Tower of Hanoi", Color.green);

        // Destroy all the existing objects
        foreach (Rod rodObj in this._rodList) Destroy(rodObj.gameObject);
        foreach (Disk diskObj in this._diskList) Destroy(diskObj.gameObject);

        // Clear the master object lists
        this._rodList.Clear();
        this._diskList.Clear();

        this.SetupRods();

        // Reset move count
        this._moveCount = 0;
    }
    // // // // // RULE HANDLERS END // // // // //

    private void GR_OnPhraseRecognised(PhraseRecognizedEventArgs args)
    {
        // Key/Value semantic pairs go into a dict, makes it more elegant
        Dictionary<string, string> semanticMeanings = new Dictionary<string, string>();
        for (int i = 0; i < args.semanticMeanings.Length; i++)
            semanticMeanings.Add(args.semanticMeanings[i].key, args.semanticMeanings[i].values[0]);

        // Display the recongnised phrase
        Debug.Log(args.text);

        // Handlers for each action, then calls the corresponding method
        switch (semanticMeanings["action"]) {
            case "ADD_ROD":
                this.AddRod();
                break;
            case "REMOVE_ROD":
                this.DestroyRod();
                break;
            case "MOVE_DISK_BY_COLOUR":
                this.MoveDiskByColour(semanticMeanings["fromColour"], semanticMeanings["toColour"]);
                this.CheckWinCondition();
                break;
            case "MOVE_FROM_ROD_TO_ROD":
                this.MoveFromRodToRod(semanticMeanings["fromColour"], semanticMeanings["toColour"]);
                this.CheckWinCondition();
                break;
            case "SHUFFLE":
                this.Shuffle();
                break;
            case "SHOW_MOVE_COUNT":
                this.ShowMoveCount();
                break;
            case "RESET":
                this.Reset();
                break;
        }
    }

    public void Start()
    {
        _instance = this; // Set the singleton instance

        this.SetupGrammar(); // Setup the grammar GR object
        this.Reset(); // Reset the game state
    }
}
