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
            HookTitanHealth();
        }

        private void Start()
        {
            HookTitanHealth();
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

            // 1. Spawn Appa's marked blueprint map at Titan wreckage
            FatherClue.SpawnMap(spawnPos);

            // 2. Unlock and open the chamber door
            if (entranceDoor != null)
            {
                entranceDoor.SetLocked(false);
                entranceDoor.Open();
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

            GameEvents.Overseer("OVERSEER: Unit-94 destroyed?! Impossible... He left something for you in the wreckage, didn't he?");
            GameEvents.Objective("TITAN DESTROYED: Collect Appa's Factory Blueprint from the wreckage!");
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

            // 1. Slam the door shut and lock it
            if (entranceDoor != null)
            {
                entranceDoor.Close();
                entranceDoor.SetLocked(true);
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
