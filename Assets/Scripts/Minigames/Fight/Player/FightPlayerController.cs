﻿using UnityEngine;
using UnityEngine.InputSystem;
using DDEngine.Utils;
using DDEngine.Utils.FSM;

namespace DDEngine.Minigame.Fight
{
    public class FightPlayerController : StateMachine<FighterState>
    {
        public GameObject animationPrefabObject;
        public GameObject picturePrefabObject;
        public FightOpponentController directOpponent;
        public float timeTriggerIntent = 0.1f;

        private bool continueTriggered = false;

        private void RegOrUnregControl(bool register)
        {
            var fightActionMap = GameInputManager.Instance.FightMinigameActionMap;

            InputAction jabPressed = fightActionMap.FindAction("Jab Pressed");
            InputAction punchPressed = fightActionMap.FindAction("Punch Pressed");
            InputAction strongPunchPressed = fightActionMap.FindAction("Strong Punch Pressed");
            InputAction dodgePressed = fightActionMap.FindAction("Dodge Pressed");
            InputAction continueRequested = fightActionMap.FindAction("Continue Requested");

            if (register)
            {
                jabPressed.performed += OnJabPressed;
                punchPressed.performed += OnPunchPressed;
                strongPunchPressed.performed += OnStrongPunchPressed;
                dodgePressed.performed += OnDodgePressed;
                continueRequested.performed += OnContinueRequested;
            }
            else
            {
                jabPressed.performed -= OnJabPressed;
                punchPressed.performed -= OnPunchPressed;
                strongPunchPressed.performed -= OnStrongPunchPressed;
                dodgePressed.performed -= OnDodgePressed;
                continueRequested.performed -= OnContinueRequested;
            }
        }

        private void Awake()
        {
            RegOrUnregControl(true);
        }

        private void OnDestroy()
        {
            RegOrUnregControl(false);
        }

        public void Setup(FightPlayerInfo playerInfo, string wonScript)
        {
            transform.localPosition = GameUtils.ToUnityCoordinates(playerInfo.Position);

            base.AddState(FighterState.Idle, new FightPlayerIdleState(this, playerInfo));
            base.AddState(FighterState.Dodging, new FightPlayerDodgingState(this, playerInfo));
            base.AddState(FighterState.Attacking, new FightPlayerAttackingState(this, playerInfo));
            base.AddState(FighterState.TakingDamage, new FightPlayerTakingDamageState(this, playerInfo));
            base.AddState(FighterState.KnockedOut, new FightPlayerKnockedOutState(this, playerInfo, wonScript));
        }

        protected override FighterState GetInitialState()
        {
            return FighterState.Idle;
        }

        public void OnJabPressed(InputAction.CallbackContext context)
        {
            GiveData(FightPunchType.Jab);
        }

        public void OnPunchPressed(InputAction.CallbackContext context)
        {
            GiveData(FightPunchType.Punch);
        }

        public void OnStrongPunchPressed(InputAction.CallbackContext context)
        {
            GiveData(FightPunchType.StrongPunch);
        }

        public void OnDodgePressed(InputAction.CallbackContext context)
        {
            GiveData(FightPunchType.Dodging);
        }

        public void OnContinueRequested(InputAction.CallbackContext context)
        {
            if (continueTriggered)
            {
                return;
            }

            // If either one of us is knocked out, end the fight and disable input
            if ((directOpponent.CurrentState == FighterState.KnockedOut) ||
                (CurrentState == FighterState.KnockedOut))
            {
                FightEndIntent endIntent = new FightEndIntent();

                directOpponent.GiveDataFrom(this, endIntent);
                GiveData(endIntent);

                continueTriggered = true;
            }
        }
    }
}
