using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{
    public int[,] shapeData;
    public List<Transform> pieces = new List<Transform>();
    public Vector3 targetOffset = Vector3.zero;

    public void Init(int[,] shape, GameObject piecePrefab, float cellSize)
    {
        shapeData = shape;
        int width = shape.GetLength(0);
        int height = shape.GetLength(1);

        // 중심점 맞추기 위한 오프셋
        float offsetX = (width - 1) * cellSize * 0.5f;
        float offsetY = (height - 1) * cellSize * 0.5f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (shape[x, y] == 1)
                {
                    GameObject p = Instantiate(piecePrefab, transform);
                    p.transform.localPosition = new Vector3(x * cellSize - offsetX, y * cellSize - offsetY, 0);
                    p.transform.localScale = Vector3.one * (cellSize * 0.95f);
                    pieces.Add(p.transform);
                }
            }
        }
    }
    public void SetSortingOrder(int order)
    {
        foreach (Transform piece in pieces)
        {
            SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
            if (sr) sr.sortingOrder = order;
        }
    }

    public void ReturnToOrigin(Vector3 origin, Vector3 scale)
    {
        StartCoroutine(AnimateReturn(origin, scale));
    }
    private System.Collections.IEnumerator AnimateReturn(Vector3 origin, Vector3 endScale)
    {
        float t = 0;
        Vector3 startPos = transform.position; // 애니메이션 시작 시점의 위치
        Vector3 startScale = transform.localScale; // 애니메이션 시작 시점의 크기 (드래그 중인 큰 크기)

        while (t < 1) // t가 1이 될 때까지 반복 (Time.deltaTime * 10 속도로 증가하므로 약 0.1초 소요)
        {
            t += Time.deltaTime * 10;
            transform.position = Vector3.Lerp(startPos, origin, t); // 시작 위치에서 목표 위치로 부드럽게 이동
            transform.localScale = Vector3.Lerp(startScale, endScale, t); // 크기도 부드럽게 변경
            yield return null;
        }
        // 루프 종료 후 정확한 목표 값으로 확정
        transform.position = origin;
        transform.localScale = endScale;

        Vector3 pos = transform.position;
        pos.z = 0;
        transform.position = pos;

        targetOffset = Vector3.zero;
    }
}
