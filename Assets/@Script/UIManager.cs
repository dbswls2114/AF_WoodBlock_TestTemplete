using UnityEngine;

public class UIManager : MonoBehaviour
{
   public static UIManager Instance;
    //GameManager 에 있는 UI 관련 기능들을 여기에 추가할 예정

    public Transform scoreContainer; // 숫자가 생성될 부모 객체 (Layout Group 있음)
    public GameObject digitPrefab;   // 숫자 하나를 표시할 Image 프리팹

    private Sprite[] numberSprites;  // 로드된 0~9 스프라이트 저장

    void Awake()
    {
         if (Instance == null)
         {
              Instance = this;
              DontDestroyOnLoad(gameObject);
              LoadNumberSprites();
        }
         else
         {
              Destroy(gameObject);
         }
    }

    void LoadNumberSprites()
    {
        numberSprites = new Sprite[10];

        for (int i = 0; i < 10; i++)
        {
            Sprite loadedSprite = Resources.Load<Sprite>($"Numbers/{i}");

            if (loadedSprite != null)
            {
                numberSprites[i] = loadedSprite;
            }
            else
            {
                Debug.LogError($"숫자 스프라이트 {i}를 찾을 수 없습니다! Resources/Numbers 폴더를 확인하세요.");
            }
        }
    }


    public void GameoverUIScore(int score)
    {
        // 기존 점수 UI 초기화
        foreach (Transform child in scoreContainer)
        {
            Destroy(child.gameObject);
        }
        // 점수를 문자열로 변환
        string scoreStr = score.ToString();
        // 각 자리수마다 이미지 생성
        foreach (char digitChar in scoreStr)
        {
            int digit = digitChar - '0'; // 문자에서 숫자 변환
            if (digit >= 0 && digit <= 9)
            {
                GameObject digitObj = Instantiate(digitPrefab, scoreContainer);
                UnityEngine.UI.Image digitImage = digitObj.GetComponent<UnityEngine.UI.Image>();
                digitImage.sprite = numberSprites[digit];
            }
            else
            {
                Debug.LogError($"잘못된 숫자 문자: {digitChar}");
            }
        }
    }
}
