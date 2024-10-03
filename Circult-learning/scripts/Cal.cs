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
    public Vector3Int FromVertex { get; set; }  // 출발 정점
    public Vector3Int ToVertex { get; set; }    // 도착 정점
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
    밑에 세 함수는 원래 문법 override해서 수정하는 작업이라 계산에는 크게 중요하지 않음.
    Dictionary에 내가 만든 class 자료형을 사용하기 위한 밑작업임.
    참고만 하기 바람.
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

    // Edge 정보를 문자열로 표현
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

    //HashSet 사용하려 하였으나 순서가 중요하여 List의 형태로 나타냄.
    List<Edge> edges = new List<Edge>();
    List<Vector3Int> zeros = new List<Vector3Int>();
    List<List<List<float>>> kvls = new List<List<List<float>>>();
    List<List<int>> kcls = new List<List<int>>();


    public static Dictionary<Vector3Int, double> calculated = new Dictionary<Vector3Int, double>();

    int count = 0;
    bool started = false;

    int polynomial = 0; //다항식이라는 뜻임.

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
        start = new Vector3Int(); // start 초기화
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

        // 초기화
        //try
        //{ 
            clearation();
            // 분기점 계산
            checkSeperate();
            // DFS 탐색
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

        //HashSet 사용하려 하였으나 순서가 중요하여 List의 형태로 나타냄.
        edges = new List<Edge>();
        zeros = new List<Vector3Int>() { };
        kvls = new List<List<List<float>>>() { };
        kcls = new List<List<int>>() { };

        calculated = new Dictionary<Vector3Int, double>();

        count = 0;
        started = false;
        polynomial = 0; //다항식이라는 뜻임.
    }


    //분기점 추출
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

    //분기점 판단
    //check adjacent tiles
    private int HasAdjacentTiles(Vector3Int position)
    {
        int cnt = 0;
        Vector3Int[] adjacentPositions = new Vector3Int[]
        {
            new Vector3Int(position.x + 1, position.y, position.z),  // 오른쪽
            new Vector3Int(position.x - 1, position.y, position.z),  // 왼쪽
            new Vector3Int(position.x, position.y + 1, position.z),  // 위
            new Vector3Int(position.x, position.y - 1, position.z)   // 아래
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
        // 분기점에 도착한 경우, 간선과 가중치를 저장
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
            sapr = position; // 분기점으로 새로 설정
            register = 0;
            voltage = 0;

        }
        sep.Add(position);
        //data를 사용하지 않고 이걸 사용하는 이유는 preview 때문임.
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


        // 네 방향으로 DFS 탐색
        Vector3Int[] adjacentPositions = new Vector3Int[]
        {
        new Vector3Int(position.x + 1, position.y, position.z),  // 오른쪽
        new Vector3Int(position.x - 1, position.y, position.z),  // 왼쪽
        new Vector3Int(position.x, position.y + 1, position.z),  // 위
        new Vector3Int(position.x, position.y - 1, position.z)   // 아래
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
                // 방문하지 않은 인접 타일로 DFS 재귀 호출
                DFS_FIRST(adjacentPosition, sapr, register, voltage, i, new HashSet<Vector3Int>(sep), position, new List<Vector3Int>(road));
            }
        }
    }





    //seconde DFS()
    //first랑 거의 비슷하지만 first에서 구했던 전류 방향을 고려하여 지나갈 때의 voltage와 register를 합산해 주어야 함.
    //first에서 구했던 전류에 index를 부여하여 계산하면 될 듯 함. : edge에 부여하는 것.
    //전선을 다 안갈것임. : 저장된 edge를 가지고 시작지점 edge에서 특정 방향으로 시작하여 DFS로 모든 방향을 돌아 다시 원점으로 돌아왔을 때 그 식을 저장할 것임.
    //이 과정에서 저항이 0인 상황을 고려하여 함께 프로그래밍 할 수 있도록 할 것임.
    private void DFS_SECOND(List<List<float>> edgess, Edge position, int depth, int index, bool isDirect)
    {

        // 새로운 리스트 추가 및 값 설정
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
            // 값 저장
            polynomial++;
            kvls.Add(edgess);  // edgess를 kvls에 추가
            return;
        }


        ///!!!!!!!!!!! isDirect에 따라서 position.to인지 position.from인지 달라짐 수정 해야함.
        bool isZero = false;
        for (int i = 0; i < edges.Count; i++)
        {
            bool visited = false;
            for (int j = 0; j < edgess.Count; j++)
            {
                if (edgess[j].Count > 0 && i == edgess[j][0])
                {
                    visited = true;
                    break; // 이미 방문한 곳 예외 처리
                }
            }
            if (visited) continue;
            if (isDirect)
            {
                if (edges[i].FromVertex == position.ToVertex && edges[i].Register == 0)
                {
                    isZero = true;
                    //if(!isZero) kcls_zero.Add(new List<int> { 0, 0, 0, 0 }); //0 : input, 1-3 : output //필요없어 보임.
                    DFS_SECOND(edgess.Select(list => new List<float>(list)).ToList(), edges[i], depth + 1, i, true);
                }
                if (edges[i].ToVertex == position.ToVertex && edges[i].Register == 0)
                {
                    isZero = true;
                    DFS_SECOND(edgess.Select(list => new List<float>(list)).ToList(), edges[i], depth + 1, i, false);
                }
            }
            else //반대방향
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
                    // 정방향 고려
                    if (edges[i].FromVertex == position.ToVertex && edges[i] != position)
                    {
                        DFS_SECOND(edgess.Select(list => new List<float>(list)).ToList(), edges[i], depth + 1, i, true);
                    }
                    // 역방향 고려
                    if (edges[i].ToVertex == position.ToVertex && edges[i] != position)
                    {
                        DFS_SECOND(edgess.Select(list => new List<float>(list)).ToList(), edges[i], depth + 1, i, false);
                    }
                }
                else
                {
                    // 정방향 고려
                    if (edges[i].FromVertex == position.FromVertex && edges[i] != position)
                    {
                        DFS_SECOND(edgess.Select(list => new List<float>(list)).ToList(), edges[i], depth + 1, i, true);
                    }
                    // 역방향 고려
                    if (edges[i].ToVertex == position.FromVertex && edges[i] != position)
                    {
                        DFS_SECOND(edgess.Select(list => new List<float>(list)).ToList(), edges[i], depth + 1, i, false);
                    }
                }
            }
        }
    }

    //가장 먼저 저항이 0이었던 분기점들 먼저 고려해야함(from, to 둘 다 해줘야함)
    private void KCL()
    {

        for (int i = 0; i < edges.Count; i++)
        {
            if (polynomial >= edges.Count) return;
            //저항이 0이면 KCL우선 input.
            if (edges[i].Register == 0)
            {
                List<int> zero = new List<int>() { i, 1000, 1000, 1000, 1, 0, 0, 0 }; //앞에 4개는 index, 뒤에 4개는 정방향 : 1 / 역방향 : -1
                int cnt = 1;

                //시작지점 for문
                for (int j = 0; j < edges.Count; j++)
                {
                    if (edges[i].FromVertex == edges[j].ToVertex && i != j)//같으면 성립하므로 예외처리
                    {
                        zero[cnt + 4] = -1;
                        zero[cnt++] = j; //정방향으로 들어오므로 +

                    }
                    else if (edges[i].FromVertex == edges[j].FromVertex && i != j)
                    {
                        zero[cnt + 4] = 1;
                        zero[cnt++] = j; //역방향으로 들어오므로 -   
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
            //저항이 0이면 KCL우선 input.
            if (edges[i].Register != 0)
            {
                List<int> zero = new List<int>() { i, 1000, 1000, 1000, 1, 0, 0, 0 }; //앞에 4개는 index, 뒤에 4개는 정방향 : 1 / 역방향 : -1
                int cnt = 1;

                //시작지점 for문
                for (int j = 0; j < edges.Count; j++)
                {
                    if (edges[i].FromVertex == edges[j].ToVertex && i != j)//같으면 성립하므로 예외처리
                    {
                        zero[cnt + 4] = -1;
                        zero[cnt++] = j; //정방향으로 들어오므로 +

                    }
                    else if (edges[i].FromVertex == edges[j].FromVertex && i != j)
                    {
                        zero[cnt + 4] = 1;
                        zero[cnt++] = j; //역방향으로 들어오므로 -   
                    }

                }

                kcls.Add(zero);
                havetodo--;
            }
        }
    }


    private void metrix()
    {
        int rowCount = kvls.Count + kcls.Count; // 총 행의 개수
        int colCount = edges.Count; // 열의 개수는 edges의 개수만큼


        // kvls와 kcls를 결합할 행렬 (rowCount x colCount 크기)
        double[,] matrixData = new double[rowCount, colCount];
        double[] voltageVector = new double[rowCount]; // 전압 벡터

        // kvls를 행렬에 넣기 (저항값과 전압값을 대응하는 index에 넣음)
        for (int i = 0; i < kvls.Count; i++)
        {
            foreach (var edgeData in kvls[i])
            {
                int edgeIndex = (int)edgeData[0]; // edge의 index
                double resistance = edgeData[1]; // 저항값
                double voltage = edgeData[2]; // 전압값



                matrixData[i, edgeIndex] = resistance; // 행렬에 저항값 삽입
                voltageVector[i] += voltage; // 전압 벡터에 전압값 저장


            }

        }

        // kcls를 행렬에 넣기 (전류의 방향과 여부를 0, 1, -1로 표현)
        for (int i = 0; i < kcls.Count; i++)
        {
            for (int j = 0; j < 4; j++) // kcls 데이터의 4개의 인덱스
            {
                int edgeIndex = kcls[i][j]; // edge의 index
                if (edgeIndex == 1000) continue; // 빈 값 처리
                matrixData[kvls.Count + i, edgeIndex] = kcls[i][4 + j]; // 방향에 따른 부호 고려
            }
        }



        // 행렬 연산을 위한 라이브러리 사용 (MathNet.Numerics)
        var matrix = MathNet.Numerics.LinearAlgebra.Matrix<double>.Build.DenseOfArray(matrixData);
        var voltageMatrix = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.Dense(voltageVector);

        print(matrix);
        print(voltageMatrix);

        // 가우스 소거법을 이용한 해 구하기
        

        if (matrix.Determinant()==0)
        {
            HUD.errors();
            return;
        }

        var solution = matrix.Solve(voltageMatrix);

        // 결과 출력
        Debug.Log("해: " + solution);


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
