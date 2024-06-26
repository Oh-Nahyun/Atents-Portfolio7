using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetPlayer : NetworkBehaviour
{
    /// <summary>
    /// 이동 속도
    /// </summary>
    public float moveSpeed = 3.5f;

    /// <summary>
    /// 회전 속도
    /// </summary>
    public float rotateSpeed = 90.0f;

    /// <summary>
    /// 마지막 입력으로 인한 이동 방향 (전진, 정지, 후진)
    /// 네트워크에서 공유되는 변수
    /// </summary>
    NetworkVariable<float> netMoveDir = new NetworkVariable<float>(0.0f);

    /// <summary>
    /// 마지막 입려으로 인한 회전 방향 (좌회전, 정지, 우회전)
    /// </summary>
    NetworkVariable<float> netRotate = new NetworkVariable<float>(0.0f);

    /// <summary>
    /// 애니메이션 상태
    /// </summary>
    enum AnimationState
    {
        Idle,       // 대기
        Walk,       // 걷기
        BackWalk,   // 뒤로 걷기
        None        // 초기값
    }

    /// <summary>
    /// 현재 애니메이션 상태
    /// </summary>
    AnimationState state = AnimationState.None;

    /// <summary>
    /// 애니메이션 상태 처리용 네트워크 변수
    /// </summary>
    NetworkVariable<AnimationState> netAnimState = new NetworkVariable<AnimationState>();

    /// <summary>
    /// 채팅용 네트워크 변수
    /// </summary>
    NetworkVariable<FixedString512Bytes> chatString = new NetworkVariable<FixedString512Bytes>();

    /// <summary>
    /// 공격용 총알 프리팹
    /// </summary>
    public GameObject bulletPrefab;

    /// <summary>
    /// 공격용 오브 프리팹
    /// </summary>
    public GameObject orbPrefab;

    /// <summary>
    /// 발사 위치용 트랜스폼
    /// </summary>
    Transform firePosition;

    // 컴포넌트들
    CharacterController controller;
    Animator animator;
    PlayerInputActions inputActions;

    // 유니티 이벤트 함수들 ---------------------------------------------------------

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        inputActions = new PlayerInputActions();

        netAnimState.OnValueChanged += OnAnimStateChange;
        chatString.OnValueChanged += OnChatRecieve;

        firePosition = transform.GetChild(4);
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.MoveForward.performed += OnMoveInput;
        inputActions.Player.MoveForward.canceled += OnMoveInput;
        inputActions.Player.Rotate.performed += OnRotateInput;
        inputActions.Player.Rotate.canceled += OnRotateInput;
        inputActions.Player.Attack1.performed += OnAttack1;
        inputActions.Player.Attack2.performed += OnAttack2;
    }

    private void OnDisable()
    {
        inputActions.Player.Attack2.performed -= OnAttack2;
        inputActions.Player.Attack1.performed -= OnAttack1;
        inputActions.Player.Rotate.canceled -= OnRotateInput;
        inputActions.Player.Rotate.performed -= OnRotateInput;
        inputActions.Player.MoveForward.canceled -= OnMoveInput;
        inputActions.Player.MoveForward.performed -= OnMoveInput;
        inputActions.Player.Disable();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            GameManager.Instance.VCam.Follow = transform.GetChild(0);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            GameManager.Instance.VCam.Follow = null;
            GameManager.Instance.onPlayerDisconnected?.Invoke();
        }
    }

    private void Update()
    {
        if (netMoveDir.Value != 0.0f)
        {
            controller.SimpleMove(netMoveDir.Value * transform.forward);
        }
        transform.Rotate(0, netRotate.Value * Time.deltaTime, 0, Space.World);
    }

    // 입력 처리용 함수들 ----------------------------------------------------------

    private void OnMoveInput(InputAction.CallbackContext context)
    {
        float moveInput = context.ReadValue<float>(); // 키보드라 -1, 0, 1 중 하나
        SetMoveInput(moveInput);
    }

    private void OnRotateInput(InputAction.CallbackContext context)
    {
        float rotateInput = context.ReadValue<float>(); // 키보드라 -1, 0, 1 중 하나
        SetRotateInput(rotateInput);
    }

    private void OnAttack1(InputAction.CallbackContext context)
    {
        // 좌클릭 : 총알 발사
        Attack1();
    }

    private void OnAttack2(InputAction.CallbackContext context)
    {
        // 우클릭 : 오브 발사
        Attack2();
    }

    // 이동 및 공격 ---------------------------------------------------------------

    /// <summary>
    /// 이동 입력 처리 함수
    /// </summary>
    /// <param name="moveInput">이동 입력된 정도</param>
    void SetMoveInput(float moveInput)
    {
        if (IsOwner) // 오너일때만 이동 처리
        {
            float moveDir = moveInput * moveSpeed;  // 이동 정도 결정

            if (IsServer)
            {
                netMoveDir.Value = moveDir;         // 서버이면 직접 수정
            }
            else
            {
                MoveRequestServerRpc(moveDir);      // 서버가 아니면 서버에게 수정 요청하는 Rpc 실행
            }

            // 애니메이션 변경
            if (moveDir > 0.001f)
            {
                state = AnimationState.Walk;
            }
            else if (moveDir < -0.001f)
            {
                state = AnimationState.BackWalk;
            }
            else
            {
                state = AnimationState.Idle;
            }

            if (state != netAnimState.Value)        // 애니메이션 상태가 변경되면
            {
                // 서버인지 아닌지에 따라 수정하기
                if (IsServer)
                {
                    netAnimState.Value = state;
                }
                else
                {
                    UpdateAnimStateServerRpc(state);
                }
            }
        }
    }

    /// <summary>
    /// 회전 입력 처리 함수
    /// </summary>
    /// <param name="rotateInput">회전 입력 정도</param>
    void SetRotateInput(float rotateInput)
    {
        if (IsOwner) // 오너일때만 처리
        {
            float rotate = rotateInput * rotateSpeed;   // 회전 정도 결정

            if (IsServer)
            {
                netRotate.Value = rotate;               // 서버이면 직접 수정
            }
            else
            {
                RotateRequestServerRpc(rotate);         // 서버가 아니면 Rpc 요청
            }
        }
    }

    /// <summary>
    /// 애니메이션 상태가 변경되면 실행되는 함수
    /// </summary>
    /// <param name="previousValue">이전 값</param>
    /// <param name="newValue">새 값</param>
    private void OnAnimStateChange(AnimationState previousValue, AnimationState newValue)
    {
        animator.SetTrigger(newValue.ToString()); // 새 값으로 변경
    }

    void Attack1()
    {

    }

    void Attack2()
    {
        GameObject orb = Instantiate(orbPrefab, firePosition.position, firePosition.rotation);
    }

    // 채팅 관련 함수 -------------------------------------------------------------

    /// <summary>
    /// 채팅을 보내는 함수
    /// </summary>
    /// <param name="message"></param>
    public void SendChat(string message)
    {
        // chatString 변경
        if (IsServer)
        {
            chatString.Value = message;
        }
        else
        {
            RequestChatServerRpc(message);
        }
    }

    /// <summary>
    /// 채팅을 받았을 때 처리하는 함수 (chatString이 변경되었다 = 채팅을 받았다)
    /// </summary>
    /// <param name="previousValue"></param>
    /// <param name="newValue"></param>
    private void OnChatRecieve(FixedString512Bytes previousValue, FixedString512Bytes newValue)
    {
        GameManager.Instance.Log(newValue.ToString()); // 받은 채팅 내용을 logger에 찍기
    }

    // 서버 Rpc들 ---------------------------------------------------------------

    [ServerRpc]
    void MoveRequestServerRpc(float move)
    {
        netMoveDir.Value = move;
    }

    [ServerRpc]
    void RotateRequestServerRpc(float rotate)
    {
        netRotate.Value = rotate;
    }

    [ServerRpc]
    void UpdateAnimStateServerRpc(AnimationState state)
    {
        netAnimState.Value = state;
    }

    [ServerRpc]
    void RequestChatServerRpc(FixedString512Bytes message)
    {
        chatString.Value = message;
    }
}

/// 실습_240417
/// 1. rotate도 네트워크 변수로 적용하기
/// 2. 네트워크로 애니메이션 만들기 (netAnimState 네트워크 변수 만들어서 상태 변환 처리하기)
