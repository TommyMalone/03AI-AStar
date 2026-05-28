using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PathMarker
{
    public readonly MapLocation location;
    public float g;
    public float h;
    public float f;
    public readonly GameObject marker;
    public PathMarker parent;
    
    public PathMarker(MapLocation inLocation, float inG, float inH, float inF, GameObject inMarker, PathMarker inParent)
    {
        location = inLocation;
        g = inG;
        h = inH;
        f = inF;
        marker = inMarker;
        parent = inParent;
    }

    public override bool Equals(object obj)
    {
        if ((obj == null) || !(GetType() == obj.GetType()))
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
    [FormerlySerializedAs("start")] public GameObject startPrefab;
    [FormerlySerializedAs("end")] public GameObject endPrefab;
    public GameObject pathMarker;
    public Material closedMaterial;
    public Material openMaterial;

    private List<PathMarker> _openSet = new List<PathMarker>();
    private List<PathMarker> _closedSet = new List<PathMarker>();
    private PathMarker _goalNode;
    private PathMarker _startNode;
    private PathMarker _lastMarkerEvaluated;
    private bool _done = false;

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
        GameObject[] markers = GameObject.FindGameObjectsWithTag($"marker");
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
        Vector3 endLocation = new Vector3(locations[1].x, 0, locations[1].z) * maze.scale;
        float startLocationHValue = Vector2.Distance( new Vector2(locations[0].x, locations[0].z), new Vector2(locations[1].x, locations[1].z));
        
        GameObject start = Instantiate(startPrefab, startLocation, Quaternion.identity);
        GameObject end = Instantiate(endPrefab, endLocation, Quaternion.identity);
        
        _startNode = new PathMarker(new MapLocation(locations[0].x, locations[0].z), 0, startLocationHValue, startLocationHValue, start, null);
        _goalNode = new PathMarker(new MapLocation(locations[1].x, locations[1].z), 0, 0, 0, end, null);
        
        _openSet.Clear();
        _closedSet.Clear();
        _openSet.Add(_startNode);
        _lastMarkerEvaluated = _startNode;
    }

    private void Search(PathMarker node)
    {
        if (node != null)
        {
            if (node.Equals(_goalNode))
            {
                _done = true;
                return;
            }
            
            _openSet.Remove(node);
            _closedSet.Add(node);
            if (!node.Equals(_startNode))
            {
                node.marker.GetComponent<Renderer>().material = closedMaterial;
            }
            TextMesh[] closedMarkerValues = node.marker.GetComponentsInChildren<TextMesh>();
            foreach (TextMesh closedMarkerValue in closedMarkerValues)
            {
                closedMarkerValue.GetComponent<MeshRenderer>().enabled = false;
            }

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

                        GameObject pathBlock = Instantiate(pathMarker,new Vector3(neighbor.x, 0, neighbor.z) * maze.scale, Quaternion.identity);

                        TextMesh[] values = pathBlock.GetComponentsInChildren<TextMesh>();
                        values[0].text = "g:" + g.ToString("0.00");
                        values[1].text = "h:" + h.ToString("0.00");
                        values[2].text = "f:" + f.ToString("0.00");

                        if (!UpdateMarker(neighbor, g, h, f, node))
                        {
                            _openSet.Add(new PathMarker(neighbor, g, h, f, pathBlock, node));
                        }
                    }
                }
            }
            //Order the set by f, then secondarily by h to put the best candidate at the front
            _openSet = _openSet.OrderBy(openPathMarker => openPathMarker.f).ThenBy(openPathMarker => openPathMarker.h).ToList<PathMarker>();
            
            _lastMarkerEvaluated = _openSet[0];
        }
    }

    private bool UpdateMarker(MapLocation position, float g, float h, float f, PathMarker parentMarker)
    {
        foreach (PathMarker openPathMarker in _openSet)
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

    private bool IsClosed(MapLocation marker)
    {
        foreach (PathMarker closedPathMarker in _closedSet)
        {
            if (closedPathMarker.location.Equals(marker))
            {
                return true;
            }
        }
        return false;
    }

    private void GetPathToLastEvaluatedMarker()
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
