using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using TMPro;
using Unity.MLAgents;
using System.Collections;


public enum Player {
    None,
    Wolf,
    Sheep
}

public class GameManager : Singleton<GameManager> {
    public GameObject selectedObject;
    private Player turn = Player.Sheep;
    public Player Turn { get => turn; set => turn = value; }

    public Agent wolfAgent;
    public Agent sheepAgent;

    public Player winner;
    public GameObject wolfNextMove;
    public GameObject sheepNext;
    public GameObject sheepNextMove;

    public TMP_Text wolfGamesWon;
    public TMP_Text sheepGamesWon;

    public int wolfWon;
    public int sheepWon;

    public GameObject[] sheep;
    public GameObject wolf;
    public GameObject[,] squares;
    public int minRowCol = 8; // minimum number of rows or columns
    public int maxRowCol = 8;

    public Transform gameBoard;
    public Transform squarePrefab;
    public TMP_Text whoseTurnText;
    public TMP_Text winningText;

    public float animationDelay = 0.1f;

    [Header("Game Settings")]
    [Tooltip("Number of rows")]
    public int rows = 8;
    [Tooltip("Number of columns")]
    public int columns = 8;

    protected override void Awake() {
        base.Awake();

        //Academy.Instance.OnEnvironmentReset += ResetGame;
    }

    // Start is called before the first frame update
    void Start() {

        OneTimeSetup();

        ResetGame();
        Academy.Instance.EnvironmentStep();

        // calculates the wolf's move when it's the wolf's turn
        //StartCoroutine(CalculateWolfMove());
        //StartCoroutine(CalculateSheepMove());
    }

    void Update() {

        switch (Turn) {
            case Player.Sheep:
                if (!sheepNext || !sheepNextMove) {
                    // Collects observations and gets an action
                    // this sets sheepNext and sheepNextMove
                    sheepAgent.RequestDecision();
                    // execute decision
                    Academy.Instance.EnvironmentStep();
                } else {
                    Select(sheepNext);
                    HightlightNextPossibleMove(sheepNext);
                    sheepNext.GetComponent<SheepController>().HighLight();

                    if (SheepMovePossible(sheepNext, sheepNextMove)) {
                        SheepMoveTo(sheepNext, sheepNextMove);
                        sheepNextMove = null;
                        sheepNext = null;
                        turn = Player.Wolf;
                        UnSelect();
                    }
                }
                break;

            case Player.Wolf:
                Select(wolf);
                HightlightNextPossibleMove(wolf);

                wolf.GetComponent<WolfController>().HighLight();

                // Collects observations and gets an action
                wolfAgent.RequestDecision();
                // execute decision
                Academy.Instance.EnvironmentStep();

                if (WolfMovePossible(wolfNextMove)) {
                    WolfMoveTo(wolfNextMove);
                    wolfNextMove = null;
                    turn = Player.Sheep;
                    UnSelect();
                } else {
                    // wolf can't move

                }
                break;

            default:
                throw new Exception(String.Format("Unknown state: {0}", Turn));

        }


        if (HaveWinner()) {
            winningText.SetText($"The winner is: {winner.ToString()}");
            winningText.enabled = true;
            ResetGame();
        }

        whoseTurnText.SetText(turn.ToString());
        wolfGamesWon.SetText(wolfWon.ToString());
        sheepGamesWon.SetText(sheepWon.ToString());
    }



    public void OneTimeSetup() {

        SetupSquares();
        winningText.enabled = false;
    }

    public void ResetGame() {
        Debug.Log("Resetting environment...");

        ResetSquares();
        setWolfStartingPosition();
        setSheepStartingPositions();
        winningText.enabled = false;
        winner = Player.None;
        Turn = Player.Sheep;
    }

    // Reset squares after episode
    private void ResetSquares() {

        Debug.Log("Resetting squares...");
        for (int c = 0; c < columns; c++) {
            for (int r = 0; r < rows; r++) {
                SquareController squareC = squares[c, r].GetComponent<SquareController>();
                squareC.Empty();
            }
        }
    }

    private void SetupSquares() {
        squares = new GameObject[columns, rows];
        Color clr = Color.red;

        for (int c = 0; c < columns; c++) {
            for (int r = 0; r < rows; r++) {
                squares[c, r] = Instantiate(squarePrefab, SquareCenter(c, r), Quaternion.identity, gameBoard).gameObject;
                SquareController squareC = squares[c, r].GetComponent<SquareController>();

                squareC.column = c;
                squareC.row = r;
                squareC.color = clr;
                squareC.Empty();

                clr = (clr == Color.red) ? Color.black : Color.red;

            }
            clr = (clr == Color.red) ? Color.black : Color.red;
        }
    }

    // Returns the cener of the square given the row and col
    private Vector3 SquareCenter(int c, int r) {
        return new Vector3(c + 0.5f, 0, r + 0.5f);
    }

    private void setWolfStartingPosition() {
        int[] possibleCols = new int[] { 1, 3, 5, 7 };
        int startingRow = 7;  // fixed
        int startingCol = possibleCols[Random.Range(0, possibleCols.Length)];

        wolf.transform.SetParent(squares[startingCol, startingRow].transform);
        wolf.transform.localPosition = new Vector3(0, 0.5f, 0);
        wolf.GetComponent<WolfController>().SetSquare(squares[startingCol, startingRow]);
        squares[startingCol, startingRow].GetComponent<SquareController>().Occupy(wolf);
    }

    private void setSheepStartingPositions() {
        int[] possibleCols = new int[] { 0, 2, 4, 6 };

        int startingRow = 0;

        for (int i = 0; i < sheep.Length; i++) {
            sheep[i].transform.SetParent(squares[possibleCols[i], startingRow].transform);
            sheep[i].transform.localPosition = new Vector3(0, 0.75f, 0);
            sheep[i].GetComponent<SheepController>().SetSquare(squares[possibleCols[i], startingRow]);
            squares[possibleCols[i], startingRow].GetComponent<SquareController>().Occupy(sheep[i]);
        }
    }

    public void Select(GameObject obj) {
        selectedObject = obj;
        //HightlightNextPossibleMove(selectedObject);
    }

    public void UnSelect() {
        selectedObject = null;
    }

    public void HightlightNextPossibleMove(GameObject obj) {
        if (!obj) { return; };

        RemoveSquareHighlights();

        GameObject currentSquare = obj.GetComponent<GameObjectBase>().Square();
        List<GameObject> possibleMoves = currentSquare.GetComponent<SquareController>().PossibleMoves();

        foreach (var sq in possibleMoves) {
            sq.GetComponent<SquareController>().HighLight();
        }

    }
    public void SheepMoveTo(GameObject selectedObject, GameObject to) {
        GameObject currentSquare = selectedObject.GetComponent<SheepController>().Square();
        SquareController squareController = currentSquare.GetComponent<SquareController>();
        SquareController toController = to.GetComponent<SquareController>();

        if (SheepMovePossible(selectedObject, to)) {
            selectedObject.transform.SetParent(to.transform);
            selectedObject.transform.localPosition = new Vector3(0, 0.75f, 0);
            squareController.Empty();
            toController.Occupy(selectedObject);
            selectedObject.GetComponent<SheepController>().SetSquare(to);

            RemoveSquareHighlights();
            RemoveObjectHighlights();

            Turn = Player.Wolf;
        } else {
            Debug.Log($"[sheep] {selectedObject.GetComponent<SheepController>()} not able to move to: {to.GetComponent<SquareController>()}");
            Application.Quit(1);
        }
    }

    public bool SheepMovePossible(GameObject selectedObject, GameObject to) {
        if (!selectedObject) { return false; };

        GameObject currentSquare = selectedObject.GetComponent<SheepController>().Square();
        SquareController squareController = currentSquare.GetComponent<SquareController>();

        List<GameObject> possibleMoves = squareController.PossibleSheepMoves();

        if (possibleMoves.Contains(to)) {
            return true;

        }
        return false;

    }

    public void WolfMoveTo(GameObject to) {

        GameObject currentSquare = wolf.GetComponent<WolfController>().Square();
        WolfController wolfController = wolf.GetComponent<WolfController>();
        SquareController squareController = currentSquare.GetComponent<SquareController>();
        SquareController toController = to.GetComponent<SquareController>();

        if (WolfMovePossible(to)) {
            wolf.transform.SetParent(to.transform);
            wolf.transform.localPosition = new Vector3(0, 0.75f, 0);
            squareController.Empty();
            toController.Occupy(wolf);
            wolfController.SetSquare(to);

            RemoveSquareHighlights();
            RemoveObjectHighlights();

            Turn = Player.Sheep;
        }

    }

    public bool WolfMovePossible(GameObject to) {

        GameObject currentSquare = wolf.GetComponent<WolfController>().Square();
        SquareController squareController = currentSquare.GetComponent<SquareController>();

        List<GameObject> possibleMoves = squareController.PossibleWolfMoves();

        if (possibleMoves.Contains(to)) {
            return true;

        }
        return false;
    }

    public bool HaveWinner() {
        Debug.Log("Checking winner...");
        WolfController wolfController = wolf.GetComponent<WolfController>();

        // Wolf made it to the end
        if (wolfController.Square().GetComponent<SquareController>().row == 0) {
            Debug.Log("[manager] Wolf won!");
            winner = Player.Wolf;

            wolfAgent.SetReward(1.0f);
            wolfAgent.EndEpisode();

            sheepAgent.SetReward(-1.0f);
            sheepAgent.EndEpisode();

            wolfWon++;

            return true;
        }

        // Wolf can't move
        Debug.Log($"[manager] wolf at {wolfController.Square().GetComponent<SquareController>()}");
        List<GameObject> possibleMoves = wolfController.Square().GetComponent<SquareController>().PossibleWolfMoves();
        Debug.Log($"[manager] wolf possible moves: {possibleMoves.Count}");
        if (possibleMoves.Count == 0) {
            Debug.Log("[manager] Sheep won!");
            winner = Player.Sheep;

            wolfAgent.SetReward(-1.0f);
            wolfAgent.EndEpisode();

            sheepAgent.SetReward(1.0f);
            sheepAgent.EndEpisode();

            sheepWon++;

            return true;
        }

        // sheep can't move, but wolf hasn't made it to the end yet
        // this happens because the wolf can move randomly
        if (!SheepCanMove()) {
            Debug.Log("[manager] Sheep won (by default)!");
            winner = Player.Wolf;

            wolfAgent.SetReward(1.0f);
            wolfAgent.EndEpisode();

            sheepAgent.SetReward(-1.0f);
            sheepAgent.EndEpisode();

            wolfWon++;

            return true;
        }

        return false;
    }

    public bool SheepCanMove() {
        List<GameObject> perSheepAllowedMoves = new List<GameObject>();

        for (int i = 0; i < GameManager.Instance.sheep.Length; i++) {
            // grab the sheep controller
            SheepController sheep = GameManager.Instance.sheep[i].GetComponent<SheepController>();
            SquareController square = sheep.Square().GetComponent<SquareController>();

            perSheepAllowedMoves = square.PossibleSheepMoves();
            if (perSheepAllowedMoves.Count > 0) {
                return true;
            }
        }
        return false;
    }

    public void RemoveSquareHighlights() {
        foreach (var sq in squares) {
            sq.GetComponent<SquareController>().HighLightRemove();

        }
    }

    public void RemoveObjectHighlights() {
        foreach (var obj in sheep) {
            obj.GetComponent<SheepController>().HighLightRemove();
        }
        wolf.GetComponent<WolfController>().HighLightRemove();
    }
}
