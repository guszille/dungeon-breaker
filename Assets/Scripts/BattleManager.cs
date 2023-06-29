using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    public enum BattleMode { ON, OFF }

    public class OnBattleModeChangedEventArgs : EventArgs { public BattleMode battleMode; }
    public event EventHandler<OnBattleModeChangedEventArgs> OnBattleModeChanged;

    public class OnBattleTurnChangedEventArgs : EventArgs { public BattleAgent.AgentType agentType; }
    public event EventHandler<OnBattleTurnChangedEventArgs> OnBattleTurnChanged;
    
    public class OnBattleActionChangedEventArgs : EventArgs { public BattleAgent.BattleAction battleAction; }
    public event EventHandler<OnBattleActionChangedEventArgs> OnBattleActionChanged;

    [SerializeField] private List<EnemyController> enemiesInTheRoom;  // FIXME: temporary solution to get the enemies.

    private BattleMode battleMode;
    private List<BattleAgent> agentsInBattle;
    private int currentTurnOwner;

    private void Awake()
    {
        Instance = this;

        battleMode = BattleMode.OFF;
        agentsInBattle = new List<BattleAgent>();
        currentTurnOwner = 0;
    }

    private void ResetBattleManager()
    {
        agentsInBattle = new List<BattleAgent>();
        currentTurnOwner = 0;
    }

    private void SetupAgentsInBattle()
    {
        MainCharacterController.Instance.PutInBattle();

        agentsInBattle.Add(MainCharacterController.Instance);

        foreach (EnemyController enemy in enemiesInTheRoom)
        {
            enemy.PutInBattle();

            agentsInBattle.Add(enemy);
        }

        agentsInBattle = agentsInBattle.OrderBy(x => x.GetInitiative()).ToList();
    }

    private void GiveAgentTurnOwnersip(int index)
    {
        agentsInBattle[index].OnBattleActionChanged += BattleAgent_OnBattleActionChanged;
        agentsInBattle[index].OnBattleTurnEnded += BattleAgent_OnBattleTurnEnded;
    }

    private void RevokeAgentTurnOwnership(int index)
    {
        agentsInBattle[index].OnBattleActionChanged -= BattleAgent_OnBattleActionChanged;
        agentsInBattle[index].OnBattleTurnEnded -= BattleAgent_OnBattleTurnEnded;
    }

    private void BattleAgent_OnBattleActionChanged(object sender, BattleAgent.OnBattleActionChangedEventArgs e)
    {
        OnBattleActionChanged?.Invoke(this, new OnBattleActionChangedEventArgs { battleAction = e.action });
    }

    private void BattleAgent_OnBattleTurnEnded(object sender, EventArgs e)
    {
        ChangeTurnOwnership();
    }

    private void StartCurrentTurn()
    {
        GiveAgentTurnOwnersip(currentTurnOwner);
        agentsInBattle[currentTurnOwner].StartTurn();

        OnBattleTurnChanged?.Invoke(this, new OnBattleTurnChangedEventArgs {
            agentType = agentsInBattle[currentTurnOwner].GetAgentType()
        });
    }

    private void CheckIfBattleIsOver()
    {
        bool battleIsOver = true;

        foreach (BattleAgent agent in agentsInBattle)
        {
            if (agent.GetAgentType() == BattleAgent.AgentType.PLAYER)
            {
                if (agent.GetHitPoints() == 0f)
                {
                    break;
                }
            }
            else
            {
                if (agent.GetHitPoints() > 0f)
                {
                    battleIsOver = false;
                    break;
                }
            }
        }

        if (battleIsOver)
        {
            battleMode = BattleMode.OFF;
            OnBattleModeChanged?.Invoke(this, new OnBattleModeChangedEventArgs { battleMode = battleMode });

            foreach (BattleAgent agent in agentsInBattle)
            {
                agent.RemoveFromBattle();
            }

            ResetBattleManager();
        }
    }

    private void ChangeTurnOwnership()
    {
        RevokeAgentTurnOwnership(currentTurnOwner);
        CheckIfBattleIsOver();

        if (battleMode == BattleMode.ON)
        {
            currentTurnOwner = (currentTurnOwner + 1) % agentsInBattle.Count; // Move to next agent.

            StartCurrentTurn();
        }
    }

    public void RequestBattleToStart()
    {
        if (battleMode == BattleMode.OFF)
        {
            ResetBattleManager();
            SetupAgentsInBattle();

            battleMode = BattleMode.ON;
            OnBattleModeChanged?.Invoke(this, new OnBattleModeChangedEventArgs { battleMode = battleMode });

            StartCurrentTurn();
        }
    }

    public bool HasEnemyOnPosition(Vector3 worldPosition)
    {
        Vector2Int cellPosition = NavigationManager.Instance.ConvertToCellPosition(worldPosition);

        foreach (EnemyController enemy in enemiesInTheRoom)
        {
            if (cellPosition == NavigationManager.Instance.ConvertToCellPosition(enemy.transform.position))
            {
                return true;
            }
        }

        return false;
    }

    public BattleAgent GetEnemyOnCellPosition(Vector2Int cellPosition)
    {
        foreach (EnemyController enemy in enemiesInTheRoom)
        {
            if (cellPosition == NavigationManager.Instance.ConvertToCellPosition(enemy.transform.position))
            {
                return enemy;
            }
        }

        return null;
    }

    public List<Vector2Int> GetAllEnemiesCellPositions()
    {
        List<Vector2Int> enemiesCellPositions = new List<Vector2Int>();

        foreach (EnemyController enemy in enemiesInTheRoom)
        {
            enemiesCellPositions.Add(NavigationManager.Instance.ConvertToCellPosition(enemy.transform.position));
        }

        return enemiesCellPositions;
    }

    public BattleAgent GetTurnOwner()
    {
        return agentsInBattle[currentTurnOwner];
    }
}
