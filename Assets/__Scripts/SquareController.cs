using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;

public class SquareController : MonoBehaviour, IPointerClickHandler {
    public GameObject occupant;
    private Outline outline;
    private bool isSelected;

    public Color color;
    public float column;
    public float row;

    // Start is called before the first frame update
    void Start() {
        Renderer renderer = gameObject.GetComponent<Renderer>();
        renderer.material.SetColor("_Color", color);

        outline = gameObject.AddComponent<Outline>();
        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineColor = Color.yellow;
        outline.OutlineWidth = 3f;
        outline.enabled = false;
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

        if (occupant.TryGetComponent<WolfController>(out wolf) && GameManager.Instance.Turn == Player.Wolf) {
            List<GameObject> wolfMoves = PossibleWolfMoves();
            return wolfMoves;
        }

        if (occupant.TryGetComponent<SheepController>(out sheep) && GameManager.Instance.Turn == Player.Sheep) {
            List<GameObject> sheepMoves = PossibleSheepMoves();
            return sheepMoves;
        }

        return new List<GameObject>() { };
    }


    public List<GameObject> PossibleWolfMoves() {
        List<GameObject> moves = new List<GameObject> { };

        // wolf moves forward back backwards
        foreach (int nextRow in new int[] { row + 1, row - 1 }) {
            if (nextRow >= GameManager.Instance.maxRowCol) { continue; }
            if (nextRow < 0) { continue; };

            if (column - 1 >= 0) {
                if (!GameManager.Instance.squares[column - 1, nextRow].GetComponent<SquareController>().IsOccupied()) {
                    moves.Add(GameManager.Instance.squares[column - 1, nextRow]);
                }
            }

            if (column + 1 < GameManager.Instance.maxRowCol) {
                if (!GameManager.Instance.squares[column + 1, nextRow].GetComponent<SquareController>().IsOccupied()) {
                    moves.Add(GameManager.Instance.squares[column + 1, nextRow]);
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

                if (nextRow >= GameManager.Instance.maxRowCol) { moves.Add(false); continue; }
                if (nextRow < 0) { moves.Add(false); continue; };

                if (nextCol >= GameManager.Instance.maxRowCol) { moves.Add(false); continue; }
                if (nextCol < 0) { moves.Add(false); continue; };

                if (!GameManager.Instance.squares[nextCol, nextRow].GetComponent<SquareController>().IsOccupied()) {
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

            if (nextRow >= GameManager.Instance.maxRowCol) { moves.Add(false); continue; }
            if (nextRow < 0) { moves.Add(false); continue; };

            if (nextCol >= GameManager.Instance.maxRowCol) { moves.Add(false); continue; }
            if (nextCol < 0) { moves.Add(false); continue; };

            var square = GameManager.Instance.squares[nextCol, nextRow].GetComponent<SquareController>();
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
        if (nextRow >= GameManager.Instance.maxRowCol) {
            return moves;
        }

        if (column - 1 >= 0) {
            var square = GameManager.Instance.squares[column - 1, nextRow].GetComponent<SquareController>();
            //Debug.Log($"square: {square}, is occupied? {square.IsOccupied()}");

            if (!square.IsOccupied()) {
                moves.Add(square.gameObject);
            }
        }

        if (column + 1 < GameManager.Instance.maxRowCol) {
            var square = GameManager.Instance.squares[column + 1, nextRow].GetComponent<SquareController>();
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

        if (occupant != null) { return; };

        switch (eventData.button) {
            case PointerEventData.InputButton.Left:
                //if (!GameManager.Instance.selectedObject) { return; };

                if (GameManager.Instance.Turn == Player.Wolf) {
                    GameManager.Instance.wolfNextMove = gameObject;
                }
                if (GameManager.Instance.Turn == Player.Sheep) {
                    GameManager.Instance.sheepNextMove = gameObject;
                }
                break;
        }
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
