using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//02 Create GraphVoxel, inherit from Voxel
public class GraphVoxel : Voxel
{
    //03 Create Regions
    #region Private fields
    
    //04 Create private _state float
    private float _state;

    #endregion

    #region Public fields

    //24 Create IsObstacle variable
    public bool IsObstacle;
    public bool IsVoid;
    
    //44 Create IsTarget variable
    public bool IsTarget;

    //77 Create IsPath variable
    public bool IsPath;

    #endregion

    #region Constructors

    //05 Create GraphVoxel constructor
    /// <summary>
    /// Creates a <see cref="GraphVoxel"/>
    /// </summary>
    /// <param name="index">Index on the <see cref="VoxelGrid"/></param>
    /// <param name="voxelGrid">Grid the voxel is to be created at</param>
    /// <param name="state">Initial state the voxel should be set to</param>
    /// <param name="sizeFactor">Factor to scale the GameObject over the voxel size</param>
    public GraphVoxel(Vector3Int index, VoxelGrid voxelGrid, float state, float sizeFactor)
    {
        Index = index;
        _voxelGrid = voxelGrid;
        _size = _voxelGrid.VoxelSize;

        _state = state;

        _voxelGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _voxelGO.transform.position = (_voxelGrid.Origin + Index) * _size;
        _voxelGO.transform.localScale *= _voxelGrid.VoxelSize * sizeFactor;
        _voxelGO.name = $"Voxel_{Index.x}_{Index.y}_{Index.z}";
        _voxelGO.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/Basic");
        //_voxelGO.tag = ("ObstacleVoxel");

        IsActive = true;
    }

    #endregion

    #region Public methods

    //18 Create SetState method for the GraphVoxel
    /// <summary>
    /// Changes the state of the <see cref="GraphVoxel"/>, updating the material
    /// </summary>
    /// <param name="newState">The new state to be set</param>
    public void SetState(float newState)
    {
        //19 Set state field
        _state = newState;


        //22 Set voxel as void if value is below threshold
        if (_state <= 1 / _voxelGrid.GridSize.y)
        {
            //23 Read material and set
            _voxelGO.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/GV_Obstacle");
            
            //25 Set as not obstacle
            IsObstacle = true;

            //26 Create tag in Unity and set here
            _voxelGO.tag = "ObstacleVoxel";
        }
        //27 Set voxel as obstacle
        else
        {
            //28 Read material and set
            _voxelGO.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/GV_Void");
            
            //29 Set as obstacle
            IsObstacle = false;

            //30 Create tag in Unity and set
            _voxelGO.tag = "VoidVoxel";
        }
    }

    //42 Create public method to toggle the visibility of this voxel
    /// <summary>
    /// Toggle the visibility of the voxel
    /// </summary>
    public void ToggleVisibility()
    {
        _voxelGO.GetComponent<MeshRenderer>().enabled = !_voxelGO.GetComponent<MeshRenderer>().enabled;
    }

    //53 Create method to set voxel as target
    /// <summary>
    /// Switch the state of the voxel between target states
    /// </summary>
    public void SetAsTarget()
    {
        //54 Flip target bool
        IsTarget = !IsTarget;
        
        //55 If voxel is target, set material and tag
        if (IsTarget)
        {
            _voxelGO.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/GV_Target");
            _voxelGO.tag = "TargetVoxel";
        }
        //56 Else, set material and tag to void
        else
        {
            _voxelGO.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/GV_Void");
            _voxelGO.tag = "VoidVoxel";
        }
    }

    //78 Create method to set voxel as path
    /// <summary>
    /// Set the voxel as a Path voxel
    /// </summary>
    public void SetAsPath()
    {
        //79 Check if it is not a target
        if (!IsTarget)
        {
            //80 Set material and tag
            _voxelGO.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/GV_Path");
            _voxelGO.tag = "PathVoxel";
            IsPath = true;
        }
    }

    #endregion
}