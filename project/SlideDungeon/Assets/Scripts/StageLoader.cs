using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class StageLoader : MonoBehaviour
{
    #region private変数
    [Header("プレイヤー")]
    [SerializeField] private GameObject player;
    [Header("マップ関連")]
    [SerializeField] private Tilemap floorTilemap;   //地面用Tilemap
    [SerializeField] private Tilemap wallTilemap;    //壁用Tilemap
    [SerializeField] private TileBase floorBackTile; //木床タイル
    [SerializeField] private TileBase wallRockTile;  //石床タイル
    [SerializeField] private GameObject goal;        //ゴール生成用
    [SerializeField] private GameObject thorn;        //とげ用

    private int stageIndex;
    #endregion

    #region public変数
    public string csvFileName;                       //csvファイル名
    #endregion

    #region Unityイベント関数
    // Start is called before the first frame update
    void Start()
    {
        Init();
        //Debug.Log(stageIndex);
    }

    #endregion

    #region Start呼び出し関数

    #region 初期化
    void Init()
    {
        stageIndex = StageIndex.Instance.GetIndex();
        csvFileName = "Stage" + stageIndex;
        LoadMapFromCSV(csvFileName);
    }
    #endregion

    #region ステージ読み込み

    void LoadMapFromCSV(string fileName)
    {
        TextAsset csvFile = Resources.Load<TextAsset>(fileName);
        if (csvFile == null)
        {
            Debug.LogError("CSVファイルが見つかりません: " + fileName);
            return;
        }

        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();

        string[] lines = csvFile.text.Trim().Split('\n');

        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return;

        //カメラの縦方向の半分の長さ（orthographicSize は縦の半分を表す）
        float camHalfHeight = cam.orthographicSize;

        //カメラの横方向の半分の長さ（縦の半分 × アスペクト比 = 横の半分）
        float camHalfWidth = camHalfHeight * cam.aspect;

        //カメラのワールド座標を基準に「画面の左上ワールド座標」を求める
        Vector3 topLeftWorld = cam.transform.position;
        topLeftWorld.x -= camHalfWidth;  //中心から左へ移動
        topLeftWorld.y += camHalfHeight; //中心から上へ移動

        //左上のワールド座標をタイルマップのセル座標に変換
        Vector3Int topLeftCell = floorTilemap.WorldToCell(topLeftWorld);

        //タイル配置が 1 行分ずれるので補正（Unity の座標系と CSV の行の対応差を修正）
        topLeftCell.y -= 1;

        for (int y = 0; y < lines.Length; y++)
        {
            string[] values = lines[y].Trim().Split(',');

            for (int x = 0; x < values.Length; x++)
            {
                if (int.TryParse(values[x], out int value))
                {
                    //タイルの描画場所
                    Vector3Int cellPos = new Vector3Int(topLeftCell.x + x, topLeftCell.y - y, 0);
                    //タイルと同じ描画だとずれるからその分ずらす
                    Vector3 goalPos = new Vector3(topLeftCell.x + x + 0.5f, topLeftCell.y - y + 0.5f, 0);

                    switch (value)
                    {
                        case 0:
                            floorTilemap.SetTile(cellPos, floorBackTile);     //床タイル
                            break;
                        case 1:
                            wallTilemap.SetTile(cellPos, wallRockTile);       //壁タイル
                            break;
                        case 2:
                            floorTilemap.SetTile(cellPos, floorBackTile);     //床タイルを先に描画
                            Instantiate(goal, goalPos, Quaternion.identity, transform);  //ゴールを置く
                            break;
                        case 3:
                            floorTilemap.SetTile(cellPos, floorBackTile);     //床タイルを先に描画
                            Instantiate(thorn, goalPos, Quaternion.identity, transform); //とげを置く
                            break;
                        case 4:
                            floorTilemap.SetTile(cellPos, floorBackTile);
                            Instantiate(player, goalPos, Quaternion.identity, transform); //プレイヤーを置く
                            break;
                    }
                }
            }
        }
    }
    #endregion

    #endregion
}
