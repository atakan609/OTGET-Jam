using System;
using UnityEngine;
using Core;

namespace Managers
{
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        GameOver
    }

    public class GameManager : Singleton<GameManager>
    {
        public GameState CurrentState { get; private set; }
        public static event Action<GameState> OnStateChanged;

        protected override void Awake()
        {
            base.Awake();
            UpdateState(GameState.Menu);
        }

        public void UpdateState(GameState newState)
        {
            CurrentState = newState;
            
            switch (newState)
            {
                case GameState.Menu:
                    break;
                case GameState.Playing:
                    break;
                case GameState.Paused:
                    break;
                case GameState.GameOver:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
            }

            OnStateChanged?.Invoke(newState);
        }
    }
}
