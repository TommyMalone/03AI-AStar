using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class PathMarker
{
    public MapLocation location;
    public float g;
    public float h;
    public float f;
    public GameObject marker;
    public PathMarker parent;
    
    public PathMarker(MapLocation location, float g, float h, float f, GameObject marker, PathMarker parent)
    {
        this.location = location;
        this.g = g;
        this.h = h;
        this.f = f;
        this.marker = marker;
        this.parent = parent;
    }

    public override bool Equals(object obj)
    {
        if ((obj == null) || !this.GetType().Equals(obj.GetType()))
        {
            return false;
        }
        else
        {
            return location.Equals(((PathMarker)obj).location);
        }
    }
    
    public override int GetHashCode()
    {
        return 0;
    }
}

public class FindPathAStar : MonoBehaviour
{
    public Maze maze;

    public Material closedMaterial;
    public Material openMaterial;

    private List<PathMarker> openSet = new List<PathMarker>();
    private List<PathMarker> closedSet = new List<PathMarker>();

    public GameObject start;
    public GameObject end;
    public GameObject pathMarker;

    private PathMarker _goalNode;
    private PathMarker _startNode;

    private PathMarker _lastPos;
    private bool _done = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (InputSystem.actions["Attack"].WasReleasedThisFrame())
        {
            BeginSearch();
        }
    }

    void RemoveAllMarkers()
    {
        GameObject[] markers = GameObject.FindGameObjectsWithTag("marker");
        foreach (var marker in markers)
        {
            Destroy(marker);
        }
    }

    void BeginSearch()
    {
        _done = false;
        RemoveAllMarkers();
        
        //Acquire list of valid locations in the maze.
        List<MapLocation> locations = new List<MapLocation>();
        for (int z = 1; z < maze.depth - 1; z++)
        {
            for (int x = 1; x < maze.width - 1; x++)
            {
                // In Maze implementation, a value of 1 is a wall.
                if(maze.map[x,z] != 1)
                {
                    locations.Add(new MapLocation(x, z));
                }
            }
            locations.Shuffle();
        }

        Vector3 startLocation = new Vector3(locations[0].x, 0, locations[0].z) * maze.scale;
        _startNode = new PathMarker(new MapLocation((int)(startLocation.x), (int)(startLocation.z)), 0, 0, 0,
            Instantiate(start, startLocation, Quaternion.identity), null);
        Vector3 goalLocation = new Vector3(locations[1].x, 0, locations[1].z) * maze.scale;
        _goalNode = new PathMarker(new MapLocation((int)(goalLocation.x), (int)(goalLocation.z)), 0, 0, 0,
            Instantiate(end, goalLocation, Quaternion.identity), null);
        
        openSet.Clear();
        closedSet.Clear();
        openSet.Add(_startNode);
        _lastPos = _startNode;
    }

    void Search(PathMarker node)
    {
        if (node.Equals(_goalNode))
        {
            _done = true;
            return;
        }

        foreach (MapLocation direction in maze.directions)
        {
            MapLocation neighbor = direction + node.location;
            
            if (maze.map[neighbor.x, neighbor.z] != 1 && (neighbor.x < 1 && neighbor.x >= maze.width) && (neighbor.z < 1 && neighbor.z >= maze.depth) && !IsClosed(neighbor))
            {
                float g = Vector2.Distance(node.location.ToVector2(), neighbor.ToVector2()) + node.g;
                float h = Vector2.Distance(neighbor.ToVector2(), _goalNode.location.ToVector2());
                float f = g + h;

                GameObject pathBlock = Instantiate(pathMarker, new Vector3(neighbor.x, 0, neighbor.z) * maze.scale, Quaternion.identity);
            }
            
        }
    }

    bool IsClosed(MapLocation marker)
    {
        foreach (PathMarker pathMarker in closedSet)
        {
            if (pathMarker.location.Equals(marker))
            {
                return true;
            }
        }
        return false;
    }
}
