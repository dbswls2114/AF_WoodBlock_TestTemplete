using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("Settings")]
    public int boardSize = 8;
    private float cellSize = 1.0f;
    public float dragScale = 1.0f;

    public Sprite defaultCellSprite; // 평소 기본 셀 이미지
    public Sprite previewCellSprite; // 호버링 시 보여줄 이미지
    public Sprite destroyHighlightSprite; // 파괴 될 블록에 보여줄 이미지

    [Header("Layout References")]
    public Transform boardParent; // 보드가 생성될 부모 위치 및 크기 기준
    public Transform handParent;  // 블록들이 생성될 부모 위치 및 크기 기준

    [Header("Prefab References")]
    public GameObject cellPrefab; // 격자 배경 셀 프리팹
    public GameObject blockPiecePrefab; // 블록 조각 프리팹
    public GameObject gameOverUI; // 게임 오버 UI 패널
    public TextMeshProUGUI scoreText; // 점수 텍스트 (TMP)

    // 내부 상태 변수
    private int[,] gridData; // 0: 빈칸, 1: 채워짐
    private Transform[,] gridVisuals; // 격자에 놓인 블록 조각들의 Transform

    private SpriteRenderer[,] boardCellRenderers;
    private List<Block> activeBlocks = new List<Block>();
    private Block currentDraggingBlock = null;
    private Vector3 originalBlockPos;
    private Vector3 originalBlockScale; // 드래그 취소 시 돌아갈 크기 저장용
    private int currentScore = 0;

    private List<Vector2Int> lastPreviewCoords = new List<Vector2Int>();
    private List<GameObject> activeHighlightOverlays = new List<GameObject>();


    private readonly List<int[,]> blockShapes = new List<int[,]>() {
        new int[,] { {1} }, // 1x1
        new int[,] { {1, 1} },  // 1x2
        new int[,] { {1}, {1} }, // 2x1
        new int[,] { {1, 1, 1} }, // 1x3
        new int[,] { {1}, {1}, {1} }, // 3x1
        new int[,] { {1, 1}, {1, 1} }, // 2x2
        new int[,] { {1, 1, 1, 1} }, // 1x4
        new int[,] { {1}, {1}, {1}, {1} }, // 4x1

        new int[,] { {1, 0},
                     {1, 1} }, // L shape

        new int[,] { {0, 1},
                     {1, 1} }, // reverse L shape

        new int[,] { {1, 1, 1},
                     {1, 1, 1},
                     {1, 1, 1} }, // 3x3

        new int[,] { {1, 0, 0},
                     {1, 0, 0},
                     {1, 1, 1} }, // Big L shape

        new int[,] { {0, 0, 1},
                     {0, 0, 1},
                     {1, 1, 1} }, // reverse Big L shape

        new int[,] { {1, 1, 1},
                     {0, 0, 1},
                     {0, 0, 1} }, // Big ㄱ shape

        new int[,] { {1, 1, 1},
                     {1, 0, 0},
                     {1, 0, 0} }, // reverse Big ㄱ shape

        new int[,] { {0, 1, 0},
                     {1, 1, 1},
                     {0, 1, 0} }, // + shape

        new int[,] { {1, 1, 1},
                     {0, 1, 0},
                     {0, 1, 0} }, // T shape

        new int[,] { {0, 1, 0},
                     {0, 1, 0},
                     {1, 1, 1} }, // reverse T shape

        new int[,] { {1, 1, 1},
                     {0, 0, 1} },

        new int[,] { {1, 1, 1},
                     {1, 0, 0} },

        new int[,] { {1, 0, 0},
                     {1, 1, 1} },

        new int[,] { {0, 0, 1},
                     {1, 1, 1} },

        new int[,] { {0, 1, 1},
                     {1, 1, 0} },

        new int[,] { {1, 1, 0},
                     {0, 1, 1} },


    };



    void Start()
    {
        if (boardParent == null || handParent == null)
        {           
            return;
        }
        InitializeGame();
    }

    void Update()
    {
        HandleInput();
    }

    void InitializeGame()
    {
        currentScore = 0;
        UpdateScoreUI();
        gameOverUI.SetActive(false);
        gridData = new int[boardSize, boardSize];
        gridVisuals = new Transform[boardSize, boardSize];
        boardCellRenderers = new SpriteRenderer[boardSize, boardSize];

        Vector3 boardWorldSize = GetObjectWorldSize(boardParent);
        cellSize = boardWorldSize.x / boardSize;

        CreateBoardVisuals();
        
        SpawnNewBlocks();
    }

    Vector3 GetObjectWorldSize(Transform t)
    {
        // SpriteRenderer가 있으면 그 크기, 없으면 스케일 기반 (1유닛 프리팹 가정)
        SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
        if (sr != null) return sr.bounds.size;

        // RectTransform이 있으면 그 크기 (UI일 경우)
        RectTransform rt = t.GetComponent<RectTransform>();
        if (rt != null) return new Vector3(rt.rect.width * t.lossyScale.x, rt.rect.height * t.lossyScale.y, 1);

        // 기본 Transform 스케일 (부모가 1x1 큐브나 빈 오브젝트일 때)
        return t.lossyScale;
    }

    void CreateBoardVisuals()
    {

        foreach (Transform child in boardParent) Destroy(child.gameObject);

        float offset = (boardSize * cellSize) * 0.5f - (cellSize * 0.5f);            

        for (int x = 0; x < boardSize; x++)
        {
            for (int y = 0; y < boardSize; y++)
            {
                Vector3 pos = new Vector3(x * cellSize - offset, y * cellSize - offset, 0);

                GameObject cell = Instantiate(cellPrefab,boardParent);
                cell.transform.localPosition = pos;
                cell.transform.localScale = Vector3.one * cellSize;
                cell.name = $"Cell_{x}_{y}";

                cell.TryGetComponent(out boardCellRenderers[x, y]);

            }
        }
    }

    void HandleInput()
    {
        if (gameOverUI.activeSelf) return;

        if (Pointer.current == null) return;

        // 포인터 위치 가져오기 (마우스 좌표 or 터치 좌표)
        Vector2 screenPos = Pointer.current.position.ReadValue();
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
        mousePos.z = 0;

        // 입력 상태 확인 (누름, 누르고 있음, 뗌)
        bool wasPressed = Pointer.current.press.wasPressedThisFrame;  
        bool isPressed = Pointer.current.press.isPressed;            
        bool wasReleased = Pointer.current.press.wasReleasedThisFrame;

        if (wasPressed)
        {
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
            if (hit.collider != null)
            {
                Block block = hit.collider.GetComponentInParent<Block>();
                if (block != null && activeBlocks.Contains(block))
                {
                    SoundManager.Instance.PlaySFX(SoundManager.Instance.placeBlockClip);

                    currentDraggingBlock = block;
                    originalBlockPos = block.transform.position;
                    originalBlockScale = block.transform.localScale;

                    // 드래그 시작 시 부모에서 분리하지 않고 월드상 위치 유지하되,
                    // 크기를 보드 셀 크기(1:1)로 부드럽게 복구

                    block.transform.SetParent(null);
                    block.transform.localScale = Vector3.one; 

                    block.SetSortingOrder(100);

                    // 렌더링 순서 상위로
                    Vector3 pos = block.transform.position;
                    pos.z = 0;
                    block.transform.position = pos;

                    block.targetOffset = new Vector3(0, cellSize * 2f, 0);
                }
            }
        }

        if (isPressed && currentDraggingBlock != null)
        {
            currentDraggingBlock.transform.position = Vector3.Lerp(currentDraggingBlock.transform.position, mousePos + currentDraggingBlock.targetOffset, 25 * Time.deltaTime);
            ShowPreview(currentDraggingBlock);
        }

        if (wasReleased && currentDraggingBlock != null)
        {
            ClearPreview();
            TryPlaceBlock(currentDraggingBlock);
            currentDraggingBlock = null;
        }
    }

    void ShowPreview(Block block)
    {
        List<Vector2Int> currentCoords = new List<Vector2Int>();
        bool canPlace = true;

        foreach (Transform piece in block.pieces)
        {
            Vector2Int pos = WorldToGrid(piece.position);

            if (!IsValidCoord(pos) || gridData[pos.x, pos.y] == 1)
            {
                canPlace = false;
                break;
            }
            currentCoords.Add(pos);
        }

        if (lastPreviewCoords.Count > 0 || activeHighlightOverlays.Count > 0)
        {
            ClearPreview();
        }

        if (canPlace)
        {
            foreach (Vector2Int pos in currentCoords)
            {

                SpriteRenderer sr = boardCellRenderers[pos.x, pos.y];
                if (sr != null && previewCellSprite != null)
                {
                    sr.sprite = previewCellSprite;
                }
            }
            lastPreviewCoords = new List<Vector2Int>(currentCoords);

            HighlightLinesToClear(currentCoords);
        }
    }

    void ClearPreview()
    {
        foreach (Vector2Int pos in lastPreviewCoords)
        {
            if (IsValidCoord(pos))
            {                
                SpriteRenderer sr = boardCellRenderers[pos.x, pos.y];
                if (sr != null && defaultCellSprite != null)
                {
                    sr.sprite = defaultCellSprite;
                }
            }
        }
        lastPreviewCoords.Clear();

        foreach (GameObject overlay in activeHighlightOverlays)
        {
            if (overlay != null) Destroy(overlay);
        }
        activeHighlightOverlays.Clear();
    }

    void HighlightLinesToClear(List<Vector2Int> newBlockCoords)
    {
        int[,] tempGrid = (int[,])gridData.Clone();
        foreach (var pos in newBlockCoords)
        {
            tempGrid[pos.x, pos.y] = 1;
        }

        GetLinesToClear(tempGrid, out List<int> fullRows, out List<int> fullCols);

        HashSet<Vector2Int> blocksToHighlight = new HashSet<Vector2Int>();

        foreach (int y in fullRows)
        {
            for (int x = 0; x < boardSize; x++)
            {
                if (gridVisuals[x, y] != null) blocksToHighlight.Add(new Vector2Int(x, y));
            }
        }

        foreach (int x in fullCols)
        {
            for (int y = 0; y < boardSize; y++)
            {
                if (gridVisuals[x, y] != null) blocksToHighlight.Add(new Vector2Int(x, y));
            }
        }

        // 4. 오버레이 생성
        foreach (var pos in blocksToHighlight)
        {
            Transform blockPiece = gridVisuals[pos.x, pos.y];
            if (blockPiece != null)
            {
                GameObject overlay = new GameObject("HighlightOverlay");
                overlay.transform.SetParent(boardParent);
                overlay.transform.position = blockPiece.position;
                overlay.transform.localScale = blockPiece.localScale;

                SpriteRenderer sr = overlay.AddComponent<SpriteRenderer>();
                sr.sprite = destroyHighlightSprite;
                sr.sortingOrder = 6;

                activeHighlightOverlays.Add(overlay);
            }
        }
    }

    void SpawnNewBlocks()
    {
        foreach (Transform child in handParent) Destroy(child.gameObject);
        activeBlocks.Clear();

        Vector3 handAreaSize = GetObjectWorldSize(handParent);
        Vector3 handCenterPos = handParent.position;

        float slotWidth = handAreaSize.x / 3.0f;
        float startX = handCenterPos.x - handAreaSize.x * 0.5f + slotWidth * 0.5f; 

        List<GameObject> createdObjs = new List<GameObject>();
        List<Block> createdScripts = new List<Block>();

        float minCommonScale = 1.0f;

        for (int i = 0; i < 3; i++)
        {
            int[,] shapeData = blockShapes[Random.Range(0, blockShapes.Count)];

            Vector3 slotPos = new Vector3(startX + (i * slotWidth), handCenterPos.y, 0);

            GameObject blockObj = new GameObject($"Block_{i}");
            blockObj.transform.position = slotPos;

            Block blockScript = blockObj.AddComponent<Block>();
            blockScript.Init(shapeData, blockPiecePrefab, cellSize);

            float blockWidth = shapeData.GetLength(0) * cellSize;
            float blockHeight = shapeData.GetLength(1) * cellSize;

            float maxW = slotWidth * 0.8f;
            float maxH = handAreaSize.y * 0.8f;

            float scaleX = maxW / blockWidth;
            float scaleY = maxH / blockHeight;  
            float fitScale = Mathf.Min(scaleX, scaleY, 1.0f);

            if (fitScale < minCommonScale) minCommonScale = fitScale;
           
            createdObjs.Add(blockObj);
            createdScripts.Add(blockScript);

            BoxCollider2D col = blockObj.AddComponent<BoxCollider2D>();
            // 충돌체 크기는 로컬 스케일 영향을 받으므로 실제 사이즈로 설정
            col.size = new Vector2(blockWidth, blockHeight);

            
        }
        float finalScale = Mathf.Min(minCommonScale, 0.6f);

        for (int i = 0; i < 3; i++)
        {
            GameObject obj = createdObjs[i];
            obj.transform.localScale = Vector3.one * finalScale;

            // 부모 설정 (HandParent) - 월드 위치/크기 유지(true)
            obj.transform.SetParent(handParent, true);

            createdScripts[i].SetSortingOrder(10);

            activeBlocks.Add(createdScripts[i]);
        }

        CheckGameOver();
    }

    void TryPlaceBlock(Block block)
    {
        bool canPlace = true;
        List<Vector2Int> targetCoords = new List<Vector2Int>();

        foreach (Transform piece in block.pieces)
        {
            Vector2Int pos = WorldToGrid(piece.position);

            // 보드 범위 밖이거나 이미 채워진 칸이면 실패
            if (!IsValidCoord(pos) || gridData[pos.x, pos.y] == 1)
            {
                canPlace = false;
                break;
            }
            targetCoords.Add(pos);
        }

        if (canPlace && targetCoords.Count == block.pieces.Count)
        {
            PlaceBlockOnGrid(block, targetCoords);
        }
        else
        {
            block.transform.SetParent(handParent, true); 
            block.SetSortingOrder(10);
            block.ReturnToOrigin(originalBlockPos, originalBlockScale);
        }
    }

    Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 localPos = boardParent.InverseTransformPoint(worldPos);

        float boardRealWidth = cellSize * boardSize;
        float startOffset = -boardRealWidth * 0.5f + cellSize * 0.5f;   

        int x = Mathf.RoundToInt((localPos.x - startOffset) / cellSize);
        int y = Mathf.RoundToInt((localPos.y - startOffset) / cellSize);

        return new Vector2Int(x, y);
    }

    Vector3 GridToWorld(int x, int y)
    {
        float boardRealWidth = cellSize * boardSize;
        float startOffset = -boardRealWidth / 2.0f + cellSize / 2.0f;

        Vector3 localPos = new Vector3(startOffset + x * cellSize, startOffset + y * cellSize, 0);
        return boardParent.TransformPoint(localPos);
    }

    bool IsValidCoord(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < boardSize && pos.y >= 0 && pos.y < boardSize;
    }

    void PlaceBlockOnGrid(Block block, List<Vector2Int> coords)
    {
       SoundManager.Instance.PlaySFX(SoundManager.Instance.placeBlockClip);

        for (int i = 0; i < coords.Count; i++)
        {
            Vector2Int pos = coords[i];
            gridData[pos.x, pos.y] = 1;

            Transform piece = block.pieces[i];

            piece.SetParent(boardParent);
            piece.position = GridToWorld(pos.x, pos.y);
            piece.localScale = Vector3.one * (cellSize * 0.95f);

            SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
            if (sr) sr.sortingOrder = 5;

            gridVisuals[pos.x, pos.y] = piece;
        }
        activeBlocks.Remove(block);
        Destroy(block.gameObject);

        AddScore(10 + coords.Count);

        CheckLines();

        if (activeBlocks.Count == 0) SpawnNewBlocks();
        else CheckGameOver();
    }

    void GetLinesToClear(int[,] targetGrid, out List<int> fullRows, out List<int> fullCols)
    {
        fullRows = new List<int>();
        fullCols = new List<int>();

        // 가로 확인
        for (int y = 0; y < boardSize; y++)
        {
            bool full = true;
            for (int x = 0; x < boardSize; x++)
            {
                if (targetGrid[x, y] == 0)
                {
                    full = false;
                    break;
                }
            }
            if (full) fullRows.Add(y);
        }

        // 세로 확인
        for (int x = 0; x < boardSize; x++)
        {
            bool full = true;
            for (int y = 0; y < boardSize; y++)
            {
                if (targetGrid[x, y] == 0)
                {
                    full = false;
                    break;
                }
            }
            if (full) fullCols.Add(x);
        }
    }

    void CheckLines()
    {
        GetLinesToClear(gridData, out List<int> rowsToClear, out List<int> colsToClear);

        if (rowsToClear.Count > 0 || colsToClear.Count > 0)
            ClearLines(rowsToClear, colsToClear);
    }

    void ClearLines(List<int> rows, List<int> cols)
    {
        int totalClearedCells = 0;

        // 중복 제거를 위해 Set 사용 혹은 단순 반복
        HashSet<Transform> piecesToDestroy = new HashSet<Transform>();

        foreach (int y in rows)
        {
            for (int x = 0; x < boardSize; x++)
            {
                if (gridData[x, y] == 1)
                {
                    gridData[x, y] = 0;
                    if (gridVisuals[x, y] != null) piecesToDestroy.Add(gridVisuals[x, y]);
                    gridVisuals[x, y] = null;
                }
            }
        }

        foreach (int x in cols)
        {
            for (int y = 0; y < boardSize; y++)
            {
                if (gridData[x, y] == 1) 
                {
                    gridData[x, y] = 0;
                    if (gridVisuals[x, y] != null) piecesToDestroy.Add(gridVisuals[x, y]);
                    gridVisuals[x, y] = null;
                }
            }
        }

        foreach (Transform t in piecesToDestroy)
        {
            Destroy(t.gameObject); // 나중에 파티클 효과 추가 가능
            totalClearedCells++;
        }

        // 점수 계산 (콤보 점수 등 추가 가능)
        AddScore(totalClearedCells * 10 * (rows.Count + cols.Count));
    }

    void AddScore(int amount)
    {
        currentScore += amount;
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        string scoretxt = "Score";

        if (scoreText) scoreText.text = $"<size=20>{scoretxt}</size>\n<size=30>{currentScore}</size>";
    }

    // --- 게임 오버 확인 ---
    void CheckGameOver()
    {
        // 남은 블록 중 하나라도 놓을 곳이 있는지 확인
        bool possibleMoveExists = false;

        foreach (Block block in activeBlocks)
        {
            if (CanFitAnywhere(block))
            {
                possibleMoveExists = true;
                break;
            }
        }

        if (!possibleMoveExists && activeBlocks.Count > 0)
        {
            Debug.Log("Game Over");
            gameOverUI.SetActive(true);
        }
    }

    bool CanFitAnywhere(Block block)
    {
        // 모든 격자 위치(x,y)에 대해 블록을 놓아볼 수 있는지 테스트
        int[,] shape = block.shapeData;
        int shapeW = shape.GetLength(0);
        int shapeH = shape.GetLength(1);

        for (int x = 0; x <= boardSize - shapeW; x++)
        {
            for (int y = 0; y <= boardSize - shapeH; y++)
            {
                if (CheckFitAt(x, y, shape)) return true;
            }
        }
        return false;
    }

    bool CheckFitAt(int startX, int startY, int[,] shape)
    {
        for (int i = 0; i < shape.GetLength(0); i++)
        {
            for (int j = 0; j < shape.GetLength(1); j++)
            {
                if (shape[i, j] == 1)
                {
                    int targetX = startX + i;
                    int targetY = startY + j;

                    // 범위를 벗어나거나 이미 차있으면 실패
                    if (targetX >= boardSize || targetY >= boardSize || gridData[targetX, targetY] == 1)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }
 


}
