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
    private GameObject _dragLine;
    
    private Stone _stone;
    private Camera _camera;
    
    public StoneMovement StoneMovement { get; private set; }
    private SkillFactory stoneSkillFactory;
    
    private bool _isDragging = false;
    private Vector3 _mousePosition;
    
    private void Awake()
    {
        _stone = GetComponent<Stone>();
        _camera = Camera.main;
        
        StoneMovement = new StoneMovement(this);
        stoneSkillFactory = new SkillFactory(_stone);
    }

    private void Start()
    {
        /*_dragLine = Instantiate(dragLinePrefab, transform);
        _dragLine.SetActive(false);*/
    }

    // ----------------- Input --------------------
    
    public void OnPointerDown(PointerEventData eventData)
    {
        // 알 드래그 시작 시 1회 실행
        _isDragging = true;
        Vector2 worldPos = _camera.ScreenToWorldPoint(eventData.position);
        _mousePosition = worldPos;
        
        //ActivateDragLine();
    }

    public void OnDrag(PointerEventData eventData)
    {
        // TODO: 마우스에서 돌까지 직선 방향 계산
        Vector2 worldPos = _camera.ScreenToWorldPoint(eventData.position);
        _mousePosition = worldPos;
        
        //UpdateDragLine(worldPos);
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        // 드래그 끝
        //DeActivateDragLine();
        _isDragging = false;
        
        // 테스트 스킬
        //stoneSkillFactory.ActivateSkill(stoneSkillFactory.ChoiceRandomSkill());
        
        // 돌 날아감
        Vector2 worldPos = _camera.ScreenToWorldPoint(eventData.position);
        Vector2 direction = ((Vector2)transform.position - worldPos).normalized;
        
        float distance = Vector2.Distance(transform.position, worldPos);
        float speed = _stone.CalculateSpeed() * distance;

        RequestShoot(direction, speed);
        //StoneMovement.Shoot(transform, direction, speed);
    }
    
    // ---------------- Network --------------------
    
    // 내 알을 쏠 때 (Owner만 호출)
    public void RequestShoot(Vector2 direction, float speed)
    {
        if (!IsOwner) return;
        
        // 서버에 요청
        ShootServerRpc(direction, speed);
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
    
    /*private void ActivateDragLine()
    {
        _isDragging = true;
        _dragLine.SetActive(true);
        _dragLine.transform.position = transform.position;
        _dragLine.transform.localScale = new Vector3(1, 0, 1);
    }*/

    /*private void UpdateDragLine(Vector3 mousePosition)
    {
        // 회전값
        Vector3 direction = (mousePosition - transform.position).normalized;
        float angle = Vector2.SignedAngle(Vector2.up, direction);
        _dragLine.transform.rotation = Quaternion.Euler(0, 0, angle);
        
        // 길이
        float distance = Vector2.Distance(mousePosition, transform.position);;
        
        Vector3 scale = _dragLine.transform.localScale;
        scale.y = distance;
        _dragLine.transform.localScale = scale;
        
        _dragLine.transform.position = transform.position + (Vector3)(direction * (distance * 0.5f));
    }*/

    /*private void DeActivateDragLine()
    {
        _isDragging = false;
        _dragLine.SetActive(false);
    }*/
    
    // 디버그용 - 드래그 방향 시각화
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.6f);
        
        if (_isDragging && StoneMovement != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _mousePosition);
        }
    }
}
