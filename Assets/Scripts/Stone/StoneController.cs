using System;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Stone의 Input을 처리하는 곳
/// </summary>
public class StoneController : NetworkBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField]
    private GameObject dragLinePrefab;
    private DragLine _dragLine;
    
    [SerializeField]
    private float maxDrag = 1.5f;
    
    private Stone _stone;
    public Stone Stone => _stone;
    private Camera _camera;
    
    public StoneMovement StoneMovement { get; private set; }
    private SkillFactory stoneSkillFactory;
    public int SkillCount => stoneSkillFactory.SkillCount;


    // 디버깅용
    private bool _isDragging;
    private Vector3 _mousePosition;
    
    private void Awake()
    {
        _stone = GetComponent<Stone>();
        _camera = Camera.main;
        
        StoneMovement = new StoneMovement(this, this);
        stoneSkillFactory = new SkillFactory(_stone);
    }

    private void Start()
    {
        _dragLine = Instantiate(dragLinePrefab, transform).GetComponent<DragLine>() ;
        _dragLine.gameObject.SetActive(false);
    }

    // ----------------- Input --------------------
    
    // 내 턴인지 체크
    private bool CanInput()
    {
        if(!IsOwner) return false;
        if(TurnManager.Instance == null) return false;
        return TurnManager.Instance.IsMyTurn();
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanInput()) return;
        
        // 알 드래그 시작 시 1회 실행
        Vector2 worldPos = _camera.ScreenToWorldPoint(eventData.position);
        _mousePosition = worldPos;
        
        ActivateDragLine();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!CanInput()) return;
        
        // 마우스에서 돌까지 직선 방향 계산
        Vector2 worldPos = _camera.ScreenToWorldPoint(eventData.position);
        _mousePosition = worldPos;
        
        Vector2 direction = (worldPos - (Vector2)transform.position).normalized;
        float distance = Vector2.Distance(worldPos, transform.position);
        if(distance > maxDrag) distance = maxDrag;
        
        UpdateDragLine(direction, distance);
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!CanInput()) return;
        
        // 드래그 끝
        DeActivateDragLine();
        
        // 테스트 스킬
        //stoneSkillFactory.ActivateSkill(stoneSkillFactory.ChoiceRandomSkill());
        
        // 돌 날아감
        Vector2 worldPos = _camera.ScreenToWorldPoint(eventData.position);
        Vector2 direction = ((Vector2)transform.position - worldPos).normalized;
        
        float distance = Vector2.Distance(transform.position, worldPos);
        if(distance > maxDrag) distance = maxDrag;
        float speed = _stone.CalculateSpeed() * distance;

        RequestShoot(direction, speed);
    }
    
    // ---------------- Network --------------------
    
    // 내 알을 쏠 때 (Owner만 호출)
    public void RequestShoot(Vector2 direction, float speed)
    {
        if (!IsOwner) return;
        
        // 서버에 요청
        ShootServerRpc(direction, speed);

        // 한번 쏘고 나면 즉시 턴 종료 요청
        TurnManager.Instance.EndTurnServerRpc();
    }
    
    [ServerRpc]
    private void ShootServerRpc(Vector2 direction, float speed)
    {
        // 서버에서 이 알 이동 시작
        StoneMovement.Shoot(transform, direction, speed);
    }
    
    public void TriggerShootFromCollision(Vector2 direction, float speed)
    {
        StoneMovement.Shoot(transform, direction, speed);
    }

    // 서버가 정한 스킬을 클라이언트에게 적용
    [ClientRpc]
    public void ApplySkillClientRpc(int skillIndex)
    {
        // 자기 돌만 적용
        if (!IsOwner) return;

        var skill = stoneSkillFactory.GetSkillByIndex(skillIndex);
        stoneSkillFactory.ActivateSkill(skill);

        Debug.Log(
            $"[Skill] Owner({OwnerClientId}) received skill index={skillIndex}"
        );
    }
    
    // ------------------- 보조선 --------------------
    private void ActivateDragLine()
    {
        _isDragging = true;
        _dragLine.gameObject.SetActive(true);
        _dragLine.SetTransform(transform);
    }

    private void UpdateDragLine(Vector2 direction, float distance)
    {
        _dragLine.UpdateDragLine(transform, direction, distance);
    }

    private void DeActivateDragLine()
    {
        _isDragging = false;
        _dragLine.gameObject.SetActive(false);
    }
    
    // 디버그용 - 드래그 방향 시각화
    private void OnDrawGizmos()
    {
        if (StoneMovement != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, StoneMovement._collisionRadius);
            
            if (_isDragging)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, _mousePosition);
            }
        }
    }
}
