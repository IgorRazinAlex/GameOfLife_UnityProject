using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class TileBoardSingleplayer : MonoBehaviour
{
    [SerializeField] private Tilemap current_state;
    [SerializeField] private Tilemap next_state;
    [SerializeField] private Tile alive_tile;
    [SerializeField] private Tile dead_tile;

    [SerializeField] private AnimatedTile alive_tile_anim;
    [SerializeField] private AnimatedTile dead_tile_anim;

    [SerializeField] private Slider speed_slider;
    [SerializeField] private float update_speed = 0.01f;

    [SerializeField] private Button random_button;
    [SerializeField] private Button reset_button;

    [SerializeField] private Button switch_mode_button;

    [SerializeField] private Pattern pattern;

    [SerializeField] private Camera main_camera;

    private HashSet<Vector3Int> alive_cells;
    private HashSet<Vector3Int> cells_to_check;

    private bool paused = true;

    private void Awake()
    {
        alive_cells = new HashSet<Vector3Int>();
        cells_to_check = new HashSet<Vector3Int>();

        random_button.onClick.AddListener(delegate{RandomizeGrid();});
        reset_button.onClick.AddListener(delegate{ClearAll();});
        switch_mode_button.onClick.AddListener(delegate{SwitchGameMode();});

        main_camera.orthographic = true;
    }

    private void Start() 
    {
        SetPattern(pattern);
    }

    private void SetPattern(Pattern pattern) 
    {
        ClearAll();
        
        Vector2Int center = pattern.GetCenter();

        for (int i = 0; i <  pattern.cells.Length; ++i)
        {   
            Vector3Int cell = (Vector3Int)(pattern.cells[i] - center);
            current_state.SetTile(cell, alive_tile);
            alive_cells.Add(cell);
        }
    }

    private void SwitchGameMode() {
        SceneManager.LoadScene("MainMenu");
    }

    private void RandomizeGrid()
    {   
        if (!paused) 
        {
            return;
        }

        ClearAll();

        for (int x = -20; x < 20; ++x)
        {
            for (int y = -20; y < 20; ++y)
            {
                int is_alive = Random.Range(0, 2);
                if (is_alive == 1)
                {   
                    Vector3Int cell = new(x, y, 0);
                    current_state.SetTile(cell, alive_tile);
                    alive_cells.Add(cell);
                }
            }
        }
    }

    private void ClearAll() 
    {
        if (!paused) {
            return;
        }
         
        current_state.ClearAllTiles();
        next_state.ClearAllTiles();
        alive_cells.Clear();
        cells_to_check.Clear();
    }

    private void OnEnable()
    {
        StartCoroutine(Simulate());
    }

    private void ChangeCellState(bool alive) {
        if (!paused) 
        {
            return;
        }

        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cell = current_state.WorldToCell(mouseWorldPos);

        if (IsAliveCell(cell) && !alive)
        {
            current_state.SetTile(cell, dead_tile_anim);
            alive_cells.Remove(cell);
        }
        else if (!IsAliveCell(cell) && alive)
        {
            current_state.SetTile(cell, alive_tile_anim);
            alive_cells.Add(cell);
        }
    }

    void OnGUI() {
        if (Event.current.Equals(Event.KeyboardEvent(KeyCode.Space.ToString())))
        {
            paused = !paused;
        }
        else if (Input.GetMouseButton(0)) 
        {
            ChangeCellState(true);
        }
        else if (Input.GetMouseButton(1)) 
        {
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
            if (!paused) 
            {
                UpdateState();
            }
            UpdateSpeed();
            wait_interval = new WaitForSeconds(update_speed);

            yield return wait_interval;
        }
    }

    private bool IsAliveCell(Vector3Int cell) {
        return (current_state.GetTile(cell) == alive_tile) || (current_state.GetTile(cell) == alive_tile_anim);
    }

    private int CountAliveNeighbours(Vector3Int cell) {
        int can = 0;
        for (int x = -1; x <= 1; ++x) 
        {
            for (int y = -1; y <= 1; ++y) 
            {
                if ((x == 0) && (y == 0)) 
                {
                    continue;
                }
                if (IsAliveCell(cell + new Vector3Int(x, y, 0))) 
                {
                    ++can;
                }
            }
        }
        return can;
    }

    private void UpdateState()
    {
        cells_to_check.Clear();

        foreach (Vector3Int cell in alive_cells) {
            for (int x = -1; x <= 1; ++x) 
            {
                for (int y = -1; y <= 1; ++y) 
                {
                    cells_to_check.Add(cell + new Vector3Int(x, y, 0));
                }
            }
        }

        foreach (Vector3Int cell in cells_to_check)
        {
            int neighbours = CountAliveNeighbours(cell);
            bool is_alive = IsAliveCell(cell);

            if (!is_alive && (neighbours == 3)) 
            {
                next_state.SetTile(cell, alive_tile_anim);
                alive_cells.Add(cell);
            } 
            else if (is_alive && ((neighbours < 2) || (neighbours > 3))) 
            {
                next_state.SetTile(cell, dead_tile_anim);
                alive_cells.Remove(cell);
            } 
            else 
            {
                if (current_state.GetTile(cell) == alive_tile_anim)
                {
                    next_state.SetTile(cell, alive_tile);
                }
                else if (current_state.GetTile(cell) == dead_tile_anim)
                {
                    next_state.SetTile(cell, dead_tile);
                }
                else {
                    next_state.SetTile(cell, current_state.GetTile(cell));
                }
            }
        }

        (next_state, current_state) = (current_state, next_state);
        next_state.ClearAllTiles();
    }
}
