using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;

public struct MoveData : IReplicateData
{
    public float Horizontal;
    public float Vertical;
    private uint _tick;
    public void Dispose() { }
    public uint GetTick() => _tick;
    public void SetTick(uint value) => _tick = value;
}

public struct ReconcileData : IReconcileData
{
    public Vector3 Position;
    public Quaternion Rotation;
    public float VerticalVelocity;
    public bool IsAlive;
    private uint _tick;
    public void Dispose() { }
    public uint GetTick() => _tick;
    public void SetTick(uint value) => _tick = value;
}

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementPredicted : NetworkBehaviour
{
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _gravity = -9.81f;
    [SerializeField] private PlayerNetwork _playerNetwork;

    private CharacterController _cc;
    private float _verticalVelocity;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (_playerNetwork == null) _playerNetwork = GetComponent<PlayerNetwork>();
    }

    public override void OnStarClient()
    {
        base.TimeManager.OnTick += OnTick;
    }

    public override void OnStopNetwork()
    {
        base.TimeManager.OnTick -= OnTick;
    }

    private void OnTick()
    {
        bool alive = _playerNetwork != null && _playerNetwork.IsAlive.Value;
        if (IsOwner && alive)
        {
            Replicate(new MoveData
            {
                Horizontal = Input.GetAxisRaw("Horizontal"),
                Vertical = Input.GetAxisRaw("Vertical")
            });
        }
        else
        {
            Replicate(default);
        }

        if (IsServerInitialized) CreateReconcile();
    }

    public override void CreateReconcile()
    {
        if (_playerNetwork == null) return;
        Reconcile(new ReconcileData
        {
            Position = transform.position,
            Rotation = transform.rotation,
            VerticalVelocity = _verticalVelocity,
            IsAlive = _playerNetwork.IsAlive.Value
        });
    }

    [Replicate]
    private void Replicate(MoveData md, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
    {
        if (_playerNetwork == null || !_playerNetwork.IsAlive.Value) return;
        if (_cc == null || !_cc.enabled) return;

        Vector3 move = new Vector3(md.Horizontal, 0f, md.Vertical);
        if (move.sqrMagnitude > 1f) move.Normalize();

        _verticalVelocity += _gravity * (float)TimeManager.TickDelta;
        if (_cc.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;

        move.y = _verticalVelocity;
        _cc.Move(move * _speed * (float)TimeManager.TickDelta);
    }

    [Reconcile]
    private void Reconcile(ReconcileData rd, Channel channel = Channel.Unreliable)
    {
        if (_cc == null) return;
        _cc.enabled = false;
        transform.position = rd.Position;
        transform.rotation = rd.Rotation;
        _verticalVelocity = rd.VerticalVelocity;
        _cc.enabled = rd.IsAlive;   // включаем, только если живы
    }
}