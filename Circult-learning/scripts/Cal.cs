using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Transactions;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using MathNet.Numerics.LinearAlgebra;
using UnityEngine.SceneManagement;

// edge class
class Edge
{
    public Vector3Int FromVertex { get; set; }  // ��� ����
    public Vector3Int ToVertex { get; set; }    // ���� ����
    public List<Vector3Int> roads { get; set; }
    public float Register { get; set; }
    public float Voltage { get; set; }
    public int Capacitor { get; set; }
    public double current { get; set; }

    //class initation
    public Edge(Vector3Int fromVertex, Vector3Int toVertex)
    {
        FromVertex = fromVertex; // start node position
        ToVertex = toVertex;     // end node position
    }

    /**
    �ؿ� �� �Լ��� ���� ���� override�ؼ� �����ϴ� �۾��̶� ��꿡�� ũ�� �߿����� ����.
    Dictionary�� ���� ���� class �ڷ����� ����ϱ� ���� ���۾���.
    ���� �ϱ� �ٶ�.
    **/
    public override bool Equals(object obj)
    {
        if (obj == null || !(obj is Edge))
        {
            return false;
        }
        Edge other = (Edge)obj;
        return FromVertex == other.FromVertex && ToVertex == other.ToVertex;
    }

    // GetHashCode() -> redefination
    public override int GetHashCode()
    {
        return FromVertex.GetHashCode() ^ ToVertex.GetHashCode();
    }

    // Edge ������ ���ڿ��� ǥ��
    public override string ToString()
    {
        return $"Edge from {FromVertex} to {ToVertex}";
    }
}
public class Cal : MonoBehaviour
{
    GameObject cell;

    Tilemap tilemap;
    Tilemap tilemap_part;

    public static HashSet<Vector3Int> seperate = new HashSet<Vector3Int>();
    Vector3Int start;

    //HashSet ����Ϸ� �Ͽ����� ������ �߿��Ͽ� List�� ���·� ��Ÿ��.
    List<Edge> edges = new List<Edge>();
    List<Vector3Int> zeros = new List<Vector3Int>();
    List<List<List<float>>> kvls = new List<List<List<float>>>();
    List<List<int>> kcls = new List<List<int>>();


    public static Dictionary<Vector3Int, double> calculated = new Dictionary<Vector3Int, double>();

    int count = 0;
    bool started = false;

    int polynomial = 0; //���׽��̶�� ����.

    bool iscalculated = false;

    public void Awake()
    {
        calculated = new Dictionary<Vector3Int, double>();
        seperate = new HashSet<Vector3Int>();
        kvls = new List<List<List<float>>>();
        kcls = new List<List<int>>();
    }

    public void Start()
    {
        cell = GameObject.Find("cell");
        tilemap = cell.GetComponent<Cell>().tilemap;
        tilemap_part = cell.GetComponent<Cell>().Tilemap_Part;
        start = new Vector3Int(); // start �ʱ�ȭ
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) { 
            Cell.data = new Dictionary<Vector3Int, List<float>>();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    //main function
    public void calculate()
    {

        // �ʱ�ȭ
        //try
        //{ 
            clearation();
            // �б��� ���
            checkSeperate();
            // DFS Ž��
            DFS_FIRST(start, start, 0, 0, 0, new HashSet<Vector3Int>(), start, new List<Vector3Int>());

            DFS_SECOND(new List<List<float>>(), edges[0], 0, 0, true);
            KCL();

            metrix();
            
            iscalculated = true;
        //}
        //catch {
        //    //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        //    HUD.errors();
        //}
        
        //print("-----------------------------");

    }

    private void clearation()
    {
        seperate = new HashSet<Vector3Int>();

        //HashSet ����Ϸ� �Ͽ����� ������ �߿��Ͽ� List�� ���·� ��Ÿ��.
        edges = new List<Edge>();
        zeros = new List<Vector3Int>() { };
        kvls = new List<List<List<float>>>() { };
        kcls = new List<List<int>>() { };

        calculated = new Dictionary<Vector3Int, double>();

        count = 0;
        started = false;
        polynomial = 0; //���׽��̶�� ����.
    }


    //�б��� ����
    private void checkSeperate()
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
                    if (HasAdjacentTiles(tilePosition) > 2)
                    {
                        seperate.Add(tilePosition);
                        count++;
                        if (!started)
                        {
                            start.x = tilePosition.x;
                            start.y = tilePosition.y;
                            start.z = 0;
                            started = true;
                        }
                    }
                }
            }
        }

        if(seperate.Count == 0) { seperate.Add(start); }
    }

    //�б��� �Ǵ�
    //check adjacent tiles
    private int HasAdjacentTiles(Vector3Int position)
    {
        int cnt = 0;
        Vector3Int[] adjacentPositions = new Vector3Int[]
        {
            new Vector3Int(position.x + 1, position.y, position.z),  // ������
            new Vector3Int(position.x - 1, position.y, position.z),  // ����
            new Vector3Int(position.x, position.y + 1, position.z),  // ��
            new Vector3Int(position.x, position.y - 1, position.z)   // �Ʒ�
        };

        // adjacentPosition check
        foreach (var adjacentPosition in adjacentPositions)
        {
            if (tilemap.HasTile(adjacentPosition)) cnt++;
        }
        return cnt;
    }


    //first DFS()
    bool startpass = false;
    private void DFS_FIRST(Vector3Int position, Vector3Int sapr, float register, float voltage, int direction, HashSet<Vector3Int> sep, Vector3Int post, List<Vector3Int> road)
    {
        road.Add(position);
        foreach (var sape in sep)
        {
            if (position == sape && sape != start) return;
        }
        // �б����� ������ ���, ������ ����ġ�� ����
        if (seperate.Contains(position))
        {
            Edge edge = new Edge(sapr, position);
            edge.Register = register;
            edge.Voltage = voltage;

            bool again = false;
            if (sapr == position)
            {
                again = true;
            }

            foreach (var edged in edges)
            {
                if (edged.FromVertex == position && edged.roads.Count == road.Count)
                {
                    
                    for (int i = 1; i < road.Count - 1; i++)
                    {
                        print(i + ":" + (road.Count - i - 1));
                        if (road[i] == edged.roads[road.Count - i-1]) again = true;
                    }
                }
                if (edged.ToVertex == position && edged.roads.Count == road.Count)
                {
                    try
                    {
                        for (int i = 1; i < road.Count - 1; i++)
                        {
                            if (road[i] == edged.roads[i])
                            {
                                //print(road[i]+" =? " +edged.roads[i]);
                                again = true;
                            }
                        }
                    }
                    catch {}
                }
            }
            if (!again)
            {
                edges.Add(edge);
                edges[edges.Count - 1].roads = road;
                road = new List<Vector3Int>();
                //print("edge : " + edges[edges.Count - 1].roads[edges[edges.Count].roads.Count-1]);

                //print(edge.FromVertex + " " + edge.ToVertex );
            }
            road = new List<Vector3Int>();
            road.Add(position);
            sapr = position; // �б������� ���� ����
            register = 0;
            voltage = 0;

        }
        sep.Add(position);
        //data�� ������� �ʰ� �̰� ����ϴ� ������ preview ������.
        TileBase tile = tilemap_part.GetTile(position);
        if (tile != null)
        {
            if (tile.name == "R_H" || tile.name == "R_V")
            {
                register += Cell.data[position][1];
            }
            if (tile.name == "B_H+")
            {
                //0:right-ward 1:left-ward 2:up-ward 3:down-ward
                if (direction == 0)
                {
                    voltage += Cell.data[position][1];
                }
                if (direction == 1)
                {
                    voltage -= Cell.data[position][1];
                }
            }
            else if (tile.name == "B_H-")
            {
                //0:right-ward 1:left-ward 2:up-ward 3:down-ward
                if (direction == 0)
                {
                    voltage -= Cell.data[position][1];
                }
                if (direction == 1)
                {
                    voltage += Cell.data[position][1];
                }
            }
            if (tile.name == "B_V+")
            {
                //0:right-ward 1:left-ward 2:up-ward 3:down-ward
                if (direction == 2)
                {
                    voltage += Cell.data[position][1];
                }
                if (direction == 3)
                {
                    voltage -= Cell.data[position][1];
                }
            }
            else if (tile.name == "B_V-")
            {
                //0:right-ward 1:left-ward 2:up-ward 3:down-ward
                if (direction == 2)
                {
                    voltage -= Cell.data[position][1];
                }
                if (direction == 3)
                {
                    voltage += Cell.data[position][1];
                }
            }
        }


        // �� �������� DFS Ž��
        Vector3Int[] adjacentPositions = new Vector3Int[]
        {
        new Vector3Int(position.x + 1, position.y, position.z),  // ������
        new Vector3Int(position.x - 1, position.y, position.z),  // ����
        new Vector3Int(position.x, position.y + 1, position.z),  // ��
        new Vector3Int(position.x, position.y - 1, position.z)   // �Ʒ�
        };

        for (int i = 0; i < 4; i++)
        {
            Vector3Int adjacentPosition = adjacentPositions[i];
            if (adjacentPosition == start && post == start) continue;
            if (position == start && tilemap.HasTile(adjacentPosition))
            {
                //one directional
                DFS_FIRST(adjacentPosition, sapr, register, voltage, i, new HashSet<Vector3Int>(sep), position, new List<Vector3Int>(road));
                break;
            }
            if (adjacentPosition == start && tilemap.HasTile(adjacentPosition) && post != start)
            {
                DFS_FIRST(adjacentPosition, sapr, register, voltage, i, new HashSet<Vector3Int>(sep), position, new List<Vector3Int>(road));

            }
            if (tilemap.HasTile(adjacentPosition) && !sep.Contains(adjacentPosition))
            {
                // �湮���� ���� ���� Ÿ�Ϸ� DFS ��� ȣ��
                DFS_FIRST(adjacentPosition, sapr, register, voltage, i, new HashSet<Vector3Int>(sep), position, new List<Vector3Int>(road));
            }
        }
    }





    //seconde DFS()
    //first�� ���� ��������� first���� ���ߴ� ���� ������ ����Ͽ� ������ ���� voltage�� register�� �ջ��� �־�� ��.
    //first���� ���ߴ� ������ index�� �ο��Ͽ� ����ϸ� �� �� ��. : edge�� �ο��ϴ� ��.
    //������ �� �Ȱ�����. : ����� edge�� ������ �������� edge���� Ư�� �������� �����Ͽ� DFS�� ��� ������ ���� �ٽ� �������� ���ƿ��� �� �� ���� ������ ����.
    //�� �������� ������ 0�� ��Ȳ�� ����Ͽ� �Բ� ���α׷��� �� �� �ֵ��� �� ����.
    private void DFS_SECOND(List<List<float>> edgess, Edge position, int depth, int index, bool isDirect)
    {

        // ���ο� ����Ʈ �߰� �� �� ����
        edgess.Add(new List<float> { index, 0, 0 });

        if (isDirect)
        {
            edgess[depth][1] = edges[index].Register;
            edgess[depth][2] = edges[index].Voltage;
        }
        else
        {
            edgess[depth][1] = -edges[index].Register;
            edgess[depth][2] = -edges[index].Voltage;
        }

        if (position.ToVertex == start)
        {
            // �� ����
            polynomial++;
            kvls.Add(edgess);  // edgess�� kvls�� �߰�
            return;
        }


        ///!!!!!!!!!!! isDirect�� ���� position.to���� position.from���� �޶��� ���� �ؾ���.
        bool isZero = false;
        for (int i = 0; i < edges.Count; i++)
        {
            bool visited = false;
            for (int j = 0; j < edgess.Count; j++)
            {
                if (edgess[j].Count > 0 && i == edgess[j][0])
                {
                    visited = true;
                    break; // �̹� �湮�� �� ���� ó��
                }
            }
            if (visited) continue;
            if (isDirect)
            {
                if (edges[i].FromVertex == position.ToVertex && edges[i].Register == 0)
                {
                    isZero = true;
                    //if(!isZero) kcls_zero.Add(new List<int> { 0, 0, 0, 0 }); //0 : input, 1-3 : output //�ʿ���� ����.
                    DFS_SECOND(edgess.Select(list => new List<float>(list)).ToList(), edges[i], depth + 1, i, true);
                }
                if (edges[i].ToVertex == position.ToVertex && edges[i].Register == 0)
                {
                    isZero = true;
                    DFS_SECOND(edgess.Select(list => new List<float>(list)).ToList(), edges[i], depth + 1, i, false);
                }
            }
            else //�ݴ����
            {
                if ((edges[i].FromVertex == position.FromVertex) && edges[i].Register == 0)
                {
                    isZero = true;
                    DFS_SECOND(edgess.Select(list => new List<float>(list)).ToList(), edges[i], depth + 1, i, true);
                }
                if ((edges[i].ToVertex == position.FromVertex) && edges[i].Register == 0)
                {
                    isZero = true;
                    DFS_SECOND(edgess.Select(list => new List<float>(list)).ToList(), edges[i], depth + 1, i, false);
                }
            }

        }

        if (!isZero)
        {
            for (int i = 0; i < edges.Count; i++)
            {
                bool visited = false;
                for (int j = 0; j < edgess.Count; j++)
                {
                    if (edgess[j].Count > 0 && i == edgess[j][0])
                    {
                        visited = true;
                        break;
                    }
                }
                if (visited) continue;

                if (isDirect)
                {
                    // ������ ���
                    if (edges[i].FromVertex == position.ToVertex && edges[i] != position)
                    {
                        DFS_SECOND(edgess.Select(list => new List<float>(list)).ToList(), edges[i], depth + 1, i, true);
                    }
                    // ������ ���
                    if (edges[i].ToVertex == position.ToVertex && edges[i] != position)
                    {
                        DFS_SECOND(edgess.Select(list => new List<float>(list)).ToList(), edges[i], depth + 1, i, false);
                    }
                }
                else
                {
                    // ������ ���
                    if (edges[i].FromVertex == position.FromVertex && edges[i] != position)
                    {
                        DFS_SECOND(edgess.Select(list => new List<float>(list)).ToList(), edges[i], depth + 1, i, true);
                    }
                    // ������ ���
                    if (edges[i].ToVertex == position.FromVertex && edges[i] != position)
                    {
                        DFS_SECOND(edgess.Select(list => new List<float>(list)).ToList(), edges[i], depth + 1, i, false);
                    }
                }
            }
        }
    }

    //���� ���� ������ 0�̾��� �б����� ���� ����ؾ���(from, to �� �� �������)
    private void KCL()
    {

        for (int i = 0; i < edges.Count; i++)
        {
            if (polynomial >= edges.Count) return;
            //������ 0�̸� KCL�켱 input.
            if (edges[i].Register == 0)
            {
                List<int> zero = new List<int>() { i, 1000, 1000, 1000, 1, 0, 0, 0 }; //�տ� 4���� index, �ڿ� 4���� ������ : 1 / ������ : -1
                int cnt = 1;

                //�������� for��
                for (int j = 0; j < edges.Count; j++)
                {
                    if (edges[i].FromVertex == edges[j].ToVertex && i != j)//������ �����ϹǷ� ����ó��
                    {
                        zero[cnt + 4] = -1;
                        zero[cnt++] = j; //���������� �����Ƿ� +

                    }
                    else if (edges[i].FromVertex == edges[j].FromVertex && i != j)
                    {
                        zero[cnt + 4] = 1;
                        zero[cnt++] = j; //���������� �����Ƿ� -   
                    }

                }

                kcls.Add(zero);
                polynomial += 1;
            }
        }
        int havetodo = edges.Count - polynomial;
        for (int i = 0; i < edges.Count; i++)
        {
            if (havetodo == 0) break;
            //������ 0�̸� KCL�켱 input.
            if (edges[i].Register != 0)
            {
                List<int> zero = new List<int>() { i, 1000, 1000, 1000, 1, 0, 0, 0 }; //�տ� 4���� index, �ڿ� 4���� ������ : 1 / ������ : -1
                int cnt = 1;

                //�������� for��
                for (int j = 0; j < edges.Count; j++)
                {
                    if (edges[i].FromVertex == edges[j].ToVertex && i != j)//������ �����ϹǷ� ����ó��
                    {
                        zero[cnt + 4] = -1;
                        zero[cnt++] = j; //���������� �����Ƿ� +

                    }
                    else if (edges[i].FromVertex == edges[j].FromVertex && i != j)
                    {
                        zero[cnt + 4] = 1;
                        zero[cnt++] = j; //���������� �����Ƿ� -   
                    }

                }

                kcls.Add(zero);
                havetodo--;
            }
        }
    }


    private void metrix()
    {
        int rowCount = kvls.Count + kcls.Count; // �� ���� ����
        int colCount = edges.Count; // ���� ������ edges�� ������ŭ


        // kvls�� kcls�� ������ ��� (rowCount x colCount ũ��)
        double[,] matrixData = new double[rowCount, colCount];
        double[] voltageVector = new double[rowCount]; // ���� ����

        // kvls�� ��Ŀ� �ֱ� (���װ��� ���а��� �����ϴ� index�� ����)
        for (int i = 0; i < kvls.Count; i++)
        {
            foreach (var edgeData in kvls[i])
            {
                int edgeIndex = (int)edgeData[0]; // edge�� index
                double resistance = edgeData[1]; // ���װ�
                double voltage = edgeData[2]; // ���а�



                matrixData[i, edgeIndex] = resistance; // ��Ŀ� ���װ� ����
                voltageVector[i] += voltage; // ���� ���Ϳ� ���а� ����


            }

        }

        // kcls�� ��Ŀ� �ֱ� (������ ����� ���θ� 0, 1, -1�� ǥ��)
        for (int i = 0; i < kcls.Count; i++)
        {
            for (int j = 0; j < 4; j++) // kcls �������� 4���� �ε���
            {
                int edgeIndex = kcls[i][j]; // edge�� index
                if (edgeIndex == 1000) continue; // �� �� ó��
                matrixData[kvls.Count + i, edgeIndex] = kcls[i][4 + j]; // ���⿡ ���� ��ȣ ���
            }
        }



        // ��� ������ ���� ���̺귯�� ��� (MathNet.Numerics)
        var matrix = MathNet.Numerics.LinearAlgebra.Matrix<double>.Build.DenseOfArray(matrixData);
        var voltageMatrix = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.Dense(voltageVector);

        print(matrix);
        print(voltageMatrix);

        // ���콺 �ҰŹ��� �̿��� �� ���ϱ�
        

        if (matrix.Determinant()==0)
        {
            HUD.errors();
            return;
        }

        var solution = matrix.Solve(voltageMatrix);

        // ��� ���
        Debug.Log("��: " + solution);


        Show(solution);
    }

    private void Show(MathNet.Numerics.LinearAlgebra.Vector<double> solution)
    {
        for (int i = 0; i < solution.Count; i++)
        {
            edges[i].current = solution[i];
        }

        for (int i = 0; i < solution.Count; i++)
        {
            for (int j = 1; j < edges[i].roads.Count - 1; j++)
            {
                calculated.Add(edges[i].roads[j], solution[i] > 0 ? solution[i] : -solution[i]);
            }
        }
    }
}
