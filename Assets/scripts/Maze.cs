using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapLocation       
{
    public readonly int x;
    public readonly int z;

    public MapLocation(int inX, int inZ)
    {
        x = inX;
        z = inZ;
    }

    public Vector2 ToVector2()
    {
        return new Vector2(x, z);
    }

    public static MapLocation operator +(MapLocation a, MapLocation b) => new MapLocation(a.x + b.x, a.z + b.z);

    public override bool Equals(object obj)
    {
        if ((obj == null) || !(GetType() == obj.GetType()))
        {
            return false;
        }
        else
        {
            return (x == ((MapLocation)obj).x && z == ((MapLocation)obj).z);
        }
    }

    public override int GetHashCode()
    {
        return 0;
    }

}

public class Maze : MonoBehaviour
{
    public List<MapLocation> directions = new List<MapLocation>() {
                                            //Could be refactored to check diagonals. May do this in the future.
                                            //Clockwise from top left neighbor.
                                            // new MapLocation(-1,1),
                                            // new MapLocation(0,1),
                                            // new MapLocation(1,1),
                                            // new MapLocation(1,0),
                                            // new MapLocation(1,-1),
                                            // new MapLocation(0,-1),
                                            // new MapLocation(-1,-1),
                                            // new MapLocation(-1,0) };
                                            
                                            //Does not check diagonals.
                                            new MapLocation(1,0),
                                            new MapLocation(0,1),
                                            new MapLocation(-1,0),
                                            new MapLocation(0,-1) };
    public int width = 30; //x length
    public int depth = 30; //z length
    public byte[,] map;
    public int scale = 6;

    // Start is called before the first frame update
    private void Start()
    {
        InitialiseMap();
        Generate();
        DrawMap();
    }

    private void InitialiseMap()
    {
        map = new byte[width,depth];
        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                map[x, z] = 1; //1 = wall  0 = corridor
            }
        }
    }

    protected virtual void Generate()
    {
        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                if (Random.Range(0, 100) < 50)
                    map[x, z] = 0; //1 = wall  0 = corridor
            }
        }
    }

    void DrawMap()
    {
        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                if (map[x, z] == 1)
                {
                    Vector3 pos = new Vector3(x * scale, transform.position.y, z * scale);
                    GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.transform.localScale = new Vector3(scale, scale, scale);
                    wall.transform.position = pos;
                }
            }
        }
    }

    protected int CountSquareNeighbours(int x, int z)
    {
        int count = 0;
        if (x <= 0 || x >= width - 1 || z <= 0 || z >= depth - 1) return 5;
        if (map[x - 1, z] == 0) count++;
        if (map[x + 1, z] == 0) count++;
        if (map[x, z + 1] == 0) count++;
        if (map[x, z - 1] == 0) count++;
        return count;
    }

    private int CountDiagonalNeighbours(int x, int z)
    {
        int count = 0;
        if (x <= 0 || x >= width - 1 || z <= 0 || z >= depth - 1) return 5;
        if (map[x - 1, z - 1] == 0) count++;
        if (map[x + 1, z + 1] == 0) count++;
        if (map[x - 1, z + 1] == 0) count++;
        if (map[x + 1, z - 1] == 0) count++;
        return count;
    }

    protected int CountAllNeighbours(int x, int z)
    {
        return CountSquareNeighbours(x,z) + CountDiagonalNeighbours(x,z);
    }
}
