using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using TMPro;

public class GameManager : Singleton<GameManager> {

    public Transform trainingAreaPrefab;
    public int numTrainingAreas = 1;

    public int wolfWon;
    public int sheepWon;
    public int tie;

    public TMP_Text wolfGamesWonText;
    public TMP_Text sheepGamesWonText;
    public TMP_Text tieText;

    private List<SingleGameManager> traingingAreas = new List<SingleGameManager>();
    private StatsRecorder statsRecorder;
    private bool nextTurnReady = false;

    protected override void Awake() {
        base.Awake();

        Academy.Instance.AutomaticSteppingEnabled = false;
        statsRecorder = Academy.Instance.StatsRecorder;
    }

    // Start is called before the first frame update
    void Start() {
        for (int i = 0; i < numTrainingAreas; i++) {
            GameObject ta = Instantiate(trainingAreaPrefab, new Vector3(0, 0, 0), Quaternion.identity, transform).gameObject;
            ta.transform.SetParent(transform);
            ta.transform.localPosition = new Vector3(0, i * 10, 0);
            SingleGameManager sgm = ta.GetComponentInChildren<SingleGameManager>();

            // Disable per-game overlay in this training mode
            sgm.statsOverlay.SetActive(false);

            traingingAreas.Add(sgm);
        }

        Academy.Instance.EnvironmentStep();

    }

    // Update is called once per frame
    void Update() {

        nextTurnReady = true;

        foreach (var area in traingingAreas) {
            if (!area.turnDone) {
                nextTurnReady = false;
            }
            wolfWon += area.wolfWon;
            sheepWon += area.sheepWon;
            tie += area.tie;
        }

        if (nextTurnReady) {
            Academy.Instance.EnvironmentStep();
            AllowNextTurn();
        }

        wolfGamesWonText.SetText(wolfWon.ToString());
        sheepGamesWonText.SetText(sheepWon.ToString());
        tieText.SetText(tie.ToString());

        // Send stats via SideChannel so that they'll appear in TensorBoard.
        // These values get averaged every summary_frequency steps, so we don't
        // need to send every Update() call.
        if ((Time.frameCount % 100) == 0) {
            statsRecorder.Add("WolfGamesWon", wolfWon);
            statsRecorder.Add("SheepGamesWon", sheepWon);
        }
    }

    private void AllowNextTurn() {
        foreach (var area in traingingAreas) {
            area.turnDone = false;
        }
    }
}
