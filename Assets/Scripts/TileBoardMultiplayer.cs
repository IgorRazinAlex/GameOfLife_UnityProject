using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.SocialPlatforms.Impl;

public class TileBoardMultiplayer : MonoBehaviour
{
    [SerializeField] private Tilemap current_state;
    [SerializeField] private Tilemap next_state;
    [SerializeField] private Tile alive_tile_p1;
    [SerializeField] private Tile alive_tile_p2;
    [SerializeField] private Tile dead_tile;
    private readonly int max_start_tile = 50;
    private int cur_set_tile_p1 = 0;
    private int cur_set_tile_p2 = 0;

    [SerializeField] private TextMeshProUGUI current_figure;
    private FigureChanger fig_changer = new FigureChanger();

    [SerializeField] private Slider speed_slider;
    [SerializeField] private float update_speed = 0.01f;

    [SerializeField] private Button reset_button;
    [SerializeField] private Button switch_mode_button;

    [SerializeField] public Button switch_player_button;
    [SerializeField] public TextMeshProUGUI switch_player_button_label;
    private int current_player = 1;

    [SerializeField] private Button start_simulation_button;
    [SerializeField] private Button end_simulation_button;

    [SerializeField] private Camera main_camera;

    private HashSet<Vector3Int> alive_cells;
    private HashSet<Vector3Int> cells_to_check;

    private bool running = false;

    private void Awake()
    {
        InitializeField();
        AddClickListeners();
        InitializeCamera();
        InitializeUI();
    }

    private void InitializeField()
    {
        alive_cells = new HashSet<Vector3Int>();
        cells_to_check = new HashSet<Vector3Int>();
        fig_changer.Initialize();
    }

    private void AddClickListeners()
    {
        reset_button.onClick.AddListener(delegate{ClearAll();});
        switch_mode_button.onClick.AddListener(delegate{SwitchGameMode();});
        switch_player_button.onClick.AddListener(delegate{SwitchPlayer();});
        start_simulation_button.onClick.AddListener(delegate{StartSimulation();});
        end_simulation_button.onClick.AddListener(delegate{EndSimulation();});
    }

    private void InitializeCamera()
    {
        main_camera.orthographic = true;
    }

    private void InitializeUI()
    {
        fig_changer.current_figure = current_figure;
    }

    private void Start()
    {
        ClearAll();
    }

    private void SwitchGameMode()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void SwitchPlayer()
    {
        current_player = 3 - current_player;
        switch_player_button_label.text = $"P{current_player}";
        if (current_player == 1)
        {
            switch_player_button.GetComponent<Image>().color = Color.magenta;
        } 
        else 
        {
            switch_player_button.GetComponent<Image>().color = Color.cyan;
        }
    }

    private void ClearAll() 
    {
        current_state.ClearAllTiles();
        next_state.ClearAllTiles();
        alive_cells.Clear();
        cells_to_check.Clear();

        cur_set_tile_p1 = 0;
        cur_set_tile_p2 = 0;

        ScoreManager.p1_score = 0;
        ScoreManager.p2_score = 0;

        running = false;
    }

    private void OnEnable()
    {
        StartCoroutine(Simulate());
    }

    private void ChangeCellState(bool alive) {
        if (running)
        {
            return;
        }

        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cell = current_state.WorldToCell(mouseWorldPos);

        if ((CellController(cell) != 0) && !alive)
        {
            if (current_state.GetTile(cell) == alive_tile_p1)
            {
                --cur_set_tile_p1;
            }
            else
            {
                --cur_set_tile_p2;
            }

            current_state.SetTile(cell, dead_tile);
            alive_cells.Remove(cell);
        }
        else if ((CellController(cell) == 0) && alive)
        {
            InsertFigure(cell);
        }
    }

    private void InsertFigure(Vector3Int cell)
    {
        Pattern cur_pattern = fig_changer.figure_patterns[fig_changer.current_figure_index];

        if (!CanInsertPattern(cell, cur_pattern))
        {
            return;
        }

        if ((current_player == 1) && (cur_set_tile_p1 + cur_pattern.cells.Length <= max_start_tile))
        {
            FillPattern(cell, cur_pattern, alive_tile_p1);
            cur_set_tile_p1 += cur_pattern.cells.Length;
        }
        else if ((current_player == 2) && (cur_set_tile_p2 + cur_pattern.cells.Length <= max_start_tile))
        {
            FillPattern(cell, cur_pattern, alive_tile_p2);
            cur_set_tile_p2 += cur_pattern.cells.Length;
        }
    }

    private bool CanInsertPattern(Vector3Int cell, Pattern pattern)
    {   
        for (int i = 0; i < pattern.cells.Length; ++i)
        {
            Vector3Int tile_cell = cell + (Vector3Int)pattern.cells[i];
            if (alive_cells.Contains(tile_cell))
            {
                return false;
            }
        }
        
        return true;
    }

    private void FillPattern(Vector3Int cell, Pattern pattern, Tile tile_type)
    {
        for (int i = 0; i < pattern.cells.Length; ++i)
        {
            Vector3Int tile_cell = cell + (Vector3Int)pattern.cells[i];
            current_state.SetTile(tile_cell, tile_type);
            alive_cells.Add(tile_cell);

        }
    }

    void OnGUI() {
        if (Input.GetMouseButton(0)) {
            ChangeCellState(true);
        }
        else if (Input.GetMouseButton(1)) {
            ChangeCellState(false);
        }
        else if (Input.GetKey(KeyCode.Z))
        {
            if (main_camera.orthographicSize > 0.01f)
            {
                main_camera.orthographicSize -= 0.01f;
            }
        }
        else if (Input.GetKey(KeyCode.X))
        {
            main_camera.orthographicSize += 0.01f;
        }
        else if (Event.current.Equals(Event.KeyboardEvent(KeyCode.F.ToString())))
        {
            ChangeCurrentFigure();
        }
    }

    private void UpdateSpeed()
    {
        update_speed = speed_slider.value;
    }

    private IEnumerator Simulate() 
    {   
        var wait_interval = new WaitForSeconds(update_speed);
        while (enabled)
        {   
            if (running) {
                UpdateState();
            }
            UpdateSpeed();
            wait_interval = new WaitForSeconds(update_speed);

            yield return wait_interval;
        }
    }

    // Checks if cell is alive: return 0 if dead, 1 if controlled by p1,
    // 2 if controlled by p2
    private int CellController(Vector3Int cell) {
        TileBase tile = current_state.GetTile(cell);
        if (tile == alive_tile_p1)
        {
            return 1;
        }
        else if (tile == alive_tile_p2)
        {
            return 2;
        }
        return 0;
    }

    // Count alive neighbouring cells in vector: x corresponds to p1 tiles,
    // y to p2 tiles
    private Vector2Int CountAliveNeighbours(Vector3Int cell) {
        Vector2Int can = new(0, 0);
        for (int x = -1; x <= 1; ++x) {
            for (int y = -1; y <= 1; ++y) {
                if ((x == 0) && (y == 0)) {
                    continue;
                }

                int cell_controller = CellController(cell + new Vector3Int(x, y, 0));

                if (cell_controller == 1) 
                {
                    ++can.x;
                }
                else if (cell_controller == 2)
                {
                    ++can.y;
                }
            }
        }
        return can;
    }

    private void UpdateState()
    {
        cells_to_check.Clear();

        foreach (Vector3Int cell in alive_cells) {
            for (int x = -1; x <= 1; ++x) {
                for (int y = -1; y <= 1; ++y) {
                    cells_to_check.Add(cell + new Vector3Int(x, y, 0));
                }
            }
        }

        foreach (Vector3Int cell in cells_to_check)
        {
            Vector2Int neighbours = CountAliveNeighbours(cell);
            int total_neighbours = neighbours.x + neighbours.y;
            int cell_controller = CellController(cell);

            if ((cell_controller == 0) && (total_neighbours == 3))
            {
                if (neighbours.x > neighbours.y)
                {
                    next_state.SetTile(cell, alive_tile_p1);
                    ++ScoreManager.p1_score;
                }
                else
                {
                     next_state.SetTile(cell, alive_tile_p2);
                     ++ScoreManager.p2_score;
                }
                
                alive_cells.Add(cell);
            } 
            else if ((cell_controller != 0) && ((total_neighbours < 2) || (total_neighbours > 3))) 
            {
                next_state.SetTile(cell, dead_tile);
                alive_cells.Remove(cell);
            } 
            else 
            {
                next_state.SetTile(cell, current_state.GetTile(cell));
            }
        }

        (next_state, current_state) = (current_state, next_state);
        next_state.ClearAllTiles();
    }

    private void StartSimulation()
    {
        running = true;
    }

    private void EndSimulation()
    {
        running = false;
        SceneManager.LoadScene("ResultsScene");
    }

    private void ChangeCurrentFigure()
    {
        fig_changer.Next();
    }
}
