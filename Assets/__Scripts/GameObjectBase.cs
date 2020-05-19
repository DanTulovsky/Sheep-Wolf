using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine;

public class GameObjectBase : MonoBehaviour {
    private Outline outline;
    private bool isSelected;

    public GameObject square;

    protected virtual void Start() {

        outline = gameObject.AddComponent<Outline>();
        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineColor = Color.yellow;
        outline.OutlineWidth = 3f;
        outline.enabled = false;
    }

    protected virtual void Update() {

    }

    public void SetSquare(GameObject sq) {
        square = sq;
    }

    public GameObject Square() {
        return square;
    }

    protected void OnPointerClick(PointerEventData eventData) {
        switch (eventData.button) {
            case PointerEventData.InputButton.Right:
                HightLightRemoveAll();
                GameManager.Instance.UnSelect();
                break;

            case PointerEventData.InputButton.Left:
                HightLightRemoveAll();
                HighLight();
                GameManager.Instance.Select(gameObject);
                break;
        }
    }

    public void HighLight() {
        outline.enabled = true;
        isSelected = true;
    }

    public void HighLightRemove() {
        outline.enabled = false;
    }

    private void HightLightRemoveAll() {
        GameManager.Instance.RemoveSquareHighlights();

        foreach (var obj in GameObject.FindObjectsOfType<GameObjectBase>()) {
            obj.HighLightRemove();
            obj.isSelected = false;
        }
    }

    public bool IsSelected() {
        return isSelected == true;
    }

    public override string ToString() {
        return $"Object at: {square.GetComponent<SquareController>().column},{square.GetComponent<SquareController>().row}";
    }

}
