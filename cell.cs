using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class cell : MonoBehaviour
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

    public RuleTile ruleTile;            // 애니메이션이 설정된 RuleTile

    public enum TileType
    {
        Resistor,
        Coil,
        Capacitor,
        Battery,
        swich,
    }

    public enum TileDirection
    {
        Horizontal,
        Vertical
    }

    //시발
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

    private int selection=0; //0:line, 1:register

    //line to parts setting
    SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition = new Vector2(Mathf.Round(mousePosition.x + 0.5f) - 0.5f, Mathf.Round(mousePosition.y + 0.5f) - 0.5f);

        if (Input.GetKeyDown(KeyCode.R)) selection = 1;
        if (Input.GetKeyDown(KeyCode.L)) selection = 0;

        switch (selection) //나중에는 이거 HUD로 만들어야됨
        {
            //Line create function setting
            case 0:
                createLine(mousePosition);
                break;

            //parts change function setting
            case 1:
                OnTileClick(mousePosition);
                break;
        }
        
       

        //object trasformation
        if (currentObject == null || !isObjectPlaced)
        {
            transform.position = mousePosition;
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
                    for (int i = (clicked.x < 0 ? (int)clicked.x - 1 : (int)clicked.x); i < mousePosition.x; i++) tilemap.SetTile(new Vector3Int((int)i, (clicked.y < 0 ? (int)(clicked.y) - 1 : (int)(clicked.y)), 0), null);
                }
                else if (clicked.x >= mousePosition.x && clicked.y == mousePosition.y)
                {
                    for (int i = (clicked.x < 0 ? (int)clicked.x - 1 : (int)clicked.x); i > mousePosition.x - 1; i--) tilemap.SetTile(new Vector3Int((int)i, (clicked.y < 0 ? (int)(clicked.y) - 1 : (int)(clicked.y)), 0), null);
                }
                if (clicked.y < mousePosition.y && clicked.x == mousePosition.x)
                {
                    for (int i = (clicked.y < 0 ? (int)clicked.y - 1 : (int)clicked.y); i <= mousePosition.y; i++) tilemap.SetTile(new Vector3Int((clicked.x < 0 ? (int)clicked.x - 1 : (int)clicked.x), (int)i, 0), null);
                }
                else if (clicked.y >= mousePosition.y && clicked.x == mousePosition.x)
                {
                    for (int i = (clicked.y < 0 ? (int)clicked.y - 1 : (int)clicked.y); i >= mousePosition.y - 1; i--) tilemap.SetTile(new Vector3Int((clicked.x < 0 ? (int)clicked.x - 1 : (int)clicked.x), (int)i, 0), null);
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
            Debug.LogWarning("전선의 방향을 확인할 수 없습니다.");
            return;
        }

        TileBase newTileBase = null;

        if (direction == TileDirection.Horizontal)
        {
            switch (newType)
            {
                case TileType.Resistor:
                    newTileBase = Resistor_Horizontal;
                    break;
                case TileType.Coil:
                    newTileBase = Coil_Horizontal;
                    break;
                case TileType.Capacitor:
                    newTileBase = Capacitor_Horizontal;
                    break;
            }
        }
        else if (direction == TileDirection.Vertical)
        {
            switch (newType)
            {
                case TileType.Resistor:
                    newTileBase = Resistor_Vertical;
                    break;
                case TileType.Coil:
                    newTileBase = Coil_Vertical;
                    break;
                case TileType.Capacitor:
                    newTileBase = Capacitor_Vertical;
                    break;
            }
        }

        if (newTileBase != null)
        {
            Tilemap_Part.SetTile(position, newTileBase);
        }
    }

    void OnTileClick(Vector2 tilePosition)
    {
        if(Input.GetMouseButtonDown(0))
        {
            // Vector2를 Vector3Int로 변환 (Z축은 0으로 설정)
            Vector3Int tilePosition3D = new Vector3Int(Mathf.FloorToInt(tilePosition.x), Mathf.FloorToInt(tilePosition.y), 0);

            // 기존의 Vector3Int 매개변수를 사용하는 함수 호출
            ChangeTileType(tilePosition3D, TileType.Resistor);
        }
        if(Input.GetMouseButtonDown(1))
        {
            //여기에 switch 구문 추가해서 선택할 수 있게 해야함
            print(tilePosition);
            Tilemap_Part.SetTile(new Vector3Int((tilePosition.x < 0 ? (int)tilePosition.x - 1 : (int)tilePosition.x), (tilePosition.y < 0 ? (int)tilePosition.y - 1 : (int)tilePosition.y), 0), null);
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

    void RemovePlacedObject()
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
