using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

public class WolfAgent : Agent {

    public SingleGameManager gameManager;

    public WolfController wolf;
    SquareController shpSquareController;
    SquareController wolfSquareController;

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
            SheepController shpController = shp.GetComponent<SheepController>();
            shpSquareController = shpController.Square().GetComponent<SquareController>();

            sensor.AddObservation(shpSquareController.column / (float)gameManager.columns);
            sensor.AddObservation(shpSquareController.row / (float)gameManager.rows);
        }

        haveObservation = true;
    }


    public override void OnActionReceived(float[] branches) {
        if (!haveObservation) {
            return;
        }

        wolfSquareController = wolf.Square().GetComponent<SquareController>();

        // 1 branch, 4 options = 4 movement directions
        int movement = Mathf.FloorToInt(branches[0]);
        int row = 0, col = 0;

        if (movement == 0) { row = -1; col = -1; };
        if (movement == 1) { row = -1; col = 1; }
        if (movement == 2) { row = 1; col = -1; }
        if (movement == 3) { row = 1; col = 1; }

        int nextRow = wolfSquareController.row + row;
        int nextCol = wolfSquareController.column + col;

        SquareController nextSquare = gameManager.squares[nextCol, nextRow];
        gameManager.wolfNextMove = nextSquare;

    }

    // mask some moves as not possible
    public override void CollectDiscreteActionMasks(DiscreteActionMasker actionMasker) {
        List<bool> possibleMoves = wolfSquareController.PossibleWolfMovesDir();

        List<int> notAllowed = new List<int>() { };

        for (int i = 0; i < possibleMoves.Count; i++) {
            if (!possibleMoves[i]) {
                notAllowed.Add(i); // add the movement number that is not allowed
            }
        }

        actionMasker.SetMask(0, notAllowed);
    }

    public override void Heuristic(float[] actionsOut) {
        List<bool> possibleMoves = wolfSquareController.PossibleWolfMovesDir();

        for (int i = 0; i < possibleMoves.Count; i++) {
            bool m = possibleMoves[i];
        }

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

        if (matched == 0) {
            // no possible moves left
            Debug.Log("[wolf] Heuristic failed to find possible wolf moves.");
        }

        actionsOut[0] = moveIndex;
    }
}
