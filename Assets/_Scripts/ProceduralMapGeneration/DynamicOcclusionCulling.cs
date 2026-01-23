using UnityEngine;

public class DynamicOcclusionCulling : MonoBehaviour
{
    [SerializeField] bool useCameraView = true;
    [SerializeField] int aRR_H = 6;
    [SerializeField] int aRR_V = 2;

    void OnEnable()
    {
        GameTick.OnTick += UpdateCulling;
    }

    void OnDisable()
    {
        GameTick.OnTick -= UpdateCulling;
    }

    void UpdateCulling()
    {
        if (!MapGenerator.Instance.GeneratedDungeon) return;

        PlayerData pData = GameManager.Instance.playMod.LocalPlayer.Spectator_Movement.GetPlayerData();

        if (useCameraView)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(pData.PlayerCamera);

            foreach (var room in MapGenerator.Instance.SpawnedRooms)
            {
                room.Value.SetRender(GeometryUtility.TestPlanesAABB(planes, room.Value.roomBounds));
            }
        }
        else
        {
            foreach (var room in MapGenerator.Instance.SpawnedRooms)
            {
                room.Value.SetRender(false);
            }
        }

        if (aRR_H <= 0 || aRR_V <= 0) return;

        // Always render 3x3 area
        Vector3 pp = pData.transform.position;
        Vector3Int cellPosition = Vector3Int.zero;
        cellPosition.x = Mathf.RoundToInt(pp.x) / MapGenerator.Instance.CellSize;
        cellPosition.y = Mathf.RoundToInt(pp.y) / MapGenerator.Instance.CellSize;
        cellPosition.z = Mathf.RoundToInt(pp.z) / MapGenerator.Instance.CellSize;

        var grid = MapGenerator.Instance.Grid;

        for (int dx = -aRR_H; dx <= aRR_H; dx++) 
        { 
            for (int dz = -aRR_H; dz <= aRR_H; dz++) 
            {
                for (int dy = -aRR_V; dy <= aRR_V; dy++)
                {
                    int nx = cellPosition.x + dx;
                    int nz = cellPosition.z + dz;
                    int ny = cellPosition.y + dy;
                    if (nx < 0 ||
                        nz < 0 ||
                        ny < 0 ||
                        nx >= grid.GetLength(0) ||
                        nz >= grid.GetLength(2) ||
                        ny >= grid.GetLength(1))
                        continue;

                    var cell = grid[nx, ny, nz];
                    if (cell?.placedRoom != null)
                    {
                        MapGenerator.Instance.SpawnedRooms[cell.placedRoom.id].SetRender(true);
                    }
                }
            } 
        }
    }
}
