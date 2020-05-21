using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using TMPro;
using Unity.MLAgents;


public enum Player {
    None,
    Wolf,
    Sheep
}

public enum Controller {
    None,
    Human,
    AI
}

public class SingleGameManager : MonoBehaviour {
    private Player turn = Player.Sheep;
    private StatsRecorder statsRecorder;
    private bool decisionRequested = false;

    public Player Turn { get => turn; set => turn = value; }

    public Agent wolfAgent;
    public Agent sheepAgent;


    public SheepController sheepNext;
    public SquareController sheepNextMove;
    public SquareController wolfNextMove;

    public TMP_Text wolfGamesWonText;
    public TMP_Text sheepGamesWonText;
    public TMP_Text tieText;

    public Player winner;
    public int wolfWon;
    public int sheepWon;
    public int tie;

    public SheepController[] sheep;
    public WolfController wolf;
    public SquareController[,] squares;

    public GameObject statsOverlay;
    public float minRowCol = 8; // minimum number of rows or columns
    public float maxRowCol = 8;

    public Transform gameBoard;
    public Transform squarePrefab;
    public TMP_Text whoseTurnText;
    public TMP_Text winningText;

    public bool turnDone = false;

    //public float animationDelay = 0.1f;

    [Header("Game Settings")]
    [Tooltip("Number of rows")]
    public int rows = 8;
    [Tooltip("Number of columns")]
    public int columns = 8;

    public void Awake() {
        Academy.Instance.OnEnvironmentReset += ResetGame;
        statsRecorder = Academy.Instance.StatsRecorder;
        OneTimeSetup();
    }

    public int wolfSteps;
    public int sheepSteps;


    // Start is called before the first frame update
    void Start() {


        //ResetGame();
        //Academy.Instance.EnvironmentStep();
    }

    void Update() {

        //if (!((Time.frameCount % 100) == 0)) { return; };

        if (HaveWinner()) {
            winningText.SetText($"The winner is: {winner.ToString()}");
            winningText.enabled = true;
            Debug.Log("  [update] Resetting game...");
            ResetGame();
            return;
        }


        if (!turnDone) {
            switch (Turn) {
                case Player.Sheep:
                    if (!decisionRequested) {
                        // Collects observations and gets an action
                        // this sets sheepNext and sheepNextMove
                        sheepAgent.RequestDecision();
                        decisionRequested = true;

                        sheepSteps++;

                        // Add penalty per step to encourage the seep to capture the wolf
                        float perStepSheepReward = -0.003f;
                        sheepAgent.AddReward(perStepSheepReward);
                    }
                    if (sheepNext && sheepNextMove) {
                        //Select(sheepNext);
                        HightlightNextPossibleMove(sheepNext);
                        sheepNext.GetComponent<SheepController>().HighLight();

                        if (SheepMovePossible(sheepNext, sheepNextMove)) {
                            SheepMoveTo(sheepNext, sheepNextMove);
                            sheepNextMove = null;
                            sheepNext = null;
                            turn = Player.Wolf;
                            //UnSelect();
                            decisionRequested = false;
                        }
                    }
                    turnDone = true;
                    break;

                case Player.Wolf:
                    if (!decisionRequested) {
                        //Select(wolf);
                        HightlightNextPossibleMove(wolf);
                        wolf.GetComponent<WolfController>().HighLight();

                        // Collects observations and gets an action
                        wolfAgent.RequestDecision();
                        decisionRequested = true;
                        wolfSteps++;

                        // Add penalty per step to encourage the wolf to get to the end
                        float perStepWolfReward = -0.003f;
                        wolfAgent.AddReward(perStepWolfReward);
                    }

                    if (wolfNextMove) {
                        if (WolfMovePossible(wolfNextMove)) {
                            WolfMoveTo(wolfNextMove);
                            wolfNextMove = null;
                            turn = Player.Sheep;
                            //UnSelect();
                            decisionRequested = false;
                        }
                    }

                    turnDone = true;
                    break;

                default:
                    throw new Exception(String.Format("Unknown state: {0}", Turn));
            }
        }



        if (statsOverlay.activeSelf) {
            whoseTurnText.SetText(turn.ToString());
            wolfGamesWonText.SetText(wolfWon.ToString());
            sheepGamesWonText.SetText(sheepWon.ToString());
            tieText.SetText(tie.ToString());

        }
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
        sheepNext = null;
        sheepNextMove = null;
        wolfNextMove = null;
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
        squares = new SquareController[columns, rows];
        Color clr = Color.red;

        for (int c = 0; c < columns; c++) {
            for (int r = 0; r < rows; r++) {
                GameObject sq = Instantiate(squarePrefab, SquareCenter(c, r), Quaternion.identity, gameBoard).gameObject;
                squares[c, r] = sq.GetComponent<SquareController>();
                squares[c, r].transform.localPosition = SquareCenter(c, r);
                SquareController squareC = squares[c, r].GetComponent<SquareController>();
                squareC.gameManager = this;

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
        squares[startingCol, startingRow].GetComponent<SquareController>().Occupy(wolf.gameObject);
    }

    private void setSheepStartingPositions() {
        int[] possibleCols = new int[] { 0, 2, 4, 6 };

        int startingRow = 0;

        for (int i = 0; i < sheep.Length; i++) {
            sheep[i].transform.SetParent(squares[possibleCols[i], startingRow].transform);
            sheep[i].transform.localPosition = new Vector3(0, 0.75f, 0);
            sheep[i].GetComponent<SheepController>().SetSquare(squares[possibleCols[i], startingRow]);
            squares[possibleCols[i], startingRow].GetComponent<SquareController>().Occupy(sheep[i].gameObject);
        }
    }

    public void Select(GameObject obj) {
        //selectedObject = obj;
    }

    public void UnSelect() {
        //selectedObject = null;
    }

    public void HightlightNextPossibleMove(GameObjectBase obj) {
        if (!obj) { return; };

        RemoveSquareHighlights();

        SquareController currentSquare = obj.Square();
        List<SquareController> possibleMoves = currentSquare.PossibleMoves();

        foreach (var sq in possibleMoves) {
            sq.GetComponent<SquareController>().HighLight();
        }

    }
    public void SheepMoveTo(SheepController selectedObject, SquareController to) {
        SquareController currentSquare = selectedObject.Square();
        SquareController squareController = currentSquare.GetComponent<SquareController>();
        SquareController toController = to.GetComponent<SquareController>();

        if (SheepMovePossible(selectedObject, to)) {
            selectedObject.transform.SetParent(to.transform);
            selectedObject.transform.localPosition = new Vector3(0, 0.75f, 0);
            squareController.Empty();
            toController.Occupy(selectedObject.gameObject);
            selectedObject.GetComponent<SheepController>().SetSquare(to);

            RemoveSquareHighlights();
            RemoveObjectHighlights();
        }
    }

    public bool SheepMovePossible(SheepController selectedObject, SquareController to) {
        if (!selectedObject) { return false; };

        SquareController currentSquare = selectedObject.Square();
        SquareController squareController = currentSquare.GetComponent<SquareController>();

        List<SquareController> possibleMoves = squareController.PossibleSheepMoves();

        if (possibleMoves.Contains(to)) {
            return true;

        }
        return false;

    }

    public void WolfMoveTo(SquareController to) {

        SquareController squareController = wolf.GetComponent<WolfController>().Square();
        WolfController wolfController = wolf.GetComponent<WolfController>();

        if (WolfMovePossible(to)) {
            wolf.transform.SetParent(to.transform);
            wolf.transform.localPosition = new Vector3(0, 0.75f, 0);
            squareController.Empty();
            to.Occupy(wolf.gameObject);
            wolfController.SetSquare(to);

            RemoveSquareHighlights();
            RemoveObjectHighlights();
        }
    }

    public bool WolfMovePossible(SquareController to) {

        SquareController currentSquare = wolf.GetComponent<WolfController>().Square();
        SquareController squareController = currentSquare.GetComponent<SquareController>();

        List<SquareController> possibleMoves = squareController.PossibleWolfMoves();

        if (possibleMoves.Contains(to)) {
            return true;

        }
        return false;
    }

    public bool HaveWinner() {
        //Debug.Log("Checking winner...");
        WolfController wolfController = wolf.GetComponent<WolfController>();

        // Wolf made it to the end
        if (wolfController.Square().GetComponent<SquareController>().row == 0) {
            Debug.Log(">>>>>>>>>>>>>>>>>>>>>>>>>>  [manager] Wolf won!");
            winner = Player.Wolf;

            wolfAgent.AddReward(1.0f);
            wolfAgent.EndEpisode();

            sheepAgent.AddReward(-1.0f);
            sheepAgent.EndEpisode();

            wolfWon++;

            return true;
        }

        // Wolf can't move
        List<SquareController> possibleMoves = wolfController.Square().GetComponent<SquareController>().PossibleWolfMoves();
        if (possibleMoves.Count == 0) {
            Debug.Log(">>>>>>>>>>>>>>>>>>>>>>>>>>  [manager] Sheep won!");
            winner = Player.Sheep;

            wolfAgent.AddReward(-1.0f);
            wolfAgent.EndEpisode();

            sheepAgent.AddReward(1.0f);
            sheepAgent.EndEpisode();

            sheepWon++;

            return true;
        }

        // sheep can't move, but wolf hasn't made it to the end yet
        // this happens because the wolf can move randomly
        // treat this as a tie
        if (!SheepCanMove()) {
            Debug.Log(">>>>>>>>>>>>>>>>>>>>>>>>>>  [manager] Sheep won (by default)!");
            winner = Player.None;

            wolfAgent.SetReward(0.0f);
            wolfAgent.EndEpisode();

            sheepAgent.SetReward(0.0f);
            sheepAgent.EndEpisode();

            tie++;

            return true;
        }

        return false;
    }

    public bool SheepCanMove() {
        List<SquareController> perSheepAllowedMoves = new List<SquareController>();

        for (int i = 0; i < sheep.Length; i++) {
            // grab the sheep controller
            SheepController sheepController = sheep[i].GetComponent<SheepController>();
            SquareController square = sheepController.Square().GetComponent<SquareController>();

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

