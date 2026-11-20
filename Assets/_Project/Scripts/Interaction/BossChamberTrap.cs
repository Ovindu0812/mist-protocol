using System.Collections;
using MistProtocol.AI.Agents;
using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.Interaction
{
    /// <summary>
    /// Traps the player inside the Containment Vault when they follow Appa's clues into the room.
    /// Locks the door behind them and awakens the giant Titan Automaton.
    /// </summary>
    public class BossChamberTrap : MonoBehaviour
    {
        [SerializeField] private PhysicsDoor entranceDoor;
        [SerializeField] private Light[] emergencyLights;
        [SerializeField] private AgentBase titanBoss;

        private bool _triggered;
        private Health _titanHealth;
        private Vector3 _lastTitanPosition;
        private bool _titanDefeated;

        public void Bind(PhysicsDoor door, Light[] lights, AgentBase titan)
        {
            entranceDoor = door;
            emergencyLights = lights;
            titanBoss = titan;
            if (entranceDoor != null) entranceDoor.SetLocked(false);
            HookTitanHealth();
        }

        private void Awake()
        {
            EnsureDoorUnlocked();
        }

        private void Start()
        {
            EnsureDoorUnlocked();
            HookTitanHealth();
            EnsureHoldingCell();
        }

        private void EnsureHoldingCell()
        {
            if (HoldingCell.Instance == null && FindAnyObjectByType<HoldingCell>() == null)
            {
                var cellGo = new GameObject("HoldingCell_SectorE");
                cellGo.AddComponent<HoldingCell>();
            }
        }

        private void EnsureDoorUnlocked()
        {
            if (entranceDoor == null)
            {
                var d = GameObject.Find("Door_Containment");
                if (d != null) entranceDoor = d.GetComponent<PhysicsDoor>();
            }
            if (entranceDoor != null && entranceDoor.Locked)
            {
                entranceDoor.SetLocked(false);
            }
        }

        private void HookTitanHealth()
        {
            if (titanBoss != null && _titanHealth == null)
            {
                _titanHealth = titanBoss.GetComponent<Health>();
                if (_titanHealth != null)
                {
                    _titanHealth.Died += OnTitanDied;
                }
            }
        }

        private void Update()
        {
            if (titanBoss != null && !_titanDefeated)
            {
                _lastTitanPosition = titanBoss.transform.position;
            }
        }

        private void OnDestroy()
        {
            if (_titanHealth != null)
            {
                _titanHealth.Died -= OnTitanDied;
            }
        }

        private void OnTitanDied()
        {
            if (_titanDefeated) return;
            _titanDefeated = true;
            StartCoroutine(TitanDefeatedSequence());
        }

        private IEnumerator TitanDefeatedSequence()
        {
            yield return new WaitForSeconds(1.0f);

            Vector3 spawnPos = _lastTitanPosition != Vector3.zero ? _lastTitanPosition : (transform.position + Vector3.forward * 10f);
            spawnPos.y = Mathf.Max(spawnPos.y, 0.05f);

            // 1. Spawn Master Prison Cell Key and Appa's marked blueprint map at Titan wreckage
            MasterCellKey.SpawnKey(spawnPos);
            FatherClue.SpawnMap(spawnPos + Vector3.right * 0.8f);

            // 2. Unlock and open the chamber door
            if (entranceDoor != null)
            {
                entranceDoor.SetLocked(false);
                entranceDoor.OpenAwayFrom(transform.position);
            }

            // 3. Calm amber/green lighting after alert
            if (emergencyLights != null)
            {
                foreach (var l in emergencyLights)
                {
                    if (l != null)
                    {
                        l.color = new Color(0.2f, 0.9f, 0.5f);
                        l.intensity = 2f;
                    }
                }
            }

            // 4. Audio and reactions
            ProceduralAudio.PlayAt("explosion", spawnPos, 0.8f, 0.7f);
            ProceduralAudio.PlayAt("pickup", spawnPos, 0.9f, 1.1f);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTitanDefeated();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered) return;
            if (!other.CompareTag("Player") && other.GetComponentInParent<Player.PlayerController>() == null) return;

            _triggered = true;
            StartCoroutine(SpringTheTrap());
        }

        private IEnumerator SpringTheTrap()
        {
            yield return new WaitForSeconds(0.4f);

            // 1. Keep the containment door unlocked so the player can enter/move freely
            if (entranceDoor != null)
            {
                entranceDoor.SetLocked(false);
            }

            ProceduralAudio.PlayAt("door", transform.position, 1f, 0.6f);
            ProceduralAudio.PlayAt("explosion", transform.position, 0.4f, 0.5f);

            // 2. Red emergency alert lights
            if (emergencyLights != null)
            {
                foreach (var l in emergencyLights)
                {
                    if (l != null)
                    {
                        l.enabled = true;
                        l.color = new Color(1f, 0.15f, 0.1f);
                        l.intensity = 4.5f;
                    }
                }
            }

            // 3. Audio & OVERSEER announcement
            ProceduralAudio.PlayAt("hum", transform.position, 0.5f, 0.7f);
            GameEvents.Overseer("OVERSEER: Did you think he dropped those by accident, Nila? You followed his breadcrumbs straight into the Containment Vault. Meet the Titan.");
            GameEvents.Objective("SURVIVE: Evade the giant Titan Automaton! Use crates and stealth to hide.");

            // 4. Awaken the Titan
            if (titanBoss != null)
            {
                titanBoss.gameObject.SetActive(true);
            }
        }
    }
}
