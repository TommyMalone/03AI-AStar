using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.IO;
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
    
    public override int GetHashCode() => location.GetHashCode();
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

    private PathMarker _lastMarkerEvaluated;
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
        if (InputSystem.actions["Interact"].WasReleasedThisFrame() || InputSystem.actions["Jump"].IsPressed())
        {
            if (!_done)
            {
                Search(_lastMarkerEvaluated);
            }
            else
            {
                GetPathToLastEvaluatedMarker();
            }
        }
    }

    void RemoveAllOtherMarkers(List<GameObject> keptMarkers = null)
    {
        GameObject[] markers = GameObject.FindGameObjectsWithTag("marker");
        foreach (GameObject marker in markers)
        {
            if (keptMarkers == null || !keptMarkers.Contains(marker))
            {
                Destroy(marker);
            }
            
        }
    }

    void BeginSearch()
    {
        _done = false;
        RemoveAllOtherMarkers();
        
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
        Vector3 goalLocation = new Vector3(locations[1].x, 0, locations[1].z) * maze.scale;
        
        float startLocationHValue = Vector2.Distance( new Vector2(locations[0].x, locations[0].z), new Vector2(locations[1].x, locations[1].z));
        
        _startNode = new PathMarker(new MapLocation(locations[0].x, locations[0].z), 0, startLocationHValue, startLocationHValue,
            Instantiate(start, startLocation, Quaternion.identity), null);
        
        _goalNode = new PathMarker(new MapLocation(locations[1].x, locations[1].z), 0, 0, 0,
            Instantiate(end, goalLocation, Quaternion.identity), null);
        
        openSet.Clear();
        closedSet.Clear();
        openSet.Add(_startNode);
        _lastMarkerEvaluated = _startNode;
    }

    void Search(PathMarker node)
    {
        if (node != null)
        {
            if (node.Equals(_goalNode))
            {
                _done = true;
                return;
            }
            
            openSet.Remove(node);
            closedSet.Add(node);
            node.marker.GetComponent<Renderer>().material = closedMaterial;

            foreach (MapLocation direction in maze.directions)
            {
                MapLocation neighbor = direction + node.location;

                //Caching conditions in bools for legibility
                bool xInBounds = (neighbor.x > 0 && neighbor.x < maze.width);
                bool zInBounds = (neighbor.z > 0 && neighbor.z < maze.depth);
                bool notInClosedSet = !IsClosed(neighbor);
                
                if (xInBounds && zInBounds && notInClosedSet)
                {
                    //This check must be done after evaluating that we are in bounds to avoid out of bounds errors.
                    bool isNotWall = maze.map[neighbor.x, neighbor.z] != 1;
                    
                    if (isNotWall)
                    {
                        float g = Vector2.Distance(node.location.ToVector2(), neighbor.ToVector2()) + node.g;
                        float h = Vector2.Distance(neighbor.ToVector2(), _goalNode.location.ToVector2());
                        float f = g + h;

                        GameObject pathBlock = Instantiate(pathMarker,
                            new Vector3(neighbor.x, 0, neighbor.z) * maze.scale,
                            Quaternion.identity);

                        TextMesh[] values = pathBlock.GetComponentsInChildren<TextMesh>();
                        values[0].text = "g:" + g.ToString("0.00");
                        values[1].text = "h:" + h.ToString("0.00");
                        values[2].text = "f:" + f.ToString("0.00");

                        if (!UpdateMarker(neighbor, g, h, f, node))
                        {
                            openSet.Add(new PathMarker(neighbor, g, h, f, pathBlock, node));
                        }
                    }
                }
            }

            //Order the set by f, then secondarily by h to put the best candidate at the front
            openSet = openSet.OrderBy(openPathMarker => openPathMarker.f).ThenBy(openPathMarker => openPathMarker.h)
                .ToList<PathMarker>();
            
            _lastMarkerEvaluated = openSet[0];
        }
    }

    bool UpdateMarker(MapLocation position, float g, float h, float f, PathMarker parentMarker)
    {
        foreach (PathMarker openPathMarker in openSet)
        {
            if (openPathMarker.location.Equals(position))
            {
                openPathMarker.g = g;
                openPathMarker.h = h;
                openPathMarker.f = f;
                openPathMarker.parent = parentMarker;
                return true;
            }
        }
        return false;
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

    void GetPathToLastEvaluatedMarker()
    {
        List<GameObject> path = new List<GameObject>();
        PathMarker currentMarker = _lastMarkerEvaluated;
        while (currentMarker != null)
        {
            path.Add(currentMarker.marker);
            currentMarker = currentMarker.parent;
        }
        path.Add(_startNode.marker);
        RemoveAllOtherMarkers(path);
    }
}
