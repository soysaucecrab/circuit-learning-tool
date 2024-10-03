using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public GameObject canvas; // UI connection
    public GameObject textPrefab; // text prefab

    public GameObject hudEliment;
    public GameObject errorEliment;
    public Text inputText;
    public InputField inputData;

    [Space(10)]
    [Header("Button Setting")]
    public Button r;
    public Button l;
    public Button c;
    public Button b;
    public Button s;
    public Button line;
    public Button erase;
    [Space(10)]
    [Header("Sprite Image Setting")]
    public Sprite R_defaultSprite;  // 기본 이미지
    public Sprite R_clickedSprite;  // 클릭했을 때 바꿀 이미지
    public Sprite L_defaultSprite;  // 기본 이미지
    public Sprite L_clickedSprite;  // 클릭했을 때 바꿀 이미지
    public Sprite C_defaultSprite;  // 기본 이미지
    public Sprite C_clickedSprite;  // 클릭했을 때 바꿀 이미지
    public Sprite B_defaultSprite;  // 기본 이미지
    public Sprite B_clickedSprite;  // 클릭했을 때 바꿀 이미지
    public Sprite S_defaultSprite;  // 기본 이미지
    public Sprite S_clickedSprite;  // 클릭했을 때 바꿀 이미지
    public Sprite Line_defaultSprite;  // 기본 이미지
    public Sprite Line_clickedSprite;  // 클릭했을 때 바꿀 이미지
    public Sprite Erase_defaultSprite;  // 기본 이미지
    public Sprite Erase_clickedSprite;  // 클릭했을 때 바꿀 이미지

    int data, select;
    private HashSet<(Vector2, GameObject)> spawnedTexts = new HashSet<(Vector2, GameObject)>(); // 생성된 텍스트들을 저장하는 리스트
    //private GameObject selectedText; // 현재 선택된 텍스트

    public static Action<Vector3Int,string> ui;
    public static Action<Vector2Int, string> createText;
    public static Action<Vector2Int> deltex;
    public static Action errors;

    public static Vector3Int savePosition;
    // Start is called before the first frame update
    void Awake()
    {
        
        ui = (position, type) =>
        {
            dataUI(position, type);
        };
        createText = (position, text) =>
        {
            SpawnText(position, text);
        };
        deltex = (position) =>
        {
            DeleteText(position);
        };
        errors = () =>
        {
            error();
        };
    }

    private List<Image> images;
    private List<Sprite> df;
    private List<Sprite> cl;

    private int parts = -1;

    void Start()
    {
        hudEliment.SetActive(false);

        //List 초기화
        images = new List<Image>();
        df = new List<Sprite>();
        cl = new List<Sprite>();
        // 버튼의 Image 컴포넌트를 가져옴
        images.Add(r.GetComponent<Image>());
        images.Add(l.GetComponent<Image>());
        images.Add(c.GetComponent<Image>());
        images.Add(b.GetComponent<Image>());
        images.Add(s.GetComponent<Image>());
        images.Add(line.GetComponent<Image>());
        images.Add(erase.GetComponent<Image>());

        df.Add(R_defaultSprite);
        df.Add(L_defaultSprite);
        df.Add(C_defaultSprite);
        df.Add(B_defaultSprite);
        df.Add(S_defaultSprite);
        df.Add(Line_defaultSprite);
        df.Add(Erase_defaultSprite);

        cl.Add(R_clickedSprite);
        cl.Add(L_clickedSprite);
        cl.Add(C_clickedSprite);
        cl.Add(B_clickedSprite);
        cl.Add(S_clickedSprite);
        cl.Add(Line_clickedSprite);
        cl.Add(Erase_clickedSprite);

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            Cell.isUI = false;
            if (hudEliment.activeSelf == true) hudEliment.SetActive(false);
            if (Cell.selection == 1 || Cell.selection == 0)
            {
                Cell.selection = 2;
                imageRemove();
                parts = -1;
            }
        }
        
    }
    public void imageRemove()
    {
        for (int i = 0; i < images.Count; i++)
        {
            images[i].sprite = df[i];
        }
    }
    
    public void R()
    {
        imageRemove();
        if (parts==0)
        {
            parts = -1;
            Cell.selection = 2;
        }
        else
        {
            Cell.selection = 1;
            Cell.tileType = Cell.TileType.Resistor;
            images[0].sprite = cl[0];
            parts = 0;
        }
        
    }
    public void L()
    {
        imageRemove();
        if (parts == 1)
        {
            parts = -1;
            Cell.selection = 2;
        }
        else
        {
            Cell.selection = 1;
            Cell.tileType = Cell.TileType.Coil;
            images[1].sprite = cl[1];
            parts = 1;
        }
    }
    public void C()
    {
        imageRemove();
        if (parts == 2)
        {
            parts = -1;
            Cell.selection = 2;
        }
        else
        {
            Cell.selection = 1;
            Cell.tileType = Cell.TileType.Capacitor;
            images[2].sprite = cl[2];
            parts = 2;
        }
    }
    public void B()
    {
        imageRemove();
        if (parts == 3)
        {
            parts = -1;
            Cell.selection = 2;
        }
        else
        {
            Cell.selection = 1;
            Cell.tileType = Cell.TileType.Battery_Up;
            images[3].sprite = cl[3];
            parts = 3;
        }
    }
    public void S()
    {
        imageRemove();
        if (parts == 4)
        {
            parts = -1;
            Cell.selection = 2;
        }
        else
        {
            Cell.selection = 1;
            Cell.tileType = Cell.TileType.swich_Up;
            images[4].sprite = cl[4];
            parts = 4;
        }
    }
    public void Line()
    {
        imageRemove();
        if (parts != -1)
        {
            parts = -1;
            Cell.selection = 2;
        }
        else
        {
            Cell.removeCell();
            Cell.selection = 0;
            images[5].sprite = cl[5];
            parts = 4;
        }
    }
    public void Erase()
    {
        imageRemove();
        if (parts == 6)
        {
            parts = -1;
            Cell.selection = 2;
        }
        else
        {
            Cell.selection = 1;
            Cell.tileType = Cell.TileType.erase;
            images[6].sprite = cl[6];
            parts = 6;
        }
    }


    //for submit button
    public void dataSave()
    {
        Cell.isUI = false;
        hudEliment.SetActive(false);
        float datas;
        Vector2Int posit = new Vector2Int(savePosition.x, savePosition.y);
        try
        {
            datas = float.Parse(inputData.text);
        }
        catch {
            return;
        }
        switch (select){
            
            case 0:
                Cell.data[savePosition][1] = datas;
                createText(posit, $"{datas:F1}Ω");
                break;
            case 1:
                Cell.data[savePosition][1] = datas;
                createText(posit, $"{datas:F1}μF");
                break;
            case 2:
                Cell.data[savePosition][1] = datas;
                createText(posit, $"{datas:F1}H");
                break;
            case 3:
                Cell.data[savePosition][1] = datas; 
                createText(posit, $"{datas:F1}V");
                break;
        }
        Cell.isUI = false;
        inputData.text = "";
    }
    //data setting function
    public void dataUI(Vector3Int position, string type)
    {
        Cell.isUI = true;
        savePosition = position;
        if(type == "R_H" || type == "R_V")
        {
            hudEliment.SetActive(true);
            inputText.text = "저항 변경 (Ω)";
            select = 0;
        }
        if (type == "C_H" || type == "C_V")
        {
            hudEliment.SetActive(true);
            inputText.text = "전기용량 변경 (μF)";
            select = 1;
        }
        if (type == "L_H" || type == "L_V")
        {
            hudEliment.SetActive(true);
            inputText.text = "코일 변경";
            select = 2;
        }
        if (type == "B_H+" || type == "B_V+" || type == "B_H-" || type == "B_V-")
        {
            hudEliment.SetActive(true);
            inputText.text = "전압 변경 (V)";
            select = 3;
        }
    }


    //show function
    //usage guide : selectedText.GetComponent<Text>().text = newText; : change text

    public Grid grid;
    public Vector2 GridToCanvasPosition(Vector2Int gridPosition)
    {
        // 그리드의 범위
        float gridMinX = -18;
        float gridMaxX = 18;
        float gridMinY = -11;
        float gridMaxY = 9;

        // 캔버스의 크기
        float canvasWidth = canvas.GetComponent<RectTransform>().rect.width;
        float canvasHeight = canvas.GetComponent<RectTransform>().rect.height;

        // 그리드 좌표를 캔버스 좌표로 변환
        float xRatio = (gridPosition.x - gridMinX) / (gridMaxX - gridMinX);
        float yRatio = (gridPosition.y - gridMinY) / (gridMaxY - gridMinY);

        float canvasX = xRatio * canvasWidth - (canvasWidth / 2) + 30; // 캔버스 중심을 기준으로 조정
        float canvasY = yRatio * canvasHeight - (canvasHeight / 2) + 10; // 캔버스 중심을 기준으로 조정

        return new Vector2(canvasX, canvasY);
    }

    public void SpawnText(Vector2Int gridPosition, string data)
    {
        DeleteText(gridPosition);
        Vector2 canvasPosition = GridToCanvasPosition(gridPosition);

        // 텍스트 프리팹 생성 및 위치 설정
        GameObject newText = Instantiate(textPrefab, canvas.transform);
        newText.GetComponent<RectTransform>().anchoredPosition = canvasPosition;
        newText.GetComponent<Text>().text = data;

        // 생성된 텍스트를 리스트에 추가
        spawnedTexts.Add((gridPosition, newText));
    }



    public void DeleteText(Vector2Int position)
    {
        var textToRemove = spawnedTexts.FirstOrDefault(stored => stored.Item1 == position);

        if (textToRemove != default)
        {
            spawnedTexts.Remove(textToRemove); // HashSet에서 제거
            Destroy(textToRemove.Item2); // 생성한 텍스트 오브젝트도 삭제
        }
    }
    public void error()
    {
        Cell.isUI = true;
        errorEliment.SetActive(true);
    }
    public void errorReturn()
    {
        Cell.isUI = false;
        errorEliment.SetActive(false);
    }
}
