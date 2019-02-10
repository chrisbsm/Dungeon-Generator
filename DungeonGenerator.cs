using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Cinemachine;

public class DungeonGenerator : MonoBehaviour {
    //Singleton Reference
    public static DungeonGenerator Singleton;
    //User Variables
    [Tooltip("Cinemachine Camera")]
    public CinemachineVirtualCamera vCam;
    [Tooltip("Loading Pannel")]
    public LoadingMessage LoadingPannel;
    [Tooltip("MiniMapPrefab")]
    public GameObject miniMapPrefab;
    [Tooltip("player Prefab")]
    public GameObject player;
    [Tooltip("Ground Prefab")]
    public GameObject ground;
    [Tooltip("Stairs Prefab")]
    public GameObject stairs;
    [Tooltip("Wall Prefabs; 0 SimpleWall, 1 Corner, 2 DeadEnd,3 Block, 4 Corridor")]
    public GameObject[] WallModel;
    [Tooltip("Defines how manny main paths your map will have.")]
    [SerializeField, Range(1, 10)] int Diggers = 3;
    [Tooltip("Length of Corridors, Min and Max Values ")]
    public Vector2Int CorridorSize;
    [Tooltip("Main path Length of each Digger, Min and Max Values ")]
    public Vector2Int Steps;
    [Tooltip("Width and Heigth of rooms, created at end of Each main patch; Min and Max Values ")]
    public Vector2Int RoomSize;
    [Tooltip("Percent to create a room on Middle of a main Path")]
    [Range(0, 1)] public float MiddleRoom;
    [Tooltip("Size of the Prefabs used")]
    public float StepSize;
    [Tooltip("Chance To turn or Exted the corridor, Low numbers Generate more Linear Maps ")]
    [Range(0, 1)] public float turnChance;

    //Intern Variables
    [HideInInspector] public List<Digger> DiggersList;
    [HideInInspector] public Transform GroundParent;
    [HideInInspector] public int[,] map;
    [HideInInspector] List<Vector3> importantLocations;
    [HideInInspector] List<Transform> DungeonPieces;
    float TimeEnd;
    bool end, test;
    int center;

    //Functions
    
    private void Awake()
    {
        Singleton = this;
    }

    public void Start()
    {
        LoadingPannel.gameObject.SetActive(true);
        GroundParent = new GameObject().transform;
        GroundParent.parent = this.transform;
        GroundParent.name = "Ground";
        importantLocations = new List<Vector3>();
        //Random.InitState(17);
        //instantiate and Initialize Diggers
        DiggersList = new List<Digger>();
        for (int i = 0; i < Diggers; i++)
        {
            GameObject o = new GameObject();
            o.name = "Digger" + i;
            DiggersList.Add(o.AddComponent<Digger>());
            DiggersList[DiggersList.Count - 1].Dungeon = this;
            DiggersList[DiggersList.Count - 1].Initialize();
        }
    }

    public void FixedUpdate()
    {
        //Move The diggers While exists at least one Digger alive.
        if (DiggersList.Count > 0)
        {
            for (int i = 0; i < DiggersList.Count; i++)
            {
                DiggersList[i].Move();
            }
        }

        else if (!end)
        {
            end = true;
            InitiateMatrix();
            CreateDungeon();
            CreatePlayer();
        }
        else if (!test)
        {
            test = true;
            InstantiateImportantObjects();
            LoadingPannel.EndLoading();
        }
    }
    //Create the Map Matrix
    void InitiateMatrix()
    {
        float size = 0;
        //Check Size of map
        for (int i = 0; i < GroundParent.childCount; i++)
        {
            float x = Mathf.Abs(GroundParent.GetChild(i).transform.position.x);
            float z = Mathf.Abs(GroundParent.GetChild(i).transform.position.z);
            if (size < x) size = x;
            if (size < z) size = z;
        }
        size += 5;
        center = (int)size;
        size = size * 2;
        //set ground values
        map = new int[(int)size, (int)size];
        for (int i = 0; i < GroundParent.childCount; i++)
        {
            int x = (int)Mathf.Floor(GroundParent.GetChild(i).transform.position.x / StepSize);
            int z = (int)Mathf.Floor(GroundParent.GetChild(i).transform.position.z / StepSize);
            map[center + x, center + z] = 1;
        }
        //set wall values
        Debug.Log("center " + center);
        Debug.Log("size " + size);
        for (int i = 1; i < size - 2; i++)
        {
            for (int j = 1; j < size - 2; j++)
            {
                if (map[i, j] == 0)
                {
                    if (checkWall(i, j)) map[i, j] = 2;
                }
            }
        }
        //end
        Debug.Log("end matrix");


    }

    //Check if are nessessary put a Wall on index[i,j]
    bool checkWall(int i, int j)
    {
        if (map[i - 1, j - 1] == 1) return true;
        else if (map[i - 1, j] == 1) return true;
        else if (map[i - 1, j + 1] == 1) return true;
        else if (map[i, j - 1] == 1) return true;
        else if (map[i, j + 1] == 1) return true;
        else if (map[i + 1, j - 1] == 1) return true;
        else if (map[i + 1, j] == 1) return true;
        else if (map[i + 1, j + 1] == 1) return true;
        else return false;
    }

    //Create the Optimized Dungeon
    void CreateDungeon()
    {
        //Remove Temp Objects
        for (int i = 0; i < GroundParent.childCount; i++)
        {
            Destroy(GroundParent.GetChild(i).gameObject);
        }

        int center = map.GetLength(0) / 2;
        DungeonPieces = new List<Transform>();
        //Create the Dungeon Based on the Binary Matrix 
        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                //Create Ground
                if (map[i, j] == 1)
                {
                    GameObject G = (GameObject)Instantiate(ground, new Vector3((i - center) * StepSize, 0, (j - center) * StepSize), transform.rotation, GroundParent);
                    DungeonPieces.Add(G.transform);
                }
                //Create Wall
                else if (map[i, j] == 2)
                {
                    GameObject W = (GameObject)Instantiate(ground, new Vector3((i - center) * StepSize, 0, (j - center) * StepSize), transform.rotation, GroundParent);
                    DungeonPieces.Add(W.transform);
                    Wall NewWall = W.AddComponent<Wall>();
                    NewWall.index = new Vector2Int(i, j);
                    NewWall.Dungeon = this;
                    NewWall.SetModel();
                }
            }
        }


    }

    public void addLocation(Vector3 location)
    {
        importantLocations.Add(location);
    }
    void CreatePlayer()
    {
        Vector3 position = importantLocations[Random.Range(0, importantLocations.Count)];
        importantLocations.Remove(position);
        player = (GameObject)Instantiate(player, position, transform.rotation);
        vCam.Follow = player.transform;
        player.GetComponent<TestPlayer>().Initialize(this);

    }
    //Select a random "important location" (end of digger's path or rooms) to spaw important objecs, like stairs, keys and chests
    void InstantiateImportantObjects()
    {
        Vector3 position = importantLocations[Random.Range(0, importantLocations.Count)];
        importantLocations.Remove(position);
        GameObject oldPiece = GetPieceAtPosition(position);
        GameObject NewPiece = (GameObject)Instantiate(stairs, oldPiece.transform.position, oldPiece.transform.rotation);
        Destroy(oldPiece);
        DungeonPieces.Add(NewPiece.transform);
    }

    //Return the tile piece at chosen position
    public GameObject GetPieceAtPosition(Vector3 position)
    {
        Ray R = new Ray(position + new Vector3(0, StepSize * 2, 0), new Vector3(0, -1, 0));

        RaycastHit hit;
        if (Physics.Raycast(R, out hit, StepSize * 3))
        {
            return hit.transform.gameObject;

        }
        else return null;
    }


    public bool CheckValidMove(Vector2Int index, int direction)
    {
        int x = index.x;
        int z = index.y;
        if (direction > -1 && direction < 4)
        {
            if (direction == 0) z += 1;
            else if (direction == 1) x += 1;
            else if (direction == 2) z -= 1;
            else x -= 1;
            if (x > 0 && z > 0 && x < map.GetLength(0) && z < map.GetLength(1))
            {
                if (map[x, z] == 1) return true;
                else return false;
            }
            else return false;
        }
        else return false;
    }

    //convert a 3D position into index of map matrix
    public Vector2Int positionToIndex(Vector3 position)
    {
        int x = (int)Mathf.Floor(position.x / StepSize);
        int z = (int)Mathf.Floor(position.z / StepSize);
        Vector2Int index = new Vector2Int(center + x, center + z);
        return index;
    }

    //convert a index on a 3D position
    public Vector3 indexToPositon(Vector2Int index)
    {
        return new Vector3((index.x - center) * StepSize, 0, (index.y - center) * StepSize);
    }
}

public class Digger : MonoBehaviour
{
    public int direction; // 0 Forward, 1Rigth, 2 Back , 3 Left
    public int StepsLeft, CorridorSteps;
    int MiddleRoomAux = -10;
    Vector3 MiddleRoom;
    Vector3 MoveVector;
    public DungeonGenerator Dungeon;

    public Digger(DungeonGenerator D)
    {
        Dungeon = D;
        StepsLeft = Random.Range(Dungeon.Steps.x, Dungeon.Steps.y + 1);
        CorridorSteps = Random.Range(Dungeon.CorridorSize.x, Dungeon.CorridorSize.y + 1);
    }

    public void Initialize()
    {
        StepsLeft = Random.Range(Dungeon.Steps.x, Dungeon.Steps.y + 1);
        CorridorSteps = Random.Range(Dungeon.CorridorSize.x, Dungeon.CorridorSize.y + 1);
        if (Random.value <= Dungeon.MiddleRoom) MiddleRoomAux = StepsLeft / 2;
    }
    void Place()
    {
        Instantiate(Dungeon.ground, transform.position, transform.rotation, Dungeon.GroundParent.transform);

    }
    public void Move()
    {
        if (StepsLeft > 0)
        {

            transform.position += MoveVector * Dungeon.StepSize;
            StepsLeft--;
            CorridorSteps--;
            Place();
            if (CorridorSteps == 0)
            {
                SetDirection();
            }
            if(StepsLeft==MiddleRoomAux)
            {
                MiddleRoom = transform.position;
            }
            
        }
        else if (StepsLeft == 0)
        {
            int w = Random.Range(Dungeon.RoomSize.x, Dungeon.RoomSize.y);
            int h = Random.Range(Dungeon.RoomSize.x, Dungeon.RoomSize.y);
            Dungeon.addLocation(transform.position);
            CreateRoom(w, h);
            Dungeon.DiggersList.Remove(this);
            
            if(MiddleRoom != Vector3.zero)
            {
                transform.position = MiddleRoom;
                Dungeon.addLocation(transform.position);
                w = Random.Range(Dungeon.RoomSize.x, Dungeon.RoomSize.y);
                h = Random.Range(Dungeon.RoomSize.x, Dungeon.RoomSize.y);
                CreateRoom(w, h);
            }

            Destroy(this.gameObject);
        }
    }
    public void SetDirection()
    {
        CorridorSteps = Random.Range(Dungeon.CorridorSize.x, Dungeon.CorridorSize.y + 1);
        if (direction == 0) MoveVector = transform.forward;
        else if (direction == 1) MoveVector = -transform.forward;
        else if (direction == 2) MoveVector = transform.right;
        else if (direction == 3) MoveVector = -transform.right;
        if (Random.value < Dungeon.turnChance)
        {
            if (Random.value < 0.5f)
            {
                direction--;
                if (direction < 0) direction = 3;
            }
            else
            {
                direction++;
                if (direction > 3) direction = 0;
            }

        }

    }
    void CreateRoom(int x, int y)
    {
        StepsLeft = -1;
        transform.position -= new Vector3((int)(x / 2) * Dungeon.StepSize, 0, (int)(y / 2) * Dungeon.StepSize);
        Vector3 reference = transform.position;
        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y; j++)
            {
                transform.position = reference + new Vector3(i, 0, j);
                Place();
            }
        }
    }


}

public class Wall : MonoBehaviour
{
    public Vector2Int index;
    public int GroundNear;
    public DungeonGenerator Dungeon;
    bool u, r, d, l;

    public void SetModel()
    {
        GroundNear = checkGroundNearby();
        if (GroundNear == 1)
        {
            if (l) transform.Rotate(0, -90, 0);
            else if (r) transform.Rotate(0, 90, 0);
            else if (u) transform.Rotate(0, 180, 0);
            GameObject W = Instantiate(Dungeon.WallModel[0], transform.position, transform.rotation, transform.parent);
        }
        if (GroundNear == 2)
        {
            if (l && r || u && d)
            {
                if (u) transform.Rotate(0, 90, 0);
                GameObject W = Instantiate(Dungeon.WallModel[4], transform.position, transform.rotation, transform.parent);
            }
            else
            {
                if (l && u) transform.Rotate(0, -90, 0);
                else if (u && r) transform.Rotate(0, 180, 0);
                else if (r && d) transform.Rotate(0, 90, 0);
                GameObject W = Instantiate(Dungeon.WallModel[1], transform.position, transform.rotation, transform.parent);
            }
        }
        else if (GroundNear == 3)
        {
            if (!l) transform.Rotate(0, -90, 0);
            else if (!r) transform.Rotate(0, 90, 0);
            else if (!u) transform.Rotate(0, 180, 0);
            GameObject W = Instantiate(Dungeon.WallModel[2], transform.position, transform.rotation, transform.parent);
        }
        else if (GroundNear == 4) Instantiate(Dungeon.WallModel[3], transform.position, transform.rotation, transform.parent);


        Destroy(this.gameObject);
    }

    int checkGroundNearby()
    {
        int i = index.x;
        int j = index.y;
        int n = 0;

        if (Dungeon.map[i - 1, j] == 1)
        {
            n++;
            l = true;
        }
        if (Dungeon.map[i, j - 1] == 1)
        {
            n++;
            u = true;
        }
        if (Dungeon.map[i, j + 1] == 1)
        {
            n++;
            d = true;
        }
        if (Dungeon.map[i + 1, j] == 1)
        {
            n++;
            r = true;
        }

        return n;
    }


}


