using Unity.Jobs;
using UnityEngine;

public class LateBloomer : SkillBase
{
    private Stone _s;
    private float _st;
    private float _debuffedWeight;
    private float _power;

    private int _turnCnt = 0;
    
    public LateBloomer(Stone stone, SkillSO data) : base(stone, data)
    {
        var so = data as LateBloomerSO;
        _s = stone;
        _st = so.strength;
        _debuffedWeight = so.debuffedWeight;
    }

    private void ApplyInitialDebuff()
    {
        // 서버에 변경된 무게를 동기화 (RPC 인자에 맞게 수정 필요)
        _s.ChangeStoneWeightServerRpc(_debuffedWeight); 
    }

    // 매 턴 시작할 때마다 자동으로 실행되는 메서드
    public override void Activate()
    {
        _turnCnt++;

        // 2. 화면에 같은 애니메이션 띄우기 (매 턴 실행)
        PlaySkillAnimation();

        // 3. 스킬 부여된 직후의 첫 턴(1) 제외, 2번째 턴부터 버프 적용
        if (_turnCnt > 1)
        {
            ApplyTurnBuff();
        }
        else
        {
            ApplyInitialDebuff();
        }
        // 3. 2, 4, 6번째 턴에만 알 이미지 변경
        if (_turnCnt == 3 || _turnCnt == 7)
        {
            ChangeStoneImage();
        }
    }

    private void ApplyTurnBuff()
    {
        // 무게 1.2배, 파워 1.2배 버프 계산
        // 기획에 따라 하드코딩된 1.2f 대신 SO에서 받아온 _st를 사용해도 좋습니다.
        float buffedWeight = _s.GetWeight() * _st; 
        float buffedPower = _s.GetPower() * _st;

        // 서버에 변경된 스탯 동기화
        _s.ChangeStoneWeightServerRpc(buffedWeight);
        _s.ChangeStonePowerServerRpc(buffedPower); 
    }

    private void PlaySkillAnimation()
    {
        // TODO: 실제 애니메이션 실행 로직을 여기에 구현하세요.
        // 예: _s.PlayEffectServerRpc("LateBloomerAnim"); 또는 로컬 파티클 재생 등
        Debug.Log($"[{_turnCnt}턴] 대기만성(LateBloomer) 애니메이션 재생!");
    }
    
    private void ChangeStoneImage()
    {
        string spriteName = SkillName.ToString();
        if (_turnCnt == 3)
        {
            spriteName = SkillName.ToString() + "2";
        }
        else if (_turnCnt == 7)
        {
            spriteName = SkillName.ToString() + "3";
        }
        if (_s.Resolver != null)
        {
            _s.Resolver.SetCategoryAndLabel("Idle", spriteName);
            Debug.Log($"[{_turnCnt}턴] 알 이미지 변경 완료!");
        }
    }

    public override void Deactivate()
    {
        _turnCnt = 0;
    }

}
