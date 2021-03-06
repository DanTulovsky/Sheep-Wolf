﻿using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine;

public class GameObjectBase : MonoBehaviour {
    [SerializeField] protected SingleGameManager gameManager;

    private Outline outline;
    private bool isSelected;
    private SquareController square;

    public Animator anim;

    protected virtual void Start() {
        anim = GetComponent<Animator>();
        anim.enabled = false;

        AddOutline();
    }

    protected virtual void Update() {

    }

    private void AddOutline() {

        outline = gameObject.AddComponent<Outline>();
        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineColor = Color.yellow;
        outline.OutlineWidth = 3f;
        outline.enabled = false;
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
                break;

            case PointerEventData.InputButton.Left:
                HightLightRemoveAll();
                HighLight();
                gameManager.HightlightNextPossibleMove(this);
                break;
        }
    }

    public void HighLight() {
        outline.enabled = true;
        isSelected = true;
    }

    public void HighLightRemove() {
        outline.enabled = false;
        isSelected = false;
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

    public void DisableAnim() {
        anim.enabled = false;
    }

    public void EnableAnim() {
        anim.enabled = true;
    }

}
