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

    //45 Create list to stores target voxels
    List<GraphVoxel> _targets = new List<GraphVoxel>();

    #endregion

    #region Unity methods

    public void Start()
    {
        //01 Create a basic VoxelGrid
        _voxelGrid = new VoxelGrid(new Vector3Int(10, 1, 10), transform.position, 1f);

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


    //62 Create the method to calculate the shortest path
    public void FindShortestPath()
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
            if (voxelA != null && !voxelA.IsVoid && voxelA.IsActive &&
                voxelB != null && !voxelB.IsVoid && voxelB.IsActive)
            {
                //67 Add face to list
                faces.Add(face);
            }
        }

        //68 Create the edges from the graph using the faces (the voxels are the vertices)
        var graphEdges = faces.Select(f => new TaggedEdge<GraphVoxel, Face>((GraphVoxel)f.Voxels[0], (GraphVoxel)f.Voxels[1], f));

        //69 Create the undirected graph from the edges
        var graph = graphEdges.ToUndirectedGraph<GraphVoxel, TaggedEdge<GraphVoxel, Face>>();

        //70 Iterate through all the targets, starting at index 1
        for (int i = 1; i < _targets.Count; i++)
        {
            //71 Define the start vertex of this path
            var start = _targets[i - 1];

            //72 Set next target as the end of the path
            var end = _targets[i];

            //73 Construct the Shortest Path graph, unweighted
            var shortest = graph.ShortestPathsDijkstra(e => 1.0, start);

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
            if (objectHit.CompareTag("ObstacleVoxel") || objectHit.CompareTag("TargetVoxel"))
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
        List<Face> faces = new List<Face>();
        foreach (var face in _voxelGrid.GetFaces())
        {
            GraphVoxel voxelA = (GraphVoxel)face.Voxels[0];
            GraphVoxel voxelB = (GraphVoxel)face.Voxels[1];

            if (voxelA != null && !voxelA.IsVoid && voxelA.IsActive &&
                voxelB != null && !voxelB.IsVoid && voxelB.IsActive)
            {
                faces.Add(face);
            }
        }

        var graphFaces = faces.Select(f => new TaggedEdge<GraphVoxel, Face>((GraphVoxel)f.Voxels[0], (GraphVoxel)f.Voxels[1], f));
        var graph = graphFaces.ToUndirectedGraph<GraphVoxel, TaggedEdge<GraphVoxel, Face>>();

        for (int i = 1; i < _targets.Count; i++)
        {
            var start = _targets[i - 1];

            var shortest = graph.ShortestPathsDijkstra(e => 1.0, start);

            var end = _targets[i];
            if(shortest(end, out var endPath))
            {
                var endPathVoxels = new HashSet<GraphVoxel>(endPath.SelectMany(e => new[] { e.Source, e.Target }));
                foreach (var pathVoxel in endPathVoxels)
                {
                    pathVoxel.SetAsPath();
                    
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
