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
        if (GameManager.Instance.winner == Player.Wolf) {
            SetReward(1.0f);
            Debug.Log("[wolf] Ending episode, wolf won...");
            EndEpisode();
            return;
        } else if (GameManager.Instance.winner == Player.Sheep) {
            SetReward(-1.0f);
            Debug.Log("[wolf] Ending episode, sheep won...");
            EndEpisode();
            return;
        }

        Debug.Log($"[wolf] action received: {branches}");

        wolfSquareController = wolf.Square().GetComponent<SquareController>();

        // 1 branch, 4 options = 4 movement directions
        int movement = Mathf.FloorToInt(branches[0]);
        Debug.Log($"[wolf] movement: {movement}");
        int row = 0, col = 0;

        //List<bool> possibleMoves = wolfSquareController.PossibleWolfMovesDir();

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

        actionMasker.SetMask(0, notAllowed);
    }

    public override void Heuristic(float[] actionsOut) {
        Debug.Log("[wolf] in heuristic");
        List<bool> possibleMoves = wolfSquareController.PossibleWolfMovesDir();

        bool haveMove = false;
        int tries = 0;

        for (int i = 0; i < possibleMoves.Count; i++) {
            bool m = possibleMoves[i];
            Debug.Log($"[wolf] index: {i}; value: {m}");
        }

        while (!haveMove) {
            if (tries > 20) {
                Debug.Log("[wolf] Failed to find wolf move in 20 tries...");
                return;
            }
            for (int i = 0; i < possibleMoves.Count; i++) {
                //if (possibleMoves[i] && Random.Range(0, 1) > 0.5) {
                if (possibleMoves[i]) {
                    Debug.Log($"[wolf] heuristic says: {i} (out of {possibleMoves.Count} possible moves");
                    actionsOut[0] = i;
                    haveMove = true;
                    break;
                }
                tries++;
            }
        }
    }
}
