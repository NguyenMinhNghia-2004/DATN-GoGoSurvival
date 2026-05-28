using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Phase F foundation scaffold (Phase F.4). Replaces DATN's
    /// <c>PlayerManager</c> + <c>JoystickManager</c> for input + movement.
    /// Driven by joystick input; scales velocity by
    /// <c>StatsBehavior.Get(StatType.Speed)</c>.
    ///
    /// <para>Dormant until <c>MigrationFlags.UseLuzartPlayerController</c> flips
    /// true. Phase F.x will:
    /// <list type="bullet">
    /// <item>Read JoystickBroadcastData (legacy movementJoystick still emits today)</item>
    /// <item>Set Rigidbody2D.linearVelocity = dir * stats.Speed</item>
    /// <item>Set Animator bool/float params for animation</item>
    /// <item>Listen to OnCollision with enemy → stats.TakeDamage</item>
    /// </list>
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class LuzartPlayerController : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _rb;
        [SerializeField] private Animator _animator;

        private void Reset()
        {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponentInChildren<Animator>();
        }

        // Phase F.x: implement OnEnable/Update + collision handling here.
        // Kept empty so adding this component to Player GO does nothing yet.
    }
}
