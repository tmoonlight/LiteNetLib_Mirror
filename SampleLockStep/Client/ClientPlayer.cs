using System.Diagnostics;
using System.Numerics;
using Code.Shared;
using LiteNetLib;
using UnityEngine;

namespace Code.Client
{ 
    public class ClientPlayer : BasePlayer
    {
        private PlayerInputPacket _nextCommand;
        private readonly ClientLogic _clientLogic;
        private readonly ClientPlayerManager _playerManager;
        private readonly LiteRingBuffer<PlayerInputPacket> _predictionPlayerStates;
        private ushort _lastServerTick;
        private const int MaxStoredCommands = 60;
        
        public Vector2 LastPosition { get; private set; }
        public float LastRotation { get; private set; }

        public int StoredCommands => _predictionPlayerStates.Count;

        public ClientPlayer(ClientLogic clientLogic, ClientPlayerManager manager, string name, byte id) : base(manager, name, id)
        {
            _playerManager = manager;
            _predictionPlayerStates = new LiteRingBuffer<PlayerInputPacket>(MaxStoredCommands);
            _clientLogic = clientLogic;
            
            _predictionPlayerStates.FastClear();
            _predictionPlayerStates.Add(new PlayerInputPacket { Id = 0 });
        }

        public void ReceiveServerState(ServerState serverState, PlayerState ourState)
        {
            if (serverState.Tick == _lastServerTick)
                return;
            _lastServerTick = serverState.Tick;

            //sync
            _position = ourState.Position;
            _rotation = ourState.Rotation;
            if (_predictionPlayerStates.Count == 0)
                return;

            ushort lastProcessedCommand = serverState.LastProcessedCommand;
            int diff = NetworkGeneral.SeqDiff(lastProcessedCommand,_predictionPlayerStates.First.Id);
            

            //apply prediction
            if (diff >= 0 && diff < _predictionPlayerStates.Count)
            {
                _predictionPlayerStates.RemoveFromStart(diff+1);
                foreach (var state in _predictionPlayerStates)
                    ApplyInput(state, LogicTimer.FixedDelta);
            }
            else
            {
                Debug.Log($"[C] Player input lag: {_predictionPlayerStates.First.Id} {lastProcessedCommand}");
                //lag
                _predictionPlayerStates.FastClear();
                _nextCommand.Id = lastProcessedCommand;
            }
        }

        public override void Spawn(Vector2 position)
        {
            base.Spawn(position);
        }

        public void SetInput(Vector2 velocity, float rotation, bool fire)
        {
            _nextCommand.Keys = 0;
            if(fire)
                _nextCommand.Keys |= MovementKeys.Fire;
            
            if (velocity.x < -0.5f)
                _nextCommand.Keys |= MovementKeys.Left;
            if (velocity.x > 0.5f)
                _nextCommand.Keys |= MovementKeys.Right;
            if (velocity.y < -0.5f)
                _nextCommand.Keys |= MovementKeys.Up;
            if (velocity.y > 0.5f)
                _nextCommand.Keys |= MovementKeys.Down;

            _nextCommand.Rotation = rotation;
        }
        
        public override void Update(float delta)
        {
            LastPosition = _position;
            LastRotation = _rotation;
            
            _nextCommand.Id = (ushort)((_nextCommand.Id + 1) % NetworkGeneral.MaxGameSequence);
            _nextCommand.ServerTick = _lastServerTick;
            ApplyInput(_nextCommand, delta);
            if (_predictionPlayerStates.IsFull)
            {
                _nextCommand.Id = _predictionPlayerStates.First.Id;
                _predictionPlayerStates.FastClear();
            }
            _predictionPlayerStates.Add(_nextCommand);
            _clientLogic.SendPacketSerializable(PacketType.Movement, _nextCommand, DeliveryMethod.ReliableOrdered);

            base.Update(delta);
        }
    }
}