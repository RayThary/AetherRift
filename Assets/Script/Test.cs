using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField] private bool checkSavedData;
    
    public bool test = false;
    [SerializeField] private RelicData testRelic;
    [SerializeField] private DungeonData testDungeon;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (test)
        {
            GameProgressManager.Instance.AddCurrency(100);
            GameProgressManager.Instance.AddRelic(testRelic);
            GameProgressManager.Instance.CompleteDungeon(testDungeon);

            Debug.Log("진행 데이터 테스트 완료: 골드 증가 / 유물 획득 / 던전 클리어");

            SaveManager.Instance.SaveGame();
            test = false;
        }

        if (checkSavedData)
        {
            GameProgressData data = GameProgressManager.Instance.CurrentData;

            Debug.Log($"보유 골드: {data.currency}");
            Debug.Log($"보유 유물: {string.Join(", ", data.ownedRelicIds)}");
            Debug.Log($"장착 유물: {string.Join(", ", data.equippedRelicIds)}");
            Debug.Log($"클리어 던전: {string.Join(", ", data.completedDungeonIds)}");

            checkSavedData = false;
        }
     
    }
}
