using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RelicTest : MonoBehaviour
{

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.f1Key.wasPressedThisFrame)
            AcquireRandomRelic();

        if (Keyboard.current.f2Key.wasPressedThisFrame)
            PrintOwnedRelics();
    }

    private void AcquireRandomRelic()
    {
        if (RelicManager.Instance == null)
        {
            Debug.LogError("[RelicTest] RelicManager를 찾지 못했습니다.");
            return;
        }

        IReadOnlyList<RelicInstanceData> rewardOptions = RelicManager.Instance.CreateRewardOptions(1);

        if (rewardOptions.Count == 0)
        {
            Debug.LogError("[RelicTest] 획득할 유물이 없습니다. RelicDatabase 등록 상태를 확인하세요.");
            return;
        }

        RelicInstanceData rewardRelic = rewardOptions[0];
        RelicData relicData = RelicManager.Instance.GetRelicData(rewardRelic);

        int previousCount = RelicManager.Instance.GetOwnedRelics(rewardRelic.RelicId).Count;

        if (!RelicManager.Instance.ClaimReward(rewardRelic))
        {
            Debug.LogError($"[RelicTest] 유물 획득에 실패했습니다. RelicId: {rewardRelic.RelicId}");
            return;
        }

        int currentCount = RelicManager.Instance.GetOwnedRelics(rewardRelic.RelicId).Count;
        string relicName = relicData != null ? relicData.DisplayName : rewardRelic.RelicId;

        Debug.Log($"[RelicTest] 유물 획득 성공: {relicName} / 보유 개수: {previousCount} → {currentCount}");

        if (currentCount == previousCount + 1)
            Debug.Log("[RelicTest] 보유 목록에 정상적으로 추가되었습니다.");
        else
            Debug.LogError("[RelicTest] 획득은 처리됐지만 보유 개수가 정상적으로 증가하지 않았습니다.");
    }

    private void PrintOwnedRelics()
    {
        if (RelicManager.Instance == null)
        {
            Debug.LogError("[RelicTest] RelicManager를 찾지 못했습니다.");
            return;
        }

        IReadOnlyList<RelicInventoryData> ownedRelicGroups = RelicManager.Instance.GetOwnedRelicGroups();

        if (ownedRelicGroups.Count == 0)
        {
            Debug.Log("[RelicTest] 현재 보유한 유물이 없습니다.");
            return;
        }

        Debug.Log($"[RelicTest] 보유 유물 종류: {ownedRelicGroups.Count}");

        for (int i = 0; i < ownedRelicGroups.Count; i++)
        {
            RelicInventoryData relicGroup = ownedRelicGroups[i];
            RelicData relicData = RelicManager.Instance.FindRelic(relicGroup.RelicId);
            string relicName = relicData != null ? relicData.DisplayName : relicGroup.RelicId;

            Debug.Log($"[RelicTest] {relicName} / RelicId: {relicGroup.RelicId} / 보유 개수: {relicGroup.Count}");
        }
    }
}