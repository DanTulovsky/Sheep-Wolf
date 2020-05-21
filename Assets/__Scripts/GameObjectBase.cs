﻿using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine;

public class GameObjectBase : MonoBehaviour {
    public SingleGameManager gameManager;

    private Outline outline;
    private bool isSelected;

    public SquareController square;

    protected virtual void Start() {

        outline = gameObject.AddComponent<Outline>();
        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineColor = Color.yellow;
        outline.OutlineWidth = 3f;
        outline.enabled = false;
    }

    protected virtual void Update() {

    }

    public void SetSquare(SquareController sq) {
        square = sq;
    }

    public SquareController Square() {
        return square;
    }

    protected void OnPointerClick(PointerEventData eventData) {
        switch (eventData.button) {
            case PointerEventData.InputButton.Right:
                HightLightRemoveAll();
                gameManager.UnSelect();
                break;

            case PointerEventData.InputButton.Left:
                HightLightRemoveAll();
                HighLight();
                gameManager.Select(gameObject);
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
        gameManager.RemoveSquareHighlights();

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
