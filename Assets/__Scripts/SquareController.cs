using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;

public class SquareController : MonoBehaviour, IPointerClickHandler {
    public SingleGameManager gameManager;

    public GameObject occupant;
    private Outline outline;
    private bool isSelected;

    public Color color;
    public int column;
    public int row;

    void Awake() {

        outline = gameObject.AddComponent<Outline>();
        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineColor = Color.yellow;
        outline.OutlineWidth = 3f;
        outline.enabled = false;
    }

    // Start is called before the first frame update
    void Start() {
        Renderer renderer = gameObject.GetComponent<Renderer>();
        renderer.material.SetColor("_Color", color);
    }

    // Update is called once per frame
    void Update() {

    }

    public bool IsOccupied() {
        return occupant != null;
    }

    public void Occupy(GameObject o) {
        occupant = o;
    }
    public void Empty() {
        occupant = null;
    }

    public List<GameObject> PossibleMoves() {
        if (!occupant) { return new List<GameObject>() { }; };

        WolfController wolf;
        SheepController sheep;

        if (occupant.TryGetComponent<WolfController>(out wolf) && gameManager.Turn == Player.Wolf) {
            List<GameObject> wolfMoves = PossibleWolfMoves();
            return wolfMoves;
        }

        if (occupant.TryGetComponent<SheepController>(out sheep) && gameManager.Turn == Player.Sheep) {
            List<GameObject> sheepMoves = PossibleSheepMoves();
            return sheepMoves;
        }

        return new List<GameObject>() { };
    }


    public List<GameObject> PossibleWolfMoves() {
        List<GameObject> moves = new List<GameObject> { };

        // wolf moves forward back backwards
        foreach (int nextRow in new int[] { row + 1, row - 1 }) {
            if (nextRow >= gameManager.maxRowCol) { continue; }
            if (nextRow < 0) { continue; };

            if (column - 1 >= 0) {
                if (!gameManager.squares[column - 1, nextRow].GetComponent<SquareController>().IsOccupied()) {
                    moves.Add(gameManager.squares[column - 1, nextRow]);
                }
            }

            if (column + 1 < gameManager.maxRowCol) {
                if (!gameManager.squares[column + 1, nextRow].GetComponent<SquareController>().IsOccupied()) {
                    moves.Add(gameManager.squares[column + 1, nextRow]);
                }
            }
        }
        return moves;
    }

    // returns a true, false list for the possible directions the wolf can move:
    // row: -1, col: -1 = 0
    // row: -1, col: 1 = 1
    // row: 1, col: -1 = 2
    // row: 1, col: 1 = 3
    public List<bool> PossibleWolfMovesDir() {
        List<bool> moves = new List<bool> { };

        // wolf moves forward and backwards
        foreach (int nextRow in new int[] { row - 1, row + 1 }) {
            foreach (int nextCol in new int[] { column - 1, column + 1 }) {

                if (nextRow >= gameManager.maxRowCol) { moves.Add(false); continue; }
                if (nextRow < 0) { moves.Add(false); continue; };

                if (nextCol >= gameManager.maxRowCol) { moves.Add(false); continue; }
                if (nextCol < 0) { moves.Add(false); continue; };

                if (!gameManager.squares[nextCol, nextRow].GetComponent<SquareController>().IsOccupied()) {
                    moves.Add(true);
                } else {
                    moves.Add(false);
                }
            }
        }

        for (int i = 0; i < moves.Count; i++) {
            bool m = moves[i];
        }

        return moves;
    }

    // returns a true, false list for the possible directions a sheep can move:
    // row: 1, col: -1 = 0
    // row: 1, col: 1= 1
    public List<bool> PossibleSheepMovesDir() {
        List<bool> moves = new List<bool> { };

        int nextRow = row + 1;

        // sheep can only move forward
        foreach (int nextCol in new int[] { column - 1, column + 1 }) {

            if (nextRow >= gameManager.maxRowCol) { moves.Add(false); continue; }
            if (nextRow < 0) { moves.Add(false); continue; };

            if (nextCol >= gameManager.maxRowCol) { moves.Add(false); continue; }
            if (nextCol < 0) { moves.Add(false); continue; };

            var square = gameManager.squares[nextCol, nextRow].GetComponent<SquareController>();
            Debug.Log($">> square: {square}, is occupied? {square.IsOccupied()}");

            if (!square.IsOccupied()) {
                moves.Add(true);
            } else {
                moves.Add(false);
            }
        }

        for (int i = 0; i < moves.Count; i++) {
            bool m = moves[i];
        }

        return moves;
    }

    public List<GameObject> PossibleSheepMoves() {
        List<GameObject> moves = new List<GameObject> { };

        // sheep only move forward
        int nextRow = row + 1;
        if (nextRow >= gameManager.maxRowCol) {
            return moves;
        }

        if (column - 1 >= 0) {
            var square = gameManager.squares[column - 1, nextRow].GetComponent<SquareController>();
            //Debug.Log($"square: {square}, is occupied? {square.IsOccupied()}");

            if (!square.IsOccupied()) {
                moves.Add(square.gameObject);
            }
        }

        if (column + 1 < gameManager.maxRowCol) {
            var square = gameManager.squares[column + 1, nextRow].GetComponent<SquareController>();
            //Debug.Log($"square: {square}, is occupied? {square.IsOccupied()}");

            if (!square.IsOccupied()) {
                moves.Add(square.gameObject);
            }
        }

        return moves;
    }

    public void HighLight() {
        outline.enabled = true;
        isSelected = true;
    }

    public void HighLightRemove() {
        outline.enabled = false;
    }

    public override string ToString() {
        return $"{column},{row}";
    }

    public void OnPointerClick(PointerEventData eventData) {

        //if (occupant != null) { return; };

        //switch (eventData.button) {
        //    case PointerEventData.InputButton.Left:
        //        //if (!gameManager.selectedObject) { return; };

        //        if (gameManager.Turn == Player.Wolf) {
        //            gameManager.wolfNextMove = gameObject;
        //        }
        //        if (gameManager.Turn == Player.Sheep) {
        //            gameManager.sheepNextMove = gameObject;
        //        }
        //        break;
        //}
    }

    public bool IsWolf(GameObject obj) {

        if (obj.TryGetComponent<WolfController>(out _)) {
            return true;
        }
        return false;
    }

    public bool IsSheep(GameObject obj) {

        if (obj.TryGetComponent<SheepController>(out _)) {
            return true;
        }
        return false;
    }
}
