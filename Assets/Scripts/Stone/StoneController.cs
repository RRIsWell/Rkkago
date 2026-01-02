using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Stone의 Input을 처리하는 곳
/// </summary>
public class StoneController : MonoBehaviour, IPointerClickHandler
{
    private Stone _stone;
    private StoneMovement stoneMovement;
    private SkillFactory stoneSkillFactory;
    private void Awake()
    {
        _stone = GetComponent<Stone>();
        stoneMovement = new StoneMovement();
        stoneSkillFactory = new SkillFactory(_stone);

    }

    private void Start()
    {
        // 테스트용
        stoneSkillFactory.ActivateSkill(stoneSkillFactory.ChoiceRandomSkill());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("스킬 발동");
        stoneSkillFactory.ActivateSkill(stoneSkillFactory.ChoiceRandomSkill());
    }
}
