using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

public class SheepAgent : Agent {

    public WolfController wolf;
    SquareController wolfSquareController;
    SquareController shpSquareController;

    public override void OnEpisodeBegin() {
        Debug.Log("[sheep] begin episode!");
    }

    public override void CollectObservations(VectorSensor sensor) {
        Debug.Log("[sheep] Observing for sheep...");

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
            SetReward(-1.0f);
            Debug.Log("[sheep] Ending episode, wolf won...");
            EndEpisode();
            return;
        } else if (GameManager.Instance.winner == Player.Sheep) {
            SetReward(1.0f);
            Debug.Log("[sheep] Ending episode, sheep won...");
            EndEpisode();
            return;
        }

        Debug.Log($"[sheep] action received: {branches}");

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
        Debug.Log($"[sheep] selection: {selection}");


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
        GameManager.Instance.sheepNext = GameManager.Instance.sheep[sheep];
        SheepController shController = GameManager.Instance.sheep[sheep].GetComponent<SheepController>();
        SquareController squareController = shController.Square().GetComponent<SquareController>();
        Debug.Log($"[sheep] Sheep at: {shController.ToString()}");

        // which square
        int nextRow = squareController.row + row;
        int nextCol = squareController.column + col;

        Debug.Log($"[sheep] nextRow: {nextRow}");
        Debug.Log($"[sheep] nextCol: {nextCol}");

        GameObject nextSquare = GameManager.Instance.squares[nextCol, nextRow];
        Debug.Log($"[sheep] Sheep will move to: {nextSquare.GetComponent<SquareController>().ToString()}");
        GameManager.Instance.sheepNextMove = nextSquare;
    }

    // mask some moves as not possible
    public override void CollectDiscreteActionMasks(DiscreteActionMasker actionMasker) {
        Debug.Log("[sheep] Calculating mask...");

        // contains the list of disallowed sheep/moves by number (see: OnActionReceived)
        List<int> notAllowed = new List<int>();
        List<bool> perSheepAllowedMoves = new List<bool>();

        for (int i = 0; i < GameManager.Instance.sheep.Length; i++) {
            // grab the sheep controller
            SheepController sheep = GameManager.Instance.sheep[i].GetComponent<SheepController>();
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

        Debug.Log($"[sheep] Not Allowed actions: ");
        foreach (var i in notAllowed) {
            Debug.Log($"  {i}");

        }

        // all sheep can't move, this happens if they get to the other side
        // but the wolf has not yet made it to the end (which can happen with random movements)
        if (notAllowed.Count == 8) { // 8 sheep * 2 moves each
            SetReward(-1.0f);
            EndEpisode();
            GameManager.Instance.wolfWon++;
            return;
        };

        actionMasker.SetMask(0, notAllowed);
    }

    // returns the index of a random sheep that can move
    // places the sheep itself into the sheep variable
    private int randomSheepWithMoves(out SheepController sheep) {
        int numSheep = GameManager.Instance.sheep.Length;
        SheepController shController;
        SquareController sqController;

        GameObject match = null;
        float matched = 0;
        int returnIndex = 0;

        for (int i = 0; i < GameManager.Instance.sheep.Length; i++) {
            GameObject sh = GameManager.Instance.sheep[i];
            shController = sh.GetComponent<SheepController>();
            sqController = shController.Square().GetComponent<SquareController>();

            if (sqController.PossibleSheepMoves().Count == 0) { continue; }

            matched++;

            if (Random.value < (1.0f / matched)) {
                returnIndex = i;
                match = sh;
            }
        }

        if (match != null) {
            sheep = match.GetComponent<SheepController>();
        } else {
            Debug.Log("Failed to find a sheep that can move!");
            sheep = null;
        }
        return returnIndex;
    }

    public override void Heuristic(float[] actionsOut) {
        Debug.Log("[sheep] in heuristic");
        List<bool> possibleMoves;

        // pick random sheep that can move
        SheepController shController;
        int sheepIndex = randomSheepWithMoves(out shController);

        if (!shController) { return; };
        Debug.Log($"[sheep] Sheep index (in sheep list) picked: {sheepIndex}");
        Debug.Log($"[sheep] Sheep is at: {GameManager.Instance.sheep[sheepIndex].GetComponent<SheepController>()}");

        SquareController sheepSquareController = shController.Square().GetComponent<SquareController>();

        // pick random move
        possibleMoves = sheepSquareController.PossibleSheepMovesDir();

        // debug output
        for (int i = 0; i < possibleMoves.Count; i++) {
            bool m = possibleMoves[i];
            Debug.Log($"[sheep] index: {i}; value: {m}");
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

        actionsOut[0] = 2 * sheepIndex + moveIndex;
        Debug.Log($"[sheep] heuristic says: {actionsOut[0]} (out of {possibleMoves.Count * GameManager.Instance.sheep.Length} possible moves");
    }
}
