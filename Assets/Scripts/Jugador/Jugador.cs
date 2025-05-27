﻿using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using TMPro;


public class Jugador : NetworkBehaviour
{
    #region Parametros
    private Rigidbody _rb;
    private InfoJugador _usernamePanel;

    [Header("Movimiento")]
    private Vector3 _moveDirection = new Vector3();
    public float moveSpeed = 10;
    public float speedLimit = 10;
    
    [Header("Gravedad")]
    public float gravityNormal = 50f;
    public float gravityJump = 9.81f;
    private bool _isJumping;

    [Header("Camara")]
    public Transform transformCam;
    public float camSpeed = 10f;
    private float _pitch = 0;
    private float _yaw = 0;
    private Vector2 _camInput = new Vector2();
    public float maxPitch = 60;
    public InputAction lookAction;

    [Header("Armas")]
    public Transform transformCannon;

    [Header("HP"), SyncVar(hook = nameof(HealthChanged))]
    private int hp = 5;
    private int maxHp;
    public Transform healthBar;

    [SyncVar(hook = nameof(AliveHasChanged))]
    bool isAlive = true;

    public float respawnTime = 5;
    

    [Header("NameTag")]
    public TextMeshPro nametagObject;
    [SyncVar(hook = nameof(NameChanged))]
    private string username;


    [Header("Helmets")]
    [SyncVar(hook = nameof(OnHelmetChanged))]
    [SerializeField] private string HelmetName = "Nothing"; // Default value

    public Transform helmetObject;
    public static string helmSelected = "Nothing"; // Default value
    #endregion
    #region Unity
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        maxHp = hp;
    }
    
    void FixedUpdate()
    {
        if(!isLocalPlayer)return;
        Vector3 flat = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        Quaternion orientation = Quaternion.LookRotation(flat);
        Vector3 worldMoveDirection = orientation * _moveDirection;
        _rb.AddForce(worldMoveDirection * moveSpeed, ForceMode.Impulse);

        Vector3 horizontalVelocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
        if (horizontalVelocity.magnitude > speedLimit)
        {
            Vector3 limitedVelocity = horizontalVelocity.normalized * speedLimit;
            _rb.linearVelocity = new Vector3(limitedVelocity.x, _rb.linearVelocity.y, limitedVelocity.z);
        }

        if (!_isJumping)
        {
            _rb.AddForce(Vector3.down * gravityNormal, ForceMode.Acceleration);
        }

    }
    void Update()
    {
        //_camInput = lookAction.ReadValue<Vector2>();
        _camInput = Mouse.current.delta.ReadValue();
        _yaw += _camInput.x * camSpeed * Time.deltaTime;
        _pitch += _camInput.y * camSpeed * Time.deltaTime;

        _pitch = _pitch > maxPitch ? maxPitch : _pitch < (-maxPitch) ? -maxPitch : _pitch;
        transform.eulerAngles = new Vector3(0, _yaw,0);
        transformCam.eulerAngles = new Vector3(-_pitch, transformCam.rotation.eulerAngles.y, transformCam.rotation.eulerAngles.z);
    }
    #endregion
    #region PewPew
    [Command]
    private void CommandShoot(Vector3 origen, Vector3 direccion)
    {
        if(Physics.Raycast(origen, direccion,  out RaycastHit hit, 100f))
        {
            if(hit.collider.gameObject.TryGetComponent<Jugador>(out Jugador elGolpeado) == true)
            {
                Debug.Log("Me pijie al: " + elGolpeado.gameObject.name);
                elGolpeado.TakeDamage(1);
            }
        }
    }
    #endregion
    #region HP
    [Server]
    public void TakeDamage(int amount)
    {
        if(hp <= 0) { hp = 0; return; }

        hp -= amount;
        if(hp < 0)
        {
            KillPlayer();//DED
        }
    }
    private void HealthChanged(int oldHealth, int newHealth)
    {
        healthBar.localScale = new Vector3(healthBar.localScale.y, (float) newHealth / 5, healthBar.localScale.z);
    }
    [Server]
    private void KillPlayer()
    {
        isAlive = false;
    }
    private void AliveHasChanged(bool oldBool, bool newBool) 
    {
        if (newBool == false)
        {
            transform.localScale = new Vector3(1, 0.3f, 1);
            transformCam.gameObject.SetActive(false);
            gameObject.GetComponent<PlayerInput>().enabled = false;
            healthBar.gameObject.SetActive(false);
            if (!isLocalPlayer) return;
            Invoke("CommandRespawn", respawnTime);
        }
        else
        {
            transform.localScale = Vector3.one;
            healthBar.gameObject.SetActive(true);
            transform.position = ShooterNetworkManager.singleton.GetStartPosition().position;

            if (!isLocalPlayer) return;
            transformCam.gameObject.SetActive(true);
            gameObject.GetComponent<PlayerInput>().enabled = true;


        }
    }
    #endregion

    #region Helmet
    private void OnHelmetChanged(string _, string newHelmetName)
    {
        ActivateHelmet(newHelmetName);
    }

    private void ActivateHelmet(string helmetName)
    {
        if (HelmetName == "Nothing") return;
        else
        {
            foreach (Transform helmet in helmetObject)
            {
                helmet.gameObject.SetActive(helmet.name == helmetName);
            }
        }
    }

    [Command]
    public void CmdSetHelmet(string helmetName)
    {
        HelmetName = helmetName;
    }
    #endregion

    #region Input

    public void Disparar(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;
        if (!context.performed)
        {
            return;
        }
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 puntoObjetivo;
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            puntoObjetivo = hit.point;
        }
        else
        {
            puntoObjetivo = ray.origin + ray.direction * 100f;
        }

        Vector3 direccion = (puntoObjetivo - transformCannon.position).normalized;

        CommandShoot(transformCannon.position, direccion);
    }
    public void SetMovement(InputAction.CallbackContext context)
    {
        if(!isLocalPlayer)return;
        Debug.Log("Moving");
        var dir = context.ReadValue<Vector2>().normalized;
        _moveDirection = new Vector3(dir.x, 0, dir.y);
        
    }

    public void SetLookDirection(InputAction.CallbackContext context)
    {
       
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        Cursor.lockState = CursorLockMode.Locked;
        _usernamePanel = GameObject.FindGameObjectWithTag("Username").GetComponent<InfoJugador>();
        username = _usernamePanel.PideUsuario();
        _usernamePanel.gameObject.SetActive(false);

        CmdSetHelmet(helmSelected);
        ActivateHelmet(HelmetName);

    }
    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        PlayerInput playerInput = GetComponent<PlayerInput>();
        playerInput.enabled = true;
        lookAction = playerInput.actions.actionMaps[0].actions[1];
        transformCam.gameObject.SetActive(true);
        nametagObject.gameObject.SetActive(false);
        healthBar.gameObject.SetActive(false);
    }
    #endregion

    [Command]
    private void CommandChangeName(string maiName)
    {
        username = maiName;
    }

    private void NameChanged(string oldName, string newName)
    {
        nametagObject.text = newName;
        name = newName;
    }
    [Command]
    private void CommandRespawn()
    {
        isAlive = true;
        hp = maxHp;
    }
}
