using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class Show : MonoBehaviour
{
    
    [Header("Tilebase - BasicParts")]
    public Tilemap Tilemap;
    public Tilemap Tilemap_Part;
    [Header("label - parts_data")]
    public Text data_show ;
    public Text parts_show;

    Vector3Int tilePosition3D;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if(!Cell.isUI) showing();

    }

    private void showing()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition = new Vector2(Mathf.Round(mousePosition.x + 0.5f) - 0.5f, Mathf.Round(mousePosition.y + 0.5f) - 0.5f);
        tilePosition3D = new Vector3Int(Mathf.FloorToInt(mousePosition.x), Mathf.FloorToInt(mousePosition.y), 0);

        TileBase clickedTile = null;
        TileBase clickedLine = null;
        try
        {
            clickedTile = Tilemap_Part.GetTile(tilePosition3D);
            clickedLine = Tilemap.GetTile(tilePosition3D);
        }
        catch
        {
        }
        string tiletext="없음";
        string symbol = "";
        if (clickedTile == null) tiletext = "";
        else if (clickedTile.name == "R_V" || clickedTile.name == "R_H") { tiletext = "저항"; symbol = "Ω"; }
        else if (clickedTile.name == "C_V" || clickedTile.name == "C_H") { tiletext = "축전기"; symbol = "μF"; }
        else if (clickedTile.name == "L_V" || clickedTile.name == "L_H") { tiletext = "코일"; symbol = "H"; }
        else if (clickedTile.name == "B_V+" || clickedTile.name == "B_V-" || clickedTile.name == "B_H+" || clickedTile.name == "B_H-") { tiletext = "배터리"; symbol = "V"; }
        else if (clickedTile.name == "S_V+" || clickedTile.name == "S_V-" || clickedTile.name == "S_H+" || clickedTile.name == "S_H-") { tiletext = "스위치"; symbol = ""; }
        else if (clickedLine != null) tiletext = "전선";
        try
        {
            if (clickedTile != null)
            {
                parts_show.text = tiletext + ":" + Cell.data[tilePosition3D][1] + symbol;
            }
            else if (clickedLine != null)
            {
                parts_show.text = "전선";
            }
            else if (clickedTile == null && clickedLine == null) parts_show.text = "";
        }
        catch { }

        try
        {
            double data = Cal.calculated[tilePosition3D];
            string result = data.ToString("0.00");
            data_show.text = "전류(절댓값) : "+result+"A";
        }
        catch {
            if (Cal.seperate.Contains(tilePosition3D)) data_show.text = "분기점";
            else data_show.text = "";
        }
    }
}
