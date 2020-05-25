using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.MLAgents;
using TMPro;

public enum AgentController {
    Human,
    AI
}

public class GameManager : Singleton<GameManager> {

    [Header("Training Settings")]
    [SerializeField] private Transform trainingAreaPrefab;
    [SerializeField] private int numTrainingAreas = 1;

    [Tooltip("Number of rows")]
    [Range(8, 16)] public int rows = 8;
    [Tooltip("Number of columns")]
    [Range(8, 16)] public int columns = 8;

    [Header("Display Text")]
    [SerializeField] private TMP_Text wolfGamesWonText;
    [SerializeField] private TMP_Text sheepGamesWonText;
    [SerializeField] private TMP_Text tieText;

    [Header("Agent Control")]
    public AgentController sheepAgentController = AgentController.Human;
    public AgentController wolfAgentController = AgentController.AI;

    [Header("Game Settings")]
    public Color redSquareColor = Color.red;
    public Color blackSquareColor = Color.black;
    public Color sheepColor = Color.white;
    public Color wolfColor = Color.black;
    [HideInInspector] public bool haveAI = false;

    [Header("UI Elements")]
    public TMP_Dropdown dropdownSheepController;
    public TMP_Dropdown dropdownWolfController;


    private int wolfWon;
    private int sheepWon;
    private int tie;

    private List<SingleGameManager> traingingAreas = new List<SingleGameManager>();
    private StatsRecorder statsRecorder;
    private bool nextTurnReady = false;
    private bool startCalled = false;

    public bool gameReadyToStart = false;
    public GameObject menuCanvas;
    public GameObject gameStatsOverlay;


    protected override void Awake() {
        base.Awake();



        if (wolfAgentController == AgentController.AI || sheepAgentController == AgentController.AI) {
            haveAI = true;
        }

        Academy.Instance.AutomaticSteppingEnabled = false;
        statsRecorder = Academy.Instance.StatsRecorder;
    }

    // Start is called before the first frame update
    void Start() {
        StartIfRead();
    }

    void StartIfRead() {

        if (!gameReadyToStart || startCalled) { return; };

        int numCols = Mathf.FloorToInt(Mathf.Sqrt(numTrainingAreas));

        int row = 0;
        int col = 0;

        for (int i = 0; i < numTrainingAreas; i++) {
            GameObject ta = Instantiate(trainingAreaPrefab, new Vector3(0, 0, 0), Quaternion.identity, transform).gameObject;
            ta.transform.SetParent(transform);
            ta.transform.localPosition = new Vector3(col * columns + i % numCols, 0, row);
            SingleGameManager sgm = ta.GetComponentInChildren<SingleGameManager>();

            col++;

            if (col >= numCols) {
                row += rows + 1;
                col = 0;
            }


            // Disable per-game overlay in this training mode
            if (numTrainingAreas > 1) {
                sgm.DisableStatsOverlay();
            }

            traingingAreas.Add(sgm);
        }

        if (haveAI) {
            Academy.Instance.EnvironmentStep();
        }

        startCalled = true;
    }

    // Update is called once per frame
    void Update() {
        StartIfRead();
        UpdateIfRunning();
    }


    void UpdateIfRunning() {

        if (!gameReadyToStart || !startCalled) { return; };

        nextTurnReady = true;
        wolfWon = 0;
        sheepWon = 0;
        tie = 0;

        // Send stats via SideChannel so that they'll appear in TensorBoard.
        // These values get averaged every summary_frequency steps, so we don't
        // need to send every Update() call.
        if ((Time.frameCount % 100) == 0) {
            statsRecorder.Add("WolfGamesWon", wolfWon);
            statsRecorder.Add("SheepGamesWon", sheepWon);
        }

        foreach (var area in traingingAreas) {
            if (!area.turnDone) {
                nextTurnReady = false;
                return;
            }
            wolfWon += area.wolfWon;
            sheepWon += area.sheepWon;
            tie += area.tie;
        }

        if (nextTurnReady) {
            if (haveAI) {
                Academy.Instance.EnvironmentStep();
            }
            AllowNextTurn();

            wolfGamesWonText.SetText(wolfWon.ToString());
            sheepGamesWonText.SetText(sheepWon.ToString());
            tieText.SetText(tie.ToString());
        }
    }

    private void AllowNextTurn() {
        foreach (var area in traingingAreas) {
            area.turnDone = false;
        }
    }

    public void StartGame() {

        gameReadyToStart = true;
        menuCanvas.SetActive(false);
        gameStatsOverlay.SetActive(true);
    }

    public void SetSheepAgentController() {
        var value = dropdownSheepController.options[dropdownSheepController.value];
        sheepAgentController = (AgentController)System.Enum.Parse(typeof(AgentController), value.text);
    }

    public void SetWolfAgentController() {
        var value = dropdownWolfController.options[dropdownWolfController.value];
        wolfAgentController = (AgentController)System.Enum.Parse(typeof(AgentController), value.text);
    }
}
