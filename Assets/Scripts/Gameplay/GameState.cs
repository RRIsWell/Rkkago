// 매칭 화면 관리

public enum GameState
{
    Waiting, // 플레이어 대기 중
    MatchIntro, // 매칭 화면
    SkillInfo, // 스킬 정보 표시
    CoinFlip, // 선공 결정 동전 던지기
    Playing, // 실제 플레이
    SkillCutScene //TODO: 스킬 발동 화면
}