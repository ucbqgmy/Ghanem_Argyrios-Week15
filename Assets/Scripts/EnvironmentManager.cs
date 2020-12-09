using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using QuickGraph;
using QuickGraph.Algorithms;

public class EnvironmentManager : MonoBehaviour
{
    #region Private fields

    VoxelGrid _voxelGrid;
    List<Voxel> _path = new List<Voxel>();
    List<Voxel> _flow = new List<Voxel>();
    //45 Create list to stores target voxels
    List<GraphVoxel> _targets = new List<GraphVoxel>();
    UndirectedGraph<GraphVoxel, TaggedEdge<GraphVoxel, Face>> _graph;

    #endregion

    #region Unity methods

    public void Start()
    {
        //01 Create a basic VoxelGrid
        _voxelGrid = new VoxelGrid(new Vector3Int(20, 1, 20), transform.position, 1f);

    }

    public void Update()
    {
        //46.1 Cast ray clicking mouse
        if (Input.GetMouseButtonDown(0))
        {
            SetClickedAsTarget();
        }
    }

    #endregion

    #region Public methods


    public void CreateGraphWithObstacles()
    {
        //63 Create a list to store all faces of the graph in the grid
        List<Face> faces = new List<Face>();

        //64 Iterate through all the faces in the grid
        foreach (var face in _voxelGrid.GetFaces())
        {
            //65 Get the voxels associated with this face
            GraphVoxel voxelA = (GraphVoxel)face.Voxels[0];
            GraphVoxel voxelB = (GraphVoxel)face.Voxels[1];

            //66 Check if both voxels exist, are not obstacle and are active
            if (voxelA != null && !voxelA.IsVoid && voxelA.IsActive && !voxelA.IsObstacle &&
                voxelB != null && !voxelB.IsVoid && voxelB.IsActive && !voxelB.IsObstacle)
            {
                //67 Add face to list
                faces.Add(face);
            }
            else Debug.LogWarning("Hooray");
        }

        //68 Create the edges from the graph using the faces (the voxels are the vertices)
        var graphEdges = faces.Select(f => new TaggedEdge<GraphVoxel, Face>((GraphVoxel)f.Voxels[0], (GraphVoxel)f.Voxels[1], f));

        //69 Create the undirected graph from the edges
        _graph = graphEdges.ToUndirectedGraph<GraphVoxel, TaggedEdge<GraphVoxel, Face>>();
    }

    public void SetPathDirection()
    {
        for (int i = 0; i < _path.Count; i++)
        {
            if (i == 1) _path[i].Direction = _path[i + 1].Index - _path[i].Index;
            else _path[i].Direction = _path[i - 1].Index - _path[i].Index;
        }
        _flow = new List<Voxel>(_path);
    }
    public Voxel NextVoxel()
    {
        List<Voxel> possibleNeighbours = new List<Voxel>();
        foreach (var voxel in _flow)
        {
            possibleNeighbours.AddRange(voxel.GetFaceNeighbours().Where(v => v.Direction == null));
        }

        possibleNeighbours = possibleNeighbours.Distinct().ToList();
        int index = Random.Range(0, possibleNeighbours.Count);
        return possibleNeighbours[index];
    }

    public void FlowStep()
    {
        Voxel nextVoxel = NextVoxel();

        List<Voxel> flowNeighbours = nextVoxel.GetFaceNeighbours().Where(v => v.Direction != null).ToList();
        List<Vector3Int> directions = flowNeighbours.Select(v => v.Direction).ToList();
        Vector3 direction = Vector3Int.zero;
        foreach (var dir in directions)
        {
            direction += dir;
        }

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z)) direction.z = 0;
        if (Mathf.Abs(direction.z) > Mathf.Abs(direction.x)) direction.x = 0;
        nextVoxel.Direction = direction.normalized.ToVector3IntRound();
        //Find logic for when the direction is 0,0,0

        nextVoxel.ShowDirection();
        _flow.Add(nextVoxel);
    }

    void ReplaceParts()
    {
        while (_voxelGrid.GetVoxels().Where(v => v.Defined == false).Count() > 0)
        {
            Voxel undefinedVoxel = _voxelGrid.GetVoxels().First(v => v.Defined == false);

            bool continuous = true;
            Vector3Int currentIndex = undefinedVoxel.Index;
            int counter = 0;

            while (continuous)
            {
                Vector3Int nextIndex = currentIndex + undefinedVoxel.Direction;
                //first check if next index is within bounds of the grid
                //check if voxel on nextIndex is still undefined
                if (_voxelGrid.GetVoxelByIndex(nextIndex).Direction == undefinedVoxel.Direction)
                {
                    counter++;
                }
                else continuous = false;
            }

            //Get list of blocklengths out of your pattern list
            //loop over blocklengths, big to small
                //if blocklength < counter
                    //place block with undefinedVoxel as anhor and undefinedVoxel direction as direction
                        //In your blockclass in placeblock: set all voxel to undefined == false
        }
    }
    //62 Create the method to calculate the shortest path
    public void FindShortestPath()
    {
        //70 Iterate through all the targets, starting at index 1
        for (int i = 1; i < _targets.Count; i++)
        {
            //71 Define the start vertex of this path
            var start = _targets[i - 1];

            //72 Set next target as the end of the path
            var end = _targets[i];

            //73 Construct the Shortest Path graph, unweighted
            var shortest = _graph.ShortestPathsDijkstra(e => 1.0, start);

            //74 Calculate the shortest path, if such one is possible
            if (shortest(end, out var endPath))
            {
                //75 Read the path as a list of GraphVoxels
                var endPathVoxels = new List<GraphVoxel>(endPath.SelectMany(e => new[] { e.Source, e.Target }));

                //76 Set each GraphVoxel as path
                foreach (var pathVoxel in endPathVoxels)
                {
                    //81 Set as path
                    pathVoxel.SetAsPath();
                }
            }
            //82 Throw exception if path could not be found
            else
            {
                throw new System.Exception($"No Path could be found!");
            }

        }
    }

    //85 Create public method to start coroutine
    /// <summary>
    /// Start the animation of the Shortest path algorithm
    /// </summary>
    public void StartAnimation()
    {
        StartCoroutine(FindShortestPathAnimated());
    }

    #endregion

    #region Private methods

    //46 Create the method to set clicked voxel as target
    /// <summary>
    /// Cast a ray where the mouse pointer is, turning the selected voxel into a target
    /// </summary>
    private void SetClickedAsTarget()
    {
        //47 Cast ray from camer
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        //48 If ray hits something, continue
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Transform objectHit = hit.transform;

            //49 FIRST Compare tag of clicked object with VoidVoxel tag
            //60 SECOND Compare tag to TargetVoxel with ||
            if (objectHit.CompareTag("Voxel") || objectHit.CompareTag("TargetVoxel"))
            {
                //50 Read the name of the obeject and split it by _
                string[] name = objectHit.name.Split('_');

                //51 Construct index from split name
                int x = int.Parse(name[1]);
                int y = int.Parse(name[2]);
                int z = int.Parse(name[3]);
                Vector3Int index = new Vector3Int(x, y, z);

                //52 Retrieve voxel by index
                GraphVoxel voxel = (GraphVoxel)_voxelGrid.Voxels[index.x, index.y, index.z];

                //57 Set voxel as target and test
                voxel.SetAsTarget();

                //58 If voxel has be set as target, add it to _targets list
                if (voxel.IsTarget)
                {
                    _targets.Add(voxel);
                }
                //59 Else, remove it from _targets list
                else
                {
                    _targets.Remove(voxel);
                }
            }
        }
    }

    //83 Copy method to Ienumarator
    /// <summary>
    /// IEnumerator to animate the creation of the the shortest path algorithm
    /// </summary>
    /// <returns>Every step of the path</returns>
    private IEnumerator FindShortestPathAnimated()
    {
        for (int i = 1; i < _targets.Count; i++)
        {
            var start = _targets[i - 1];

            var shortest = _graph.ShortestPathsDijkstra(e => 1.0, start);

            var end = _targets[i];
            if (shortest(end, out var endPath))
            {
                var endPathVoxels = new HashSet<GraphVoxel>(endPath.SelectMany(e => new[] { e.Source, e.Target }));
                foreach (var pathVoxel in endPathVoxels)
                {
                    pathVoxel.SetAsPath();
                    _path.Add(pathVoxel);

                    //84 Yield return after setting voxel as path
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else
            {
                throw new System.Exception($"No Path could be found!");
            }

        }
    }

    /// <summary>
    /// Get the voxels that are part of a Path
    /// </summary>
    /// <returns>All the path <see cref="GraphVoxel"/></returns>
    private IEnumerable<GraphVoxel> GetPathVoxels()
    {
        foreach (GraphVoxel voxel in _voxelGrid.Voxels)
        {
            if (voxel.IsPath)
            {
                yield return voxel;
            }
        }
    }


    #endregion


}
