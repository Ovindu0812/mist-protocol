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

        public void Bind(PhysicsDoor door, Light[] lights, AgentBase titan)
        {
            entranceDoor = door;
            emergencyLights = lights;
            titanBoss = titan;
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
