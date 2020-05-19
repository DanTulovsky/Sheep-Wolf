using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

public class WolfAgent : Agent {

    public WolfController wolf;
    SquareController shpSquareController;
    SquareController wolfSquareController;

    public override void OnEpisodeBegin() {
        Debug.Log("[wolf] begin episode!");
    }

    public override void CollectObservations(VectorSensor sensor) {
        Debug.Log("[wolf] Observing...");

        // space size: 10

        // current positions: 2
        wolfSquareController = wolf.Square().GetComponent<SquareController>();

        sensor.AddObservation(wolfSquareController.column);
        sensor.AddObservation(wolfSquareController.row);

        // position of the sheep: 4 x (1+1) = 8
        foreach (GameObject shp in GameManager.Instance.sheep) {
            SheepController shpController = shp.GetComponent<SheepController>();
            shpSquareController = shpController.Square().GetComponent<SquareController>();

            sensor.AddObservation(shpSquareController.column);
            sensor.AddObservation(shpSquareController.row);
        }
    }


    public override void OnActionReceived(float[] branches) {
        Debug.Log($"[wolf] action received: {branches}");
        //if (GameManager.Instance.winner == Player.Wolf) {
        //    SetReward(1.0f);
        //    Debug.Log("[wolf] Ending episode (sheep stuck?), wolf won...");
        //    EndEpisode();
        //}
        //if (GameManager.Instance.winner == Player.Sheep) {
        //    SetReward(-1.0f);
        //    Debug.Log("[wolf] Ending episode (how can this happen?), sheep won...");
        //    EndEpisode();
        //}

        wolfSquareController = wolf.Square().GetComponent<SquareController>();
        Debug.Log($"[wolf] Wolf at: {wolfSquareController}");

        // 1 branch, 4 options = 4 movement directions
        int movement = Mathf.FloorToInt(branches[0]);
        Debug.Log($"[wolf] movement: {movement}");
        int row = 0, col = 0;

        if (movement == 0) { row = -1; col = -1; };
        if (movement == 1) { row = -1; col = 1; }
        if (movement == 2) { row = 1; col = -1; }
        if (movement == 3) { row = 1; col = 1; }

        int nextRow = wolfSquareController.row + row;
        int nextCol = wolfSquareController.column + col;

        GameObject nextSquare = GameManager.Instance.squares[nextCol, nextRow];
        Debug.Log($"[wolf] Wolf at: {wolfSquareController.ToString()}");
        Debug.Log($"[wolf] Wolf will move to: {nextSquare.GetComponent<SquareController>().ToString()}");
        GameManager.Instance.wolfNextMove = nextSquare;

        // Check if the wolf is on the 0th row
        //if (nextSquare.GetComponent<SquareController>().row == 0) {
        //    SetReward(1.0f);
        //    GameManager.Instance.wolfWon++;
        //    GameManager.Instance.winner = Player.Wolf;
        //    EndEpisode();
        //}
    }

    // mask some moves as not possible
    public override void CollectDiscreteActionMasks(DiscreteActionMasker actionMasker) {
        Debug.Log("[wolf] Calculating mask...");
        List<bool> possibleMoves = wolfSquareController.PossibleWolfMovesDir();

        List<int> notAllowed = new List<int>() { };

        for (int i = 0; i < possibleMoves.Count; i++) {
            if (!possibleMoves[i]) {
                notAllowed.Add(i); // add the movement number that is not allowed
            }
        }
        Debug.Log($"[wolf] Not Allowed actions: ");
        foreach (var i in notAllowed) {
            Debug.Log($"  {i}");

        }

        // wolf is stuck, can't set mask that excludes all moves
        if (notAllowed.Count == 4) {
            Debug.Log("[wolf] Stuck, nowere to move.");
            //SetReward(-1.0f);
            //EndEpisode();
            //GameManager.Instance.sheepWon++;
            //GameManager.Instance.winner = Player.Sheep;
            return;
        };

        actionMasker.SetMask(0, notAllowed);
    }

    public override void Heuristic(float[] actionsOut) {
        Debug.Log("[wolf] in heuristic");
        List<bool> possibleMoves = wolfSquareController.PossibleWolfMovesDir();

        for (int i = 0; i < possibleMoves.Count; i++) {
            bool m = possibleMoves[i];
            Debug.Log($"[wolf] index: {i}; value: {m}");
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
        Debug.Log($"[wolf] heuristic says: {actionsOut[0]} (out of {possibleMoves.Count} possible moves");
    }
}
