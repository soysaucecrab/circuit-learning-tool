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
    public Sprite R_defaultSprite;  // �⺻ �̹���
    public Sprite R_clickedSprite;  // Ŭ������ �� �ٲ� �̹���
    public Sprite L_defaultSprite;  // �⺻ �̹���
    public Sprite L_clickedSprite;  // Ŭ������ �� �ٲ� �̹���
    public Sprite C_defaultSprite;  // �⺻ �̹���
    public Sprite C_clickedSprite;  // Ŭ������ �� �ٲ� �̹���
    public Sprite B_defaultSprite;  // �⺻ �̹���
    public Sprite B_clickedSprite;  // Ŭ������ �� �ٲ� �̹���
    public Sprite S_defaultSprite;  // �⺻ �̹���
    public Sprite S_clickedSprite;  // Ŭ������ �� �ٲ� �̹���
    public Sprite Line_defaultSprite;  // �⺻ �̹���
    public Sprite Line_clickedSprite;  // Ŭ������ �� �ٲ� �̹���
    public Sprite Erase_defaultSprite;  // �⺻ �̹���
    public Sprite Erase_clickedSprite;  // Ŭ������ �� �ٲ� �̹���

    int data, select;
    private HashSet<(Vector2, GameObject)> spawnedTexts = new HashSet<(Vector2, GameObject)>(); // ������ �ؽ�Ʈ���� �����ϴ� ����Ʈ
    //private GameObject selectedText; // ���� ���õ� �ؽ�Ʈ

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

        //List �ʱ�ȭ
        images = new List<Image>();
        df = new List<Sprite>();
        cl = new List<Sprite>();
        // ��ư�� Image ������Ʈ�� ������
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
                createText(posit, $"{datas:F1}��");
                break;
            case 1:
                Cell.data[savePosition][1] = datas;
                createText(posit, $"{datas:F1}��F");
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
            inputText.text = "���� ���� (��)";
            select = 0;
        }
        if (type == "C_H" || type == "C_V")
        {
            hudEliment.SetActive(true);
            inputText.text = "����뷮 ���� (��F)";
            select = 1;
        }
        if (type == "L_H" || type == "L_V")
        {
            hudEliment.SetActive(true);
            inputText.text = "���� ����";
            select = 2;
        }
        if (type == "B_H+" || type == "B_V+" || type == "B_H-" || type == "B_V-")
        {
            hudEliment.SetActive(true);
            inputText.text = "���� ���� (V)";
            select = 3;
        }
    }


    //show function
    //usage guide : selectedText.GetComponent<Text>().text = newText; : change text

    public Grid grid;
    public Vector2 GridToCanvasPosition(Vector2Int gridPosition)
    {
        // �׸����� ����
        float gridMinX = -18;
        float gridMaxX = 18;
        float gridMinY = -11;
        float gridMaxY = 9;

        // ĵ������ ũ��
        float canvasWidth = canvas.GetComponent<RectTransform>().rect.width;
        float canvasHeight = canvas.GetComponent<RectTransform>().rect.height;

        // �׸��� ��ǥ�� ĵ���� ��ǥ�� ��ȯ
        float xRatio = (gridPosition.x - gridMinX) / (gridMaxX - gridMinX);
        float yRatio = (gridPosition.y - gridMinY) / (gridMaxY - gridMinY);

        float canvasX = xRatio * canvasWidth - (canvasWidth / 2) + 30; // ĵ���� �߽��� �������� ����
        float canvasY = yRatio * canvasHeight - (canvasHeight / 2) + 10; // ĵ���� �߽��� �������� ����

        return new Vector2(canvasX, canvasY);
    }

    public void SpawnText(Vector2Int gridPosition, string data)
    {
        DeleteText(gridPosition);
        Vector2 canvasPosition = GridToCanvasPosition(gridPosition);

        // �ؽ�Ʈ ������ ���� �� ��ġ ����
        GameObject newText = Instantiate(textPrefab, canvas.transform);
        newText.GetComponent<RectTransform>().anchoredPosition = canvasPosition;
        newText.GetComponent<Text>().text = data;

        // ������ �ؽ�Ʈ�� ����Ʈ�� �߰�
        spawnedTexts.Add((gridPosition, newText));
    }



    public void DeleteText(Vector2Int position)
    {
        var textToRemove = spawnedTexts.FirstOrDefault(stored => stored.Item1 == position);

        if (textToRemove != default)
        {
            spawnedTexts.Remove(textToRemove); // HashSet���� ����
            Destroy(textToRemove.Item2); // ������ �ؽ�Ʈ ������Ʈ�� ����
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
