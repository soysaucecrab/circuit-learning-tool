using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class Cell : MonoBehaviour
{
    //tilemap setting
    [Header("Tilemap Setting")]
    public Tilemap tilemap;
    public TileBase changeTile;
    public TileBase selectTile;


    //draw and erasing setting
    bool isClicked=false;
    bool isErasing=false;
    Vector2 clicked;

    //object copy (sight-sensitivity)
    public GameObject objectToPlace;
    private GameObject currentObject;
    private bool isObjectPlaced = false;

    public static bool isUI=false;

    public RuleTile ruleTile;            // 애니메이션이 설정된 RuleTile

    public enum TileType
    {
        Resistor,
        Coil,
        Capacitor,
        Battery_Up,
        Battery_Down,
        swich_Up,
        swich_Down,
        erase
    }

    public enum TileDirection
    {
        Horizontal,
        Vertical
    }

    [Header ("Tilebase - BasicParts")]
    public Tilemap Tilemap_Part;
    [Space(10f)]
    public TileBase Resistor_Horizontal;
    public TileBase Resistor_Vertical;
    [Space(10f)] 
    public TileBase Coil_Horizontal;
    public TileBase Coil_Vertical;
    [Space(10f)]
    public TileBase Capacitor_Horizontal;
    public TileBase Capacitor_Vertical;
    [Space(10f)]
    public TileBase Battery_Horizontal_Up; //+기준 위, 오른쪽이 up임
    public TileBase Battery_Horizontal_Down;
    public TileBase Battery_Vertical_Up;
    public TileBase Battery_Vertical_Down;
    [Space(10f)]
    public TileBase Swich_Horizontal_Opened;
    public TileBase Swich_Horizontal_Closed;
    public TileBase Swich_Vertical_Opened;
    public TileBase Swich_Vertical_Closed;

    //tiletype - data saving, voltage = battery_up
    public static Dictionary<Vector3Int,List<float>> data = new Dictionary<Vector3Int,List<float>>();

    public static int selection=2; //0:line, 1:register, 2: des

    //line to parts setting
    SpriteRenderer sr;

    public static Action removeCell;

    void Awake()
    {
        removeCell = () =>
        {
            RemovePlacedObject();
        };
        isUI = false;

    }

    void Start()
    {
        data = new Dictionary<Vector3Int, List<float>>();
        sr = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition = new Vector2(Mathf.Round(mousePosition.x + 0.5f) - 0.5f, Mathf.Round(mousePosition.y + 0.5f) - 0.5f);


        if (!isUI)
        {
            switch (selection)
            {
                //Line create function setting
                case 0:
                    createLine(mousePosition);
                    break;

                //parts change function setting
                case 1:
                    RemovePlacedObject();
                    OnTileClick(mousePosition);

                    break;
            }


            //object trasformation
            if (currentObject == null || !isObjectPlaced)
            {
                transform.position = mousePosition;
            }
        }
        
        
        
    }

    //linecreate function
    void createLine(Vector2 mousePosition)
    {

        
        if (Input.GetMouseButtonDown(0))
        {

            //click -> Draw line (left-click)
            if (isClicked)
            {
                if (clicked != mousePosition)
                {

                    if (clicked.x < mousePosition.x && clicked.y == mousePosition.y)
                    {
                        for (int i = (clicked.x < 0 ? (int)clicked.x - 1 : (int)clicked.x); i < mousePosition.x; i++) tilemap.SetTile(new Vector3Int((int)i, (clicked.y < 0 ? (int)(clicked.y) - 1 : (int)(clicked.y)), 0), changeTile);
                    }
                    else if (clicked.x >= mousePosition.x && clicked.y == mousePosition.y)
                    {
                        for (int i = (clicked.x < 0 ? (int)clicked.x - 1 : (int)clicked.x); i > mousePosition.x - 1; i--) tilemap.SetTile(new Vector3Int((int)i, (clicked.y < 0 ? (int)(clicked.y) - 1 : (int)(clicked.y)), 0), changeTile);
                    }
                    if (clicked.y < mousePosition.y && clicked.x == mousePosition.x)
                    {
                        for (int i = (clicked.y < 0 ? (int)clicked.y - 1 : (int)clicked.y); i <= mousePosition.y; i++) tilemap.SetTile(new Vector3Int((clicked.x < 0 ? (int)clicked.x - 1 : (int)clicked.x), (int)i, 0), changeTile);
                    }
                    else if (clicked.y >= mousePosition.y && clicked.x == mousePosition.x)
                    {
                        for (int i = (clicked.y < 0 ? (int)clicked.y - 1 : (int)clicked.y); i >= mousePosition.y - 1; i--) tilemap.SetTile(new Vector3Int((clicked.x < 0 ? (int)clicked.x - 1 : (int)clicked.x), (int)i, 0), changeTile);
                    }

                }
                isClicked = false;
                RemovePlacedObject();
            }
            else if (!isErasing)
            {
                PlaceObjectAtMousePosition(mousePosition);
                isClicked = true;
                isErasing = false;
                clicked = mousePosition;
            }
            if (isErasing) { isErasing = false; isClicked = false; RemovePlacedObject(); UpdateObjectColor(); }
        }
        //erase line (right-click)
        if (Input.GetMouseButtonDown(1))
        {
            if (isErasing)
            {

                if (clicked.x < mousePosition.x && clicked.y == mousePosition.y)
                {
                    for (int i = (clicked.x < 0 ? (int)clicked.x - 1 : (int)clicked.x); i < mousePosition.x; i++)
                    {
                        Vector3Int position = new Vector3Int((int)i, (clicked.y < 0 ? (int)(clicked.y) - 1 : (int)(clicked.y)), 0);
                        tilemap.SetTile(position, null);
                        Tilemap_Part.SetTile(position, null);
                        try
                        {
                            data[position].Clear();
                            HUD.deltex(new Vector2Int(position.x, position.y));
                        }
                        catch { }
                    }
                }
                else if (clicked.x >= mousePosition.x && clicked.y == mousePosition.y)
                {
                    for (int i = (clicked.x < 0 ? (int)clicked.x - 1 : (int)clicked.x); i > mousePosition.x - 1; i--)
                    {
                        Vector3Int position = new Vector3Int((int)i, (clicked.y < 0 ? (int)(clicked.y) - 1 : (int)(clicked.y)), 0);
                        tilemap.SetTile(position, null);
                        Tilemap_Part.SetTile(position, null);
                        try
                        {
                            data[position].Clear();
                            HUD.deltex(new Vector2Int(position.x, position.y));
                        }
                        catch { }
                    }
                }
                if (clicked.y < mousePosition.y && clicked.x == mousePosition.x)
                {
                    for (int i = (clicked.y < 0 ? (int)clicked.y - 1 : (int)clicked.y); i <= mousePosition.y; i++)
                    {
                        Vector3Int position = new Vector3Int((clicked.x < 0 ? (int)clicked.x - 1 : (int)clicked.x), (int)i, 0);
                        tilemap.SetTile(position, null);
                        Tilemap_Part.SetTile(position, null);
                        try
                        {
                            data[position].Clear();
                            HUD.deltex(new Vector2Int(position.x, position.y));
                        }
                        catch { }
                    }
                }
                else if (clicked.y >= mousePosition.y && clicked.x == mousePosition.x)
                {
                    for (int i = (clicked.y < 0 ? (int)clicked.y - 1 : (int)clicked.y); i >= mousePosition.y - 1; i--)
                    {
                        Vector3Int position = new Vector3Int((clicked.x < 0 ? (int)clicked.x - 1 : (int)clicked.x), (int)i, 0);
                        tilemap.SetTile(position, null);
                        Tilemap_Part.SetTile(position, null);
                        try
                        {
                            data[position].Clear();
                            HUD.deltex(new Vector2Int(position.x, position.y));
                        }
                        catch { }
                    }
                }

                isClicked = false;
                isErasing = false;

                RemoveLonelyTiles();
                RemovePlacedObject();
            }
            else if (!isClicked)
            {
                PlaceObjectAtMousePosition(mousePosition);
                isClicked = false;
                isErasing = true;
                clicked = mousePosition;
            }
            if (isClicked) { isErasing = false; isClicked = false; RemovePlacedObject(); UpdateObjectColor(); }

        }

        //esc setting
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isClicked = false;
            isErasing = false;
            RemovePlacedObject();
        }

        //color setting
        UpdateObjectColor();
    }

    //LonelyTiles remove system
    private void RemoveLonelyTiles()
    {
        BoundsInt bounds = tilemap.cellBounds;
        TileBase[] allTiles = tilemap.GetTilesBlock(bounds);

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int tilePosition = new Vector3Int(x, y, 0);

                TileBase currentTile = tilemap.GetTile(tilePosition);
                if (currentTile != null)
                {
                    if (!HasAdjacentTiles(tilePosition))
                    {
                        tilemap.SetTile(tilePosition, null);
                        Tilemap_Part.SetTile(tilePosition, null);
                        try
                        {
                            data[tilePosition].Clear();
                            HUD.deltex(new Vector2Int(tilePosition.x, tilePosition.y));
                        }
                        catch { }
                    }
                }
            }
        }
    }

    //check adjacent tiles
    private bool HasAdjacentTiles(Vector3Int position)
    {
        Vector3Int[] adjacentPositions = new Vector3Int[]
        {
            new Vector3Int(position.x + 1, position.y, position.z),  // 오른쪽
            new Vector3Int(position.x - 1, position.y, position.z),  // 왼쪽
            new Vector3Int(position.x, position.y + 1, position.z),  // 위
            new Vector3Int(position.x, position.y - 1, position.z)   // 아래
        };

        // 인접 타일이 있는지 확인
        foreach (var adjacentPosition in adjacentPositions)
        {
            if (tilemap.HasTile(adjacentPosition))
            {
                return true;
            }
        }

        return false; 
    }


    /*  Tile change function codation  */
    //위에꺼랑 반환값 달라서 따로 작성함.
    TileDirection? GetWireDirection(Vector3Int position)
    {

        Vector3Int right = new Vector3Int(position.x + 1, position.y, 0);
        Vector3Int left = new Vector3Int(position.x - 1, position.y, 0);
        Vector3Int up = new Vector3Int(position.x, position.y + 1, 0);
        Vector3Int down = new Vector3Int(position.x, position.y - 1, 0);


        bool isHorizontal = tilemap.HasTile(right) && tilemap.HasTile(left) && tilemap.HasTile(position) && !tilemap.HasTile(up) && !tilemap.HasTile(down);
        bool isVertical = tilemap.HasTile(up) && tilemap.HasTile(down) && tilemap.HasTile(position) && !tilemap.HasTile(right) && !tilemap.HasTile(left);

        if (isHorizontal)
        {
            return TileDirection.Horizontal;
        }
        else if (isVertical)
        {
            return TileDirection.Vertical;
        }

        return null; // 전선 방향이 명확하지 않으면 null 반환
    }

    void ChangeTileType(Vector3Int position, TileType newType)
    {
        
        TileDirection? direction = GetWireDirection(position);

        if (direction == null)
        {
            return;
        }

        TileBase newTileBase = null;

        int select=-1;
        if (direction == TileDirection.Horizontal)
        {
            switch (newType)
            {
                case TileType.Resistor:
                    newTileBase = Resistor_Horizontal;
                    select = 0;
                    break;
                case TileType.Coil:
                    newTileBase = Coil_Horizontal;
                    select = 1;
                    break;
                case TileType.Capacitor:
                    newTileBase = Capacitor_Horizontal;
                    select = 2;
                    break;
                case TileType.Battery_Up:
                    newTileBase = Battery_Horizontal_Up;
                    tileType = TileType.Battery_Up;
                    select = 3;
                    break;
                case TileType.Battery_Down:
                    newTileBase = Battery_Horizontal_Down;
                    tileType = TileType.Battery_Up;
                    select = 4;
                    break;
                case TileType.swich_Up:
                    newTileBase = Swich_Horizontal_Opened;
                    tileType = TileType.swich_Up;
                    select = 5;
                    break;
                case TileType.swich_Down:
                    newTileBase = Swich_Horizontal_Closed;
                    tileType = TileType.swich_Up;
                    select = 6;
                    break;
                case TileType.erase:
                    newTileBase = null;
                    try
                    {
                        data[position].Clear();
                        HUD.deltex(new Vector2Int(position.x, position.y));
                    }
                    catch { }
                    break;
            }
        }
        else if (direction == TileDirection.Vertical)
        {
            switch (newType)
            {
                case TileType.Resistor:
                    newTileBase = Resistor_Vertical;
                    select = 0;
                    break;
                case TileType.Coil:
                    newTileBase = Coil_Vertical;
                    select = 1;
                    break;
                case TileType.Capacitor:
                    newTileBase = Capacitor_Vertical;
                    select = 2;
                    break;
                case TileType.Battery_Up:
                    newTileBase = Battery_Vertical_Up;
                    tileType = TileType.Battery_Up;
                    select = 3;
                    break;
                case TileType.Battery_Down:
                    newTileBase = Battery_Vertical_Down;
                    tileType = TileType.Battery_Up;
                    select = 4;
                    break;
                case TileType.swich_Up:
                    newTileBase = Swich_Vertical_Opened;
                    tileType = TileType.swich_Up;
                    select = 5;
                    break;
                case TileType.swich_Down:
                    newTileBase = Swich_Vertical_Closed;
                    tileType = TileType.swich_Up;
                    select = 6;
                    break;
                case TileType.erase:
                    newTileBase = null;
                    try
                    {
                        data[position].Clear();
                        HUD.deltex(new Vector2Int(position.x, position.y));
                    }
                    catch { }
                    break;
            }
        }
        if (data.ContainsKey(position))
            if (data[position].Count > 0 && data[position][0] != select && newTileBase != null)
            {
                try
                {
                    data[position].Clear();
                    HUD.deltex(new Vector2Int(position.x, position.y));
                }
                catch { }
            }
        Tilemap_Part.SetTile(position, newTileBase);
        if (newTileBase != null) {
            if (!data.ContainsKey(position))
            {
                data[position] = new List<float>();
            }
            data[position].Add(select);
            data[position].Add(0);
        }
    }

    public static Vector3Int tilePosition3D;
    public static TileType tileType;
    void OnTileClick(Vector2 tilePosition)
    {
        //vector2 to vector3
        tilePosition3D = new Vector3Int(Mathf.FloorToInt(tilePosition.x), Mathf.FloorToInt(tilePosition.y), 0);
        TileBase clickedTile = null;
        try
        {
            clickedTile = Tilemap_Part.GetTile(tilePosition3D);
        }
        catch
        {
        }

        if (Input.GetMouseButtonDown(0)){
            //if (EventSystem.current.IsPointerOverGameObject() == true)
            //{        
            //    return;
            //}
             //배터리의 경우 방향 바꿈 설정
                if (clickedTile != null) {
                if ((clickedTile.name == "B_H+" || clickedTile.name == "B_V+") && tileType==TileType.Battery_Up) { tileType = TileType.Battery_Down; }
                if ((clickedTile.name == "B_H-" || clickedTile.name == "B_V-") && tileType == TileType.Battery_Up) { tileType = TileType.Battery_Up; }

                if ((clickedTile.name == "S_H+" || clickedTile.name == "S_V+") && tileType == TileType.swich_Up) { tileType = TileType.swich_Up; }
                if ((clickedTile.name == "S_H" || clickedTile.name == "S_V") && tileType == TileType.swich_Up) { tileType = TileType.swich_Down; }
            }
            ChangeTileType(tilePosition3D, tileType);

        }
        if(Input.GetMouseButtonDown(1))
        {

            if (clickedTile != null) {
                HUD.ui(tilePosition3D,clickedTile.name);
            }
            
        }
    }




    //sight - efficiency
    void PlaceObjectAtMousePosition(Vector2 mousePosition)
    {
        currentObject = Instantiate(objectToPlace, mousePosition, Quaternion.identity);
        /*currentObject.transform.parent = null; */ // currentObject -> parant delete code
        UpdateObjectColor();
        isObjectPlaced = true;
    }

    public void RemovePlacedObject()
    {
        if (currentObject != null)
        {
            Destroy(currentObject);
            isObjectPlaced = false;
        }
    }

    void UpdateObjectColor()
    {
        Color newColor;

        if (isErasing)
        {
            newColor = Color.red;
        }
        else if (isClicked)
        {
            newColor = Color.green;
        }
        else
        {
            newColor = Color.white;
        }

        sr.color = newColor;

        if (currentObject != null)
        {
            SpriteRenderer currentSr = currentObject.GetComponent<SpriteRenderer>();
            if (currentSr != null)
            {
                currentSr.color = newColor;
            }
        }
    }
}
