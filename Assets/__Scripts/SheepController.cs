using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.InputSystem;

public class SheepController : GameObjectBase, IPointerClickHandler {


    // Start is called before the first frame update
    protected override void Start() {
        base.Start();

        Renderer renderer = gameObject.GetComponent<Renderer>();
        renderer.material.SetColor("_Color", gameManager.sheepColor);
    }

    // Update is called once per frame
    protected override void Update() {
        base.Update();

    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        if (gameManager.Turn == Player.Sheep && GameManager.Instance.sheepAgentController == AgentController.Human) {
            base.OnPointerClick(eventData);
            gameManager.selectedObject = gameObject;
        }
    }

}
