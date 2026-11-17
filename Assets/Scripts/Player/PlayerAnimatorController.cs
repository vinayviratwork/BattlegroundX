using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimatorController : NetworkBehaviour
{
    
    [Header("Smoothing")]
    [SerializeField] private float speedDampTime  = 0.1f;
    [SerializeField] private float leanDampTime   = 0.05f;

    [Header("IK — optional")]
    [SerializeField] private bool  enableHandIK   = true;
    [SerializeField] private Transform leftHandTarget;   
    [SerializeField] private Transform rightHandTarget;

    
    private Animator                 _anim;
    private NetworkPlayerController  _ctrl;
    private NetworkWeapon            _weapon;
    private NetworkHealth            _health;
    private CharacterController      _cc;

    
    private static readonly int PSpeed       = Animator.StringToHash("Speed");
    private static readonly int PCrouching   = Animator.StringToHash("IsCrouching");
    private static readonly int PAiming      = Animator.StringToHash("IsAiming");
    private static readonly int PGrounded    = Animator.StringToHash("IsGrounded");
    private static readonly int PDead        = Animator.StringToHash("IsDead");
    private static readonly int PFire        = Animator.StringToHash("Fire");
    private static readonly int PReload      = Animator.StringToHash("Reload");
    private static readonly int PJump        = Animator.StringToHash("Jump");
    private static readonly int PTakeDamage  = Animator.StringToHash("TakeDamage");
    private static readonly int PWeaponIndex = Animator.StringToHash("WeaponIndex");

    
    private bool  _wasReloading;
    private bool  _wasGrounded = true;
    private int   _lastWeaponIndex = -1;
    private int   _lastHealth = 100;

    
    public override void OnNetworkSpawn()
    {
        _anim   = GetComponent<Animator>();
        _ctrl   = GetComponent<NetworkPlayerController>();
        _weapon = GetComponent<NetworkWeapon>();
        _health = GetComponent<NetworkHealth>();
        _cc     = GetComponent<CharacterController>();

        if (_health != null)
            _health.OnHealthChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (_health != null)
            _health.OnHealthChanged -= OnHealthChanged;
    }

    
    private void Update()
    {
        if (_anim == null) return;

        DriveLocomotion();
        DriveWeaponState();
        DriveDeath();
    }

    
    private void DriveLocomotion()
    {
        bool crouching = _ctrl != null && _ctrl.IsCrouching.Value;
        bool aiming    = _ctrl != null && _ctrl.IsAiming.Value;
        bool running   = _ctrl != null && _ctrl.IsRunning.Value;

        
        float targetSpeed = 0f;
        if (_ctrl != null)
        {
            
            
            if (IsOwner && _cc != null)
            {
                float hVel = new Vector3(_cc.velocity.x, 0, _cc.velocity.z).magnitude;
                targetSpeed = hVel < 0.3f ? 0f :
                              hVel < 5f   ? 1f :
                              hVel < 8f   ? 2f : 3f;
            }
            else
            {
                targetSpeed = running ? 2f : 0f;
            }
        }

        if (crouching && targetSpeed > 0f) targetSpeed = 1f; 

        _anim.SetFloat(PSpeed,     targetSpeed, speedDampTime, Time.deltaTime);
        _anim.SetBool(PCrouching,  crouching);
        _anim.SetBool(PAiming,     aiming);

        
        bool grounded = IsOwner
            ? (_cc == null || _cc.isGrounded)
            : true; 
        _anim.SetBool(PGrounded, grounded);

        
        if (!grounded && _wasGrounded)
            _anim.SetTrigger(PJump);
        _wasGrounded = grounded;
    }

    
    private void DriveWeaponState()
    {
        if (_weapon == null) return;

        int weaponIdx  = _weapon.CurrentWeaponIndex.Value;
        bool reloading = _weapon.IsReloading.Value;

        
        if (weaponIdx != _lastWeaponIndex)
        {
            _anim.SetInteger(PWeaponIndex, weaponIdx);
            _lastWeaponIndex = weaponIdx;
        }

        
        if (reloading && !_wasReloading)
            _anim.SetTrigger(PReload);
        _wasReloading = reloading;
    }

    
    private void DriveDeath()
    {
        bool dead = _health != null && _health.IsDead.Value;
        _anim.SetBool(PDead, dead);
    }

    
    private void OnHealthChanged(int oldHp, int newHp)
    {
        if (newHp < oldHp && newHp > 0)
            _anim.SetTrigger(PTakeDamage);
    }

    
    public void TriggerFireAnimation()
    {
        if (_anim != null) _anim.SetTrigger(PFire);
    }

    
    private void OnAnimatorIK(int layerIndex)
    {
        if (!enableHandIK || _anim == null) return;

        if (rightHandTarget != null)
        {
            _anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
            _anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);
            _anim.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
            _anim.SetIKRotation(AvatarIKGoal.RightHand, rightHandTarget.rotation);
        }

        if (leftHandTarget != null)
        {
            _anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
            _anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1f);
            _anim.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
            _anim.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);
        }
    }
}