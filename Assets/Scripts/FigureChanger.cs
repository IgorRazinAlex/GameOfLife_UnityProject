using TMPro;
using UnityEngine;

public class FigureChanger
{
    [SerializeField] public TextMeshProUGUI current_figure;
    public int current_figure_index = 0;
    private string[] figure_names = new string[] {"Cell", "Glider", "Pentomino-R"};

    public Pattern[] figure_patterns;

    public void Initialize()
    {
        figure_patterns = new Pattern[] {
            Resources.Load<Pattern>("FigurePatterns/Cell"),
            Resources.Load<Pattern>("FigurePatterns/Glider"),
            Resources.Load<Pattern>("FigurePatterns/Pentomino-R")
        };
    }

    public void Next()
    {
        UpdateIndex();
        Updatetext();
    }

    private void UpdateIndex()
    {
        ++current_figure_index;
        current_figure_index %= figure_names.Length;
    }

    private void Updatetext()
    {
        current_figure.text = figure_names[current_figure_index];
    }
}