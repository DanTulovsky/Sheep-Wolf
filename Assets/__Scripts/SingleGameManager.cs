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
    private bool decisionRequested = false;
    private Player winner;
    private bool haveAI = false;

    [Header("Game Assets")]
    public SheepController[] sheep;
    [SerializeField] private WolfController wolf;
    [SerializeField] private Transform gameBoard;
    [SerializeField] private Transform squarePrefab;
    [SerializeField] private TMP_Text whoseTurnText;
    [SerializeField] private TMP_Text winningText;
    [SerializeField] private TMP_Text wolfGamesWonText;
    [SerializeField] private TMP_Text sheepGamesWonText;
    [SerializeField] private TMP_Text tieText;
    [SerializeField] private GameObject statsOverlay;

    [Header("AI Agents")]
    public Agent wolfAgent;
    public Agent sheepAgent;

    public int rows;
    public int columns;

    public Color redSquareColor = Color.red;
    public Color blackSquareColor = Color.black;
    public Color sheepColor = Color.white;
    public Color wolfColor = Color.black;

    [HideInInspector] public SquareController[,] squares;
    [HideInInspector] public Player Turn { get => turn; set => turn = value; }
    [HideInInspector] public SheepController sheepNext;
    [HideInInspector] public SquareController sheepNextMove;
    [HideInInspector] public SquareController wolfNextMove;

    [HideInInspector] public int wolfWon;
    [HideInInspector] public int sheepWon;
    [HideInInspector] public int tie;
    [HideInInspector] public bool turnDone = false;
    [HideInInspector] public GameObject selectedObject;


    public void Awake() {
        //Academy.Instance.OnEnvironmentReset += ResetGame;
        //statsRecorder = Academy.Instance.StatsRecorder;
        //OneTimeSetup();
    }

    public void Start() {
        haveAI = GameManager.Instance.haveAI;

        if (haveAI) {
            Academy.Instance.OnEnvironmentReset += ResetGame;
        }
        OneTimeSetup();
    }

    void Update() {

        //if (!((Time.frameCount % 100) == 0)) { return; };

        if (HaveWinner()) {
            winningText.SetText($"The winner is: {winner.ToString()}");
            winningText.enabled = true;
            ResetGame();
            return;
        }


        if (!turnDone) {
            switch (Turn) {
                case Player.Sheep:
                    if (GameManager.Instance.sheepAgentController == AgentController.AI && !decisionRequested) {
                        // Collects observations and gets an action
                        // this sets sheepNext and sheepNextMove
                        sheepAgent.RequestDecision();
                        decisionRequested = true;

                        // Add penalty per step to encourage the seep to capture the wolf
                        //float perStepSheepReward = -0.003f;
                        //sheepAgent.AddReward(perStepSheepReward);
                        turnDone = true;
                    }
                    if (sheepNext && sheepNextMove) {
                        HightlightNextPossibleMove(sheepNext);
                        sheepNext.GetComponent<SheepController>().HighLight();

                        if (SheepMovePossible(sheepNext, sheepNextMove)) {
                            SheepMoveTo(sheepNext, sheepNextMove);
                            sheepNextMove = null;
                            sheepNext = null;
                            turn = Player.Wolf;
                            decisionRequested = false;
                        }
                        selectedObject = null;
                        turnDone = true;
                    }
                    break;

                case Player.Wolf:
                    if (GameManager.Instance.wolfAgentController == AgentController.AI && !decisionRequested) {
                        HightlightNextPossibleMove(wolf);
                        wolf.GetComponent<WolfController>().HighLight();

                        // Collects observations and gets an action
                        wolfAgent.RequestDecision();
                        decisionRequested = true;

                        // Add penalty per step to encourage the wolf to get to the end
                        //float perStepWolfReward = -0.003f;
                        //wolfAgent.AddReward(perStepWolfReward);
                        turnDone = true;
                    }

                    if (wolfNextMove) {
                        if (WolfMovePossible(wolfNextMove)) {
                            WolfMoveTo(wolfNextMove);
                            wolfNextMove = null;
                            turn = Player.Sheep;
                            //UnSelect();
                            decisionRequested = false;
                        }
                        selectedObject = null;
                        turnDone = true;
                    }

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

    public void DisableStatsOverlay() {
        statsOverlay.SetActive(false);
    }

    public void OneTimeSetup() {

        sheepColor = GameManager.Instance.sheepColor;
        wolfColor = GameManager.Instance.wolfColor;

        SetupSquares();
        setWolfStartingPosition();
        setSheepStartingPositions();
        winningText.enabled = false;
    }

    public void ResetGame() {
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

        for (int c = 0; c < columns; c++) {
            for (int r = 0; r < rows; r++) {
                SquareController squareC = squares[c, r].GetComponent<SquareController>();
                squareC.Empty();
            }
        }
    }

    private void SetupSquares() {
        columns = GameManager.Instance.rows;
        rows = GameManager.Instance.columns;

        redSquareColor = GameManager.Instance.redSquareColor;
        blackSquareColor = GameManager.Instance.blackSquareColor;

        squares = new SquareController[columns, rows];
        Color clr;
        Color colColor = Color.black;

        for (int c = 0; c < columns; c++) {
            colColor = (colColor == redSquareColor) ? blackSquareColor : redSquareColor;
            clr = colColor;

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

                clr = (clr == redSquareColor) ? blackSquareColor : redSquareColor;
            }
        }
    }

    // Returns the cener of the square given the row and col
    private Vector3 SquareCenter(int c, int r) {
        return new Vector3(c + 0.5f, 0, r + 0.5f);
    }

    private void setWolfStartingPosition() {
        List<int> possibleCols = new List<int>();

        for (int i = 1; i < columns; i = i + 2) {
            possibleCols.Add(i);
        }

        int startingRow = rows - 1;  // fixed
        int startingCol = possibleCols[Random.Range(0, possibleCols.Count)];

        wolf.transform.SetParent(squares[startingCol, startingRow].transform);
        wolf.transform.localPosition = new Vector3(0, 0.5f, 0);
        wolf.GetComponent<WolfController>().SetSquare(squares[startingCol, startingRow]);
        squares[startingCol, startingRow].GetComponent<SquareController>().Occupy(wolf.gameObject);
    }

    private void setSheepStartingPositions() {
        List<int> possibleCols = new List<int>();

        for (int i = 0; i < columns; i = i + 2) {
            possibleCols.Add(i);
        }

        int startingRow = 0;

        for (int i = 0; i < sheep.Length; i++) {
            sheep[i].transform.SetParent(squares[possibleCols[i], startingRow].transform);
            sheep[i].transform.localPosition = new Vector3(0, 0.75f, 0);
            sheep[i].GetComponent<SheepController>().SetSquare(squares[possibleCols[i], startingRow]);
            squares[possibleCols[i], startingRow].GetComponent<SquareController>().Occupy(sheep[i].gameObject);
        }
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
        WolfController wolfController = wolf.GetComponent<WolfController>();

        // Wolf made it to the end
        if (wolfController.Square().GetComponent<SquareController>().row == 0) {
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
        // treat this as a wolf loss to encourage the wolf to make it to the end
        if (!SheepCanMove()) {
            winner = Player.Sheep;

            wolfAgent.SetReward(-.0f);
            wolfAgent.EndEpisode();

            sheepAgent.SetReward(1.0f);
            sheepAgent.EndEpisode();

            sheepWon++;

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

