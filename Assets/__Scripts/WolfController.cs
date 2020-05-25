using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;

public class WolfController : GameObjectBase, IPointerClickHandler {
    // Start is called before the first frame update
    protected override void Start() {
        base.Start();

        Renderer renderer = gameObject.GetComponent<Renderer>();
        renderer.material.SetColor("_Color", gameManager.wolfColor);
    }

    // Update is called once per frame
    protected override void Update() {
        base.Update();

    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        if (gameManager.Turn == Player.Wolf && GameManager.Instance.wolfAgentController == AgentController.Human) {
            base.OnPointerClick(eventData);
        }
    }
}
