using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public Player player; // temp
    public HighwayGenerator Highway;
    public EnemySpawnTable[] EnemyTables;
    public EnemyBattle[] Battles;
    public float barrierDistance;
    public Barrier forwardBarrier;
    public HighwayBarrier backBarrier;

    public VariablePool<EnemyType> Enemies;
    public List<IEnemySpawner> Spawners = new List<IEnemySpawner>();
    [HideInInspector] public List<Enemy> ActiveEnemies = new List<Enemy>();

    EnemySpawnTable currentTable;
    EnemyBattle currentBattle;
    EnemyWave currentWave;
    float TotalCost, ActiveCost, AggroCost, battlePos;
    int iTable, iBattle, iWave;
    bool battling, battleReady;

    void Start()
    {
        currentTable = EnemyTables[0];
        spawnTime = currentTable.StartInterval;
        aggroTime = currentTable.AggroInterval;
    }

    void Update()
    {
        if(Time.deltaTime == 0f)
            return;

        SpawnUpdate();
        AggroUpdate();

        if(battleReady && player.position.z > battlePos)
        {
            battleReady = false;
            StartBattle();
        }
    }
    
    float spawnTime;
    void SpawnUpdate()
    {
        if(spawnTime > 0)
        {
            spawnTime -= Time.deltaTime;
            return;
        }

        if(battling && TotalCost >= currentWave.TotalCost)
            return;
        
        spawnTime = Random.Range(currentTable.SpawnIntervalMin, currentTable.SpawnIntervalMax);
        SpawnEnemies(currentTable.SpawnCost);
    }

    void SpawnEnemies(float cost)
    {
        float spawned = 0f;
        while(spawned < cost)
        {
            if(ActiveCost >= currentTable.ActiveCost || Spawners.Count == 0)
                return;

            EnemyType enemy = currentTable.GetRandomEnemy();
            if(ActiveCost + enemy.enemyCost > currentTable.ActiveCost)
            {
                spawned += currentTable.FailCost;
                continue;
            }
            int[] seq = Util.RandomSequence(Spawners.Count);
            bool success = false;
            for(int j = 0; j < seq.Length; j++)
            {
                SpawnerVehicle spawner = (SpawnerVehicle)Spawners[seq[j]];
                if(!spawner.canSpawn || spawner.position < backBarrier.transform.position.z)
                    continue;
                if(forwardBarrier.gameObject.activeInHierarchy && spawner.position + spawner.length > forwardBarrier.transform.position.z)
                    continue;
                float dist = spawner.DistanceFrom(player.position);
                if(dist < enemy.minDistance || dist > enemy.maxDistance)
                    continue;

                Enemy nme = EnemyPool.GetObject(enemy.enemyIndex, false);
                nme.manager = this;
                nme.targetPlayer = player;
                nme.Cost = enemy.enemyCost;
                ActiveEnemies.Add(nme);
                ActiveCost += nme.Cost;
                TotalCost += nme.Cost;
                spawned += nme.Cost;
                spawner.SpawnEnemy(nme);
                nme.spawned = true;
                success = true;
                break;
            }
            if(!success)
                spawned += currentTable.FailCost;
        }
    }

    float aggroTime;
    void AggroUpdate()
    {
        if(aggroTime > 0)
        {
            aggroTime -= Time.deltaTime;
            return;
        }

        aggroTime = currentTable.AggroInterval;
        AggroEnemies();
    }

    void AggroEnemies()
    {
        if(AggroCost >= currentTable.AggroCost || ActiveEnemies.Count == 0)
            return;
        int[] seq = Util.RandomSequence(ActiveEnemies.Count);
        for(int i = 0; i < ActiveEnemies.Count; i++)
        {
            Enemy current = ActiveEnemies[seq[i]];
            if(current.gameObject.activeInHierarchy && AggroCost + current.Cost <= currentTable.AggroCost && current.TrySetAggro())
                return;
        }
    }

    public void NewBattle(float position)
    {
        battleReady = true;
        battlePos = position;
    }

    public void StartBattle()
    {
        player.freezeScore = true;
        player.score = battlePos;
        forwardBarrier.transform.position = Vector3.forward * (battlePos + barrierDistance);
        forwardBarrier.Fade(true);
        backBarrier.visible = true;
        battling = true;
        currentBattle = Battles[iBattle];
        currentTable = currentBattle.SpawnTable;
        iWave = 0;
        StartWave();
    }

    void StartWave()
    {
        if(iWave >= currentBattle.Waves.Length)
        {
            EndBattle();
            return;
        }

        currentWave = currentBattle.Waves[iWave];
        TotalCost = 0f;
        iWave++;
    }

    void EndBattle()
    {
        if(iBattle+1 < Battles.Length)
            iBattle++;
        if(iTable+1 < EnemyTables.Length)
            iTable++;
        currentTable = EnemyTables[iTable];
        spawnTime = currentTable.StartInterval;
        battling = false;
        forwardBarrier.Fade(false);
        backBarrier.visible = false;
        player.freezeScore = false;
    }

    public void RequestDie(Enemy enemy)
    {
        UpdateAggro(enemy, false);
        ActiveEnemies.Remove(enemy);
        ActiveCost -= enemy.Cost;
        if(battling && TotalCost >= currentWave.TotalCost && ActiveCost <= currentWave.EndCost)
            StartWave();
    }

    public bool UpdateAggro(Enemy enemy, bool newAggro, bool prioritize = false)
    {
        if(enemy.aggro == newAggro)
            return false;

        AggroCost += enemy.Cost * (newAggro?1f:-1f);

        enemy.aggro = newAggro;
        if(!newAggro)
            return true;
        
        if(AggroCost > currentTable.AggroCost)
        {
            if(!prioritize)
            {
                AggroCost -= enemy.Cost;
                enemy.aggro = !newAggro;
                return false;
            }

            int[] seq = Util.RandomSequence(ActiveEnemies.Count);
            for(int i = 0; i < ActiveEnemies.Count; i++)
            {
                if(ActiveEnemies[seq[i]].aggro && ActiveEnemies[seq[i]] != enemy)
                {
                    if(ActiveEnemies[seq[i]].SetAggro(false))
                        return true;
                }
            }
        }
        return true;
    }

    public List<PlatformAddress> RequestPlatformNeighbours(PlatformAddress platform, float boundsOffset)
    {
        List<PlatformAddress> answer = new List<PlatformAddress>();

        if(platform.platformIndex > 0)
        {
            PlatformAddress newPlat = platform;
            newPlat.platformIndex--;
            answer.Add(newPlat);
        }
        if(platform.platformIndex < platform.vehicle.Platforms.Length - 1)
        {
            PlatformAddress newPlat = platform;
            newPlat.platformIndex++;
            answer.Add(newPlat);
        }
        if(platform.platformIndex == 0 && platform.vehicleIndex > 0)
        {
            Vehicle nextVehicle = platform.lane.Vehicles[platform.vehicleIndex-1];
            answer.Add(new PlatformAddress(platform.lane, nextVehicle, nextVehicle.Platforms.Length - 1));
        }
        if(platform.platformIndex == platform.vehicle.Platforms.Length - 1 && platform.vehicleIndex < platform.lane.Vehicles.Count - 1)
        {
            Vehicle nextVehicle = platform.lane.Vehicles[platform.vehicleIndex+1];
            answer.Add(new PlatformAddress(platform.lane, nextVehicle, 0));
        }
        float boundsStart = platform.vehicle.position + platform.platform.BoundsStart.y - boundsOffset;
        float boundsEnd = platform.vehicle.position + platform.platform.BoundsEnd.y + boundsOffset;
        int laneIndex = platform.laneIndex - 1;
        for(int h = 0; h < 2; h++)
        {
            if(laneIndex >= 0 && laneIndex < Highway.Lanes.Length)
            {
                Lane nextLane = Highway.Lanes[laneIndex];
                for(int i = 0; i < nextLane.Vehicles.Count; i++)
                {
                    Vehicle vehicle = nextLane.Vehicles[i];
                    if(vehicle.position > boundsEnd)
                        break;
                    if(vehicle.position + vehicle.length < boundsStart)
                        continue;
                    
                    for(int j = 0; j < vehicle.Platforms.Length; j++)
                    {
                        Platform plat = vehicle.Platforms[j];
                        if(vehicle.position + plat.BoundsStart.y > boundsEnd || vehicle.position + plat.BoundsEnd.y < boundsStart)
                            continue;
                        
                        answer.Add(new PlatformAddress(nextLane, vehicle, j));
                    }
                }
            }
            laneIndex = platform.laneIndex + 1;
        }

        return answer;
    }
}

[System.Serializable]
public struct EnemyType
{
    public int enemyIndex;
    public float enemyCost, minDistance, maxDistance;
}