using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

public class SheepAgent : Agent {

    public SingleGameManager gameManager;

    public WolfController wolf;
    SquareController wolfSquareController;
    SquareController shpSquareController;

    // After a reset, there is sometimes another action that gets sent in
    // But the observation is pre-reset, so it's invalid
    // Workaround that by keeping track of observations.
    private bool haveObservation;

    public override void OnEpisodeBegin() {
        haveObservation = false;
    }

    public override void CollectObservations(VectorSensor sensor) {
        // space size: 10

        // current positions: 2
        wolfSquareController = wolf.Square().GetComponent<SquareController>();

        sensor.AddObservation(wolfSquareController.column / (float)gameManager.columns);
        sensor.AddObservation(wolfSquareController.row / (float)gameManager.rows);

        // position of the sheep: 4 x (1+1) = 8
        foreach (SheepController shp in gameManager.sheep) {
            shpSquareController = shp.Square().GetComponent<SquareController>();

            sensor.AddObservation(shpSquareController.column / (float)gameManager.columns);
            sensor.AddObservation(shpSquareController.row / (float)gameManager.rows);
        }

        haveObservation = true;
    }

    public override void OnActionReceived(float[] branches) {
        if (!haveObservation) {
            return;
        }

        // 1 branch
        //  0 = {sheep = 0; row = 1; col = -1}
        //  1 = {sheep = 0; row = 1; col = 1}
        //  2 = {sheep = 1; row = 1; col = -1}
        //  3 = {sheep = 1; row = 1; col = 1}
        //  4 = {sheep = 2; row = 1; col = -1}
        //  5 = {sheep = 2; row = 1; col = 1}
        //  6 = {sheep = 3; row = 1; col = -1}
        //  7 = {sheep = 3; row = 1; col = 1}
        int selection = Mathf.FloorToInt(branches[0]);


        int sheep = 0, row = 0, col = 0;

        // which way to go
        if (selection == 0) { sheep = 0; row = 1; col = -1; };
        if (selection == 1) { sheep = 0; row = 1; col = 1; };
        if (selection == 2) { sheep = 1; row = 1; col = -1; };
        if (selection == 3) { sheep = 1; row = 1; col = 1; };
        if (selection == 4) { sheep = 2; row = 1; col = -1; };
        if (selection == 5) { sheep = 2; row = 1; col = 1; };
        if (selection == 6) { sheep = 3; row = 1; col = -1; };
        if (selection == 7) { sheep = 3; row = 1; col = 1; };

        // which sheep
        gameManager.sheepNext = gameManager.sheep[sheep];
        SheepController shController = gameManager.sheep[sheep].GetComponent<SheepController>();
        SquareController squareController = shController.Square().GetComponent<SquareController>();

        // which square
        int nextRow = squareController.row + row;
        int nextCol = squareController.column + col;

        SquareController nextSquare = gameManager.squares[nextCol, nextRow];
        gameManager.sheepNextMove = nextSquare;
    }

    // mask some moves as not possible
    public override void CollectDiscreteActionMasks(DiscreteActionMasker actionMasker) {

        // contains the list of disallowed sheep/moves by number (see: OnActionReceived)
        List<int> notAllowed = new List<int>();
        List<bool> perSheepAllowedMoves = new List<bool>();

        for (int i = 0; i < gameManager.sheep.Length; i++) {
            // grab the sheep controller
            SheepController sheep = gameManager.sheep[i].GetComponent<SheepController>();
            SquareController square = sheep.Square().GetComponent<SquareController>();

            // Get List<bool> of possible moves (true = allowed, false = not allowed)
            // Order is (must match above!):
            // row: 1, col: -1 = 0
            // row: 1, col: 1= 1
            perSheepAllowedMoves = square.PossibleSheepMovesDir();

            // for any false, add the index (or index+1) into notAllowed list
            for (int j = 0; j < perSheepAllowedMoves.Count; j++) {
                if (!perSheepAllowedMoves[j]) {
                    notAllowed.Add(2 * i + j);
                }
            }
        }

        actionMasker.SetMask(0, notAllowed);
    }

    // returns the index of a random sheep that can move
    // places the sheep itself into the sheep variable
    private int randomSheepWithMoves(out SheepController sheep) {
        int numSheep = gameManager.sheep.Length;
        SheepController shController;
        SquareController sqController;

        SheepController match = null;
        float matched = 0;
        int returnIndex = 0;

        for (int i = 0; i < gameManager.sheep.Length; i++) {
            SheepController sh = gameManager.sheep[i];
            shController = sh.GetComponent<SheepController>();
            sqController = shController.Square().GetComponent<SquareController>();

            if (sqController.PossibleSheepMoves().Count == 0) { continue; }

            matched++;

            if (Random.value < (1.0f / matched)) {
                returnIndex = i;
                match = sh;
            }
        }

        sheep = match.GetComponent<SheepController>();
        return returnIndex;
    }


    public override void Heuristic(float[] actionsOut) {
        List<bool> possibleMoves;

        // pick random sheep that can move
        SheepController shController;
        int sheepIndex = randomSheepWithMoves(out shController);

        if (!shController) { return; };

        SquareController sheepSquareController = shController.Square().GetComponent<SquareController>();

        // pick random move
        possibleMoves = sheepSquareController.PossibleSheepMovesDir();

        int matched = 0;
        int moveIndex = 0;

        for (int i = 0; i < possibleMoves.Count; i++) {
            if (possibleMoves[i]) {
                matched++;

                if (Random.value < 1 / matched) {
                    moveIndex = i;
                }
            }
        }

        actionsOut[0] = 2 * sheepIndex + moveIndex;
    }
}
