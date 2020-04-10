using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

public class Player : NetworkBehaviour, IDamageable {
    public enum PlayerTool {
        Pickaxe,
        ObstacleVertical,
        ObstacleRamp,
        ObstacleHorizontal,
        None
    }

    [Header("Focal Point variables")]
    [SerializeField] private GameObject focalPoint;
    [SerializeField] private GameObject rotationPoint;
    [SerializeField] private float focalDistance;
    [SerializeField] private float focalSmoothness;
    [SerializeField] private KeyCode changeFocalSideKey;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactionKey;
    [SerializeField] private float interactionDistance;

    [Header("Gameplay")]
    [SerializeField] private KeyCode toolSwitchKey;
    [SerializeField] private PlayerTool tool;
    [SerializeField] private int initialResourceCount;
    [SerializeField] private float resourceCollectionCooldown;

    [Header("Obstacles")]
    [SerializeField] private GameObject[] obstaclePrefabs;

    [Header("Weapons")]
    [SerializeField] private GameObject shootOrigin;
    [SerializeField] private GameObject rocketPrefab;
    [SerializeField] private GameObject modelAxe;
    [SerializeField] private GameObject modelPistol;
    [SerializeField] private GameObject modelMachineGun;
    [SerializeField] private GameObject modelShotgun;
    [SerializeField] private GameObject modelSniper;
    [SerializeField] private GameObject modelRocketLauncher;

    [Header("Audio")]
    [SerializeField] private AudioSource soundInterface;
    [SerializeField] private AudioSource[] soundsWeapons;
    [SerializeField] private AudioSource[] soundsFootsteps;
    [SerializeField] private AudioSource soundJump;
    [SerializeField] private AudioSource soundLand;
    [SerializeField] private float stepInterval;
    [SerializeField] private AudioSource soundHit;

    [Header("Visuals")]
    [SerializeField] private GameObject playerContainer;
    [SerializeField] private GameObject energyBall;

    [Header("Energy State")]
    [SerializeField] private float energyFallingSpeed;
    [SerializeField] private float energyMovingSpeed;

    [Header("Debug")]
    [SerializeField] private GameObject debugPositionPrefab;

    private bool isFocalPointOnLeft = true;
    private int resources = 0;
    private float resourceCollectionCooldownTimer = 0;
    private GameObject currentObstacle;
    private bool obstaclePlacementLock;

    private List<Weapon> weapons;

    private Weapon weapon;

    private HUDController hud;
    private GameCamera gameCamera;
    private Rigidbody playerRigidBody;
    private GameObject obstaclePlacementContainer;
    private GameObject obstacleContainer;
    private int obstacleToAddIndex;
    private Health health;

    private float stepTimer;

    private Animator playerAnimator;
    private NetworkAnimator playerNetworkAnimator;
    private string modelName; // Current weapon or tool the player is holding
    private bool isInEnergyMode = false;
    private bool shouldAllowMovementInEnergyMode = false;

    public bool ShouldAllowMovementEnergyMode
    {
        get
        {
            return shouldAllowMovementInEnergyMode;
        }
        set
        {
            shouldAllowMovementInEnergyMode = value;

            if(value)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
          
        }
    }

    public bool IsInEnergyMode
    {
        get
        {
            return isInEnergyMode;
        }
        set
        {
            if(value)
            {
                energyBall.SetActive(true);
                playerContainer.transform.localScale = Vector3.zero;
                playerRigidBody.useGravity = false;
            }
            else
            {
                playerRigidBody.useGravity = true;
                energyBall.SetActive(false);
                playerContainer.transform.localScale = Vector3.one;
            }

            isInEnergyMode = value;
        }
    }

	// Use this for initialization
	void Start () {
      

        // Initialize values
        resources = initialResourceCount;
        weapons = new List<Weapon>();
        health = GetComponent<Health>();
        playerRigidBody = GetComponent<Rigidbody>();
        health.OnHealthChanged += OnHealthChanged;

        if (isLocalPlayer)
        {
            // Game camera
            gameCamera = FindObjectOfType<GameCamera>();
            obstaclePlacementContainer = gameCamera.ObstaclePlacementContainer;
            gameCamera.Target = focalPoint;
            gameCamera.RotationAnchorObject = rotationPoint;

            // HUD elements
            hud = FindObjectOfType<HUDController>();

            if(isServer)
            {
                hud.ShowScreen("server");
            }
            else
            {
                hud.ShowScreen("client");
            }
            hud.OnStartMatch += OnServerStartMatch;
            hud.Health = health.Value;
            hud.Resources = resources;
            hud.Tool = tool;
            hud.UpdateWeapon(null);

            // Listen to events.
            GetComponent<UnityStandardAssets.Characters.ThirdPerson.ThirdPersonCharacter>().OnFootstep += OnFootstep;
            GetComponent<UnityStandardAssets.Characters.ThirdPerson.ThirdPersonCharacter>().OnJump += OnJump;

            // Get animator.
            playerAnimator = GetComponent<Animator>();
            playerNetworkAnimator = GetComponent<NetworkAnimator>();

            // Show no models.
            CmdShowModel(gameObject, "Pickaxe");
        }

        // Obstacle container
        obstacleContainer = GameObject.Find("ObstacleContainer");

        IsInEnergyMode = true;
	}

    void OnServerStartMatch()
    {
        if (!isServer) return;

        ShouldAllowMovementEnergyMode = true;
        hud.ShowScreen("regular");

        foreach(Player p in FindObjectsOfType<Player>())
        {
            if(p != this)
            {
                p.RpcOnServerStartMatch();
            }
        }
    }

    [ClientRpc]
    public void RpcOnServerStartMatch()
    {
        if (!isLocalPlayer) return;

        ShouldAllowMovementEnergyMode = true;
        hud.ShowScreen("regular");
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        if(IsInEnergyMode)
        {
            if(ShouldAllowMovementEnergyMode)
            {
                //TODO move somewhere else 
                float horizontalSpeed = Input.GetAxis("Horizontal") * energyMovingSpeed;
                float depthSpeed = Input.GetAxis("Vertical") * energyMovingSpeed;

                Vector3 cameraForward = Vector3.Scale(gameCamera.transform.forward, new Vector3(1, 0, 1)).normalized;
                Vector3 moveVector = (gameCamera.transform.right * horizontalSpeed) + (cameraForward * depthSpeed);

                playerRigidBody.velocity = new Vector3(moveVector.x, energyFallingSpeed, moveVector.z);
            }
            else
            {
                playerRigidBody.velocity = Vector3.zero;
            }

        }
    }


    // Update is called once per frame
    void Update () {
        if (!isLocalPlayer) return;

        if(IsInEnergyMode)
        {
            RaycastHit hitInfo;

            // 0.1f is a small offset to start the ray from inside the character
            // it is also good to note that the transform position in the sample assets is at the base of the character
            if (Physics.Raycast(transform.position + (Vector3.up * 0.1f), Vector3.down, out hitInfo, 1.0f))
            {
                if(hitInfo.transform.GetComponent<Player>() == null)
                {
                    CmdDeactivateEnergyBall(gameObject);
                }
            }
        }
        else
        {
            hud.Players = FindObjectsOfType<Player>().Length;

        }

        // Update timers.
        resourceCollectionCooldownTimer -= Time.deltaTime;
        stepTimer -= Time.deltaTime;

        if (Input.GetKeyDown(changeFocalSideKey)) {
            isFocalPointOnLeft = !isFocalPointOnLeft;
        }

        float targetX = focalDistance * (isFocalPointOnLeft ? -1 : 1);
        float smoothX = Mathf.Lerp(focalPoint.transform.localPosition.x, targetX, focalSmoothness * Time.deltaTime);
        focalPoint.transform.localPosition = new Vector3(smoothX, focalPoint.transform.localPosition.y, focalPoint.transform.localPosition.z);

        // Interaction logic.
#if UNITY_EDITOR
        // Draw interaction line.
        Debug.DrawLine(gameCamera.transform.position, gameCamera.transform.position + gameCamera.transform.forward * interactionDistance, Color.green);
#endif
        if (Input.GetKeyDown(interactionKey))
        {
            RaycastHit hit;
            if (Physics.Raycast(gameCamera.transform.position, gameCamera.transform.forward, out hit, interactionDistance))
            {
                if (hit.transform.GetComponent<Door>()) {
                    hit.transform.GetComponent<Door>().Interact();
                }
            }
        }

        // Select weapons.
        if (Input.GetKeyDown("1")) {
            SwitchWeapon(0);
        } else if (Input.GetKeyDown("2")) {
            SwitchWeapon(1);
        } else if (Input.GetKeyDown("3")) {
            SwitchWeapon(2);
        } else if (Input.GetKeyDown("4")) {
            SwitchWeapon(3);
        } else if (Input.GetKeyDown("5")) {
            SwitchWeapon(4);
        }

        // Tool switch logic.
        if (Input.GetKeyDown(toolSwitchKey)) {
            SwitchTool();
        }

        // Preserving the obstacles' horizontal rotation.
        if (currentObstacle != null) {
            currentObstacle.transform.eulerAngles = new Vector3(
                0,
                currentObstacle.transform.eulerAngles.y,
                currentObstacle.transform.eulerAngles.z
            );
        }

        // Tool usage logic (continuous).
        if (Input.GetAxis("Fire1") > 0.1f) {
            UseToolContinuous();
        }

        // Tool usage logic (trigger).
        if (Input.GetAxis("Fire1") > 0.1f) {
            if (!obstaclePlacementLock)
            {
                obstaclePlacementLock = true;
                UseToolTrigger();
            }
        } else {
            obstaclePlacementLock = false;
        }

        UpdateWeapon();
	}

    private void AnimateWeaponHold (string weaponName) {
        playerAnimator.SetTrigger("Hold" + weaponName);
        playerNetworkAnimator.SetTrigger("Hold" + weaponName);
    }

    private void AnimateShoot() {
        playerAnimator.SetTrigger("Shoot");
        playerNetworkAnimator.SetTrigger("Shoot");
    }

    private void AnimateUnequip() {
        playerAnimator.SetTrigger("HoldNothing");
        playerNetworkAnimator.SetTrigger("HoldNothing");
    }

    private void AnimateMelee() {
        playerAnimator.SetTrigger("MeleeSwing");
        playerNetworkAnimator.SetTrigger("MeleeSwing");
    }

    private void SwitchWeapon (int index) {
        if (index < weapons.Count)
        {
            soundInterface.Play();

            weapon = weapons[index];
            hud.UpdateWeapon(weapon);

            // Show animations
            if (weapon is Pistol) AnimateWeaponHold("Pistol");
            else if (weapon is RocketLauncher) AnimateWeaponHold("Rocket");
            else if (weapon is MachineGun || weapon is Sniper || weapon is Shotgun) AnimateWeaponHold("Rifle");

            // Show models
            if (weapon is Pistol) CmdShowModel(gameObject, "Pistol");
            else if (weapon is Shotgun) CmdShowModel(gameObject, "Shotgun");
            else if (weapon is MachineGun) CmdShowModel(gameObject, "MachineGun");
            else if (weapon is Sniper) CmdShowModel(gameObject, "Sniper");
            else if (weapon is RocketLauncher) CmdShowModel(gameObject, "RocketLauncher");

            tool = PlayerTool.None;
            hud.Tool = tool;

            if (currentObstacle != null) Destroy(currentObstacle);

            // Zoom out.
            if (!(weapon is Sniper)) {
                gameCamera.ZoomOut();
                hud.SniperAimVisibility = false;
            }
        }
    }

    private void SwitchTool () {
        soundInterface.Play();

        AnimateUnequip();

        weapon = null;
        hud.UpdateWeapon(weapon);

        // Zoom the camera out.
        gameCamera.ZoomOut();
        hud.SniperAimVisibility = false;

        // Cycle between the avaiable tools.
        int currentToolIndex = (int)tool;
        currentToolIndex++;

        if (currentToolIndex == System.Enum.GetNames(typeof(PlayerTool)).Length)
        {
            currentToolIndex = 0;
        }

        // Get the new tool.
        tool = (PlayerTool)currentToolIndex;
        hud.Tool = tool;

        if (tool == PlayerTool.Pickaxe) {
            CmdShowModel(gameObject, "Pickaxe");
        } else {
            CmdShowModel(gameObject, "");
        }

        // Check for obstacle placement logic.
        obstacleToAddIndex = -1;
        if (tool == PlayerTool.ObstacleVertical) {
            obstacleToAddIndex = 0;
        } else if (tool == PlayerTool.ObstacleRamp) {
            obstacleToAddIndex = 1;
        } else if (tool == PlayerTool.ObstacleHorizontal) {
            obstacleToAddIndex = 2;
        }

        if (currentObstacle != null) Destroy(currentObstacle);
        if (obstacleToAddIndex >= 0)
        {
            currentObstacle = Instantiate(obstaclePrefabs[obstacleToAddIndex]);
            currentObstacle.transform.SetParent(obstaclePlacementContainer.transform);

            currentObstacle.transform.localPosition = Vector3.zero;
            currentObstacle.transform.localRotation = Quaternion.identity;

            currentObstacle.GetComponent<Obstacle>().SetPositioningMode();

            hud.UpdateResourcesRequirement(currentObstacle.GetComponent<Obstacle>().Cost, resources);
        }
    }

    private void UseToolContinuous () {
        if (tool == PlayerTool.Pickaxe)
        {
            RaycastHit hit;
            if (Physics.Raycast(gameCamera.transform.position, gameCamera.transform.forward, out hit, interactionDistance))
            {
                if (resourceCollectionCooldownTimer <= 0 && hit.transform.GetComponent<ResourceObject>() != null)
                {
                    CmdHit(gameObject);

                    AnimateMelee();

                    resourceCollectionCooldownTimer = resourceCollectionCooldown;

                    ResourceObject resourceObject = hit.transform.GetComponent<ResourceObject>();

                    int collectedResources = 0;
                    float resourceHealth = resourceObject.HealthValue;

                    if (resourceHealth - 1 < 0.01f) {
                        collectedResources = resourceObject.ResourceAmount;
                    }

                    CmdDamage(hit.transform.gameObject, 1);

                    resources += collectedResources;
                    hud.Resources = resources;
                }
            }
        }
    }

    private void UseToolTrigger () {
        if (currentObstacle != null && resources >= currentObstacle.GetComponent<Obstacle>().Cost)
        {
            CmdHit(gameObject);

            int cost = currentObstacle.GetComponent<Obstacle>().Cost;
            resources -= cost;

            hud.Resources = resources;
            hud.UpdateResourcesRequirement(cost, resources);

            CmdPlaceObstacle(obstacleToAddIndex, currentObstacle.transform.position, currentObstacle.transform.rotation);
        }
    }

    [Command]
    void CmdPlaceObstacle (int index, Vector3 position, Quaternion rotation) {
        GameObject newObstacle = Instantiate(obstaclePrefabs[index]);
        newObstacle.transform.SetParent(obstacleContainer.transform);
        newObstacle.transform.position = position;
        newObstacle.transform.rotation = rotation;
        newObstacle.GetComponent<Obstacle>().Place();

        NetworkServer.Spawn(newObstacle);
    }

	private void OnTriggerEnter (Collider otherCollider)
	{
        if (!isLocalPlayer) return;

        if (otherCollider.gameObject.GetComponent<ItemBox>() != null) {
            ItemBox itemBox = otherCollider.gameObject.GetComponent<ItemBox>();

            GiveItem(itemBox.Type, itemBox.Amount);

            CmdCollectBox(otherCollider.gameObject);
        }
	}

    [Command]
    void CmdCollectBox (GameObject box) {
        Destroy(box);
    }

    private void GiveItem (ItemBox.ItemType type, int amount) {
        CmdHit(gameObject);

        // Create a weapon reference.
        Weapon currentWeapon = null;

        // Check if we already have an instance of this weapon.
        for (int i = 0; i < weapons.Count; i++) {
            if (type == ItemBox.ItemType.Pistol && weapons[i] is Pistol) currentWeapon = weapons[i];
            else if (type == ItemBox.ItemType.MachineGun && weapons[i] is MachineGun) currentWeapon = weapons[i];
            else if (type == ItemBox.ItemType.Shotgun && weapons[i] is Shotgun) currentWeapon = weapons[i];
            else if (type == ItemBox.ItemType.Sniper && weapons[i] is Sniper) currentWeapon = weapons[i];
            else if (type == ItemBox.ItemType.RocketLauncher && weapons[i] is RocketLauncher) currentWeapon = weapons[i];
        }

        // If we don't have a weapon of this type, create one, and add it to the weapons list.
        if (currentWeapon == null)
        {
            if (type == ItemBox.ItemType.Pistol) currentWeapon = new Pistol();
            else if (type == ItemBox.ItemType.MachineGun) currentWeapon = new MachineGun();
            else if (type == ItemBox.ItemType.Shotgun) currentWeapon = new Shotgun();
            else if (type == ItemBox.ItemType.Sniper) currentWeapon = new Sniper();
            else if (type == ItemBox.ItemType.RocketLauncher) currentWeapon = new RocketLauncher();
            weapons.Add(currentWeapon);
        }

        currentWeapon.AddAmmunition(amount);
        currentWeapon.LoadClip();

        if (currentWeapon == weapon) {
            hud.UpdateWeapon(weapon);
        }
    }

    private void UpdateWeapon () {
        if (weapon != null) {
            if (Input.GetKeyDown(KeyCode.R)) {
                weapon.Reload();
            }

            float timeElapsed = Time.deltaTime;
            bool isPressingTrigger = Input.GetAxis("Fire1") > 0.1f;

            bool hasShot = weapon.Update(timeElapsed, isPressingTrigger);
            hud.UpdateWeapon(weapon);
            if (hasShot) {
                Shoot();
            }

            // Zoom logic.
            if (weapon is Sniper) {
                if (Input.GetMouseButtonDown(1)) {
                    gameCamera.TriggerZoom();
                    hud.SniperAimVisibility = gameCamera.IsZoomedIn;
                }
            }
        }
    }

    private void Shoot () {
        int amountOfBullets = 1;
        if (weapon is Shotgun) {
            amountOfBullets = ((Shotgun)weapon).AmountOfBullets;
        }

        if (weapon is Pistol) CmdPlayWeaponSound(gameObject, 0);
        else if (weapon is MachineGun) CmdPlayWeaponSound(gameObject, 1);
        else if (weapon is Shotgun) CmdPlayWeaponSound(gameObject, 2);
        else if (weapon is Sniper) CmdPlayWeaponSound(gameObject, 3);

        AnimateShoot();

        for (int i = 0; i < amountOfBullets; i++)
        {
            float distanceFromCamera = Vector3.Distance(gameCamera.transform.position, transform.position);
            RaycastHit targetHit;
            if (Physics.Raycast(gameCamera.transform.position + (gameCamera.transform.forward * distanceFromCamera), gameCamera.transform.forward, out targetHit))
            {
                Vector3 hitPosition = targetHit.point;

                Vector3 shootDirection = (hitPosition - shootOrigin.transform.position).normalized;
                shootDirection = new Vector3(
                    shootDirection.x + Random.Range(-weapon.AimVariation, weapon.AimVariation),
                    shootDirection.y + Random.Range(-weapon.AimVariation, weapon.AimVariation),
                    shootDirection.z + Random.Range(-weapon.AimVariation, weapon.AimVariation)
                );
                shootDirection.Normalize();

                if (!(weapon is RocketLauncher))
                {
                    RaycastHit shootHit;
                    if (Physics.Raycast(shootOrigin.transform.position, shootDirection, out shootHit))
                    {
                        GameObject debugPositionInstance = Instantiate(debugPositionPrefab);
                        debugPositionInstance.transform.position = shootHit.point;
                        Destroy(debugPositionInstance, 0.5f);

                        if (shootHit.transform.GetComponent<IDamageable>() != null)
                        {
                            CmdDamage(shootHit.transform.gameObject, weapon.Damage);
                        }
                        else if (shootHit.transform.GetComponentInParent<IDamageable>() != null)
                        {
                            CmdDamage(shootHit.transform.parent.gameObject, weapon.Damage);
                        }

#if UNITY_EDITOR
                        // Draw a line to show the shooting ray.
                        Debug.DrawLine(shootOrigin.transform.position, shootOrigin.transform.position + shootDirection * 100, Color.red);
#endif
                    }
                }
                else
                {
                    CmdSpawnRocket(shootDirection);
                }
            }
        }
    }

    [Command]
    private void CmdSpawnRocket (Vector3 shootDirection) {
        GameObject rocket = Instantiate(rocketPrefab);
        rocket.transform.position = shootOrigin.transform.position + shootDirection;
        rocket.GetComponent<Rocket>().Shoot(shootDirection);

        NetworkServer.Spawn(rocket);
    }

    [Command]
    private void CmdDamage (GameObject target, float damage) {
        if (target != null) target.GetComponent<IDamageable>().Damage(damage);
    }

    public int Damage(float amount)
    {
        GetComponent<Health>().Damage(amount);
        return 0;
    }

    private void OnHealthChanged (float newHealth) {
        if (!isLocalPlayer) return;

        hud.Health = newHealth;

        if (newHealth < 0.01f) {
            Cursor.lockState = CursorLockMode.None;
            hud.ShowScreen("gameOver");
            CmdDestroy();
        }
    }

    [Command]
    void CmdDestroy () {
        Destroy(gameObject);
    }

    // Network weapon sound.
    [Command]
    void CmdPlayWeaponSound(GameObject caller, int index)
    {
        if (!isServer) return;

        RpcPlayWeaponSound(caller, index);
    }

    [ClientRpc]
    void RpcPlayWeaponSound(GameObject caller, int index)
    {
        caller.GetComponent<Player>().PlayWeaponSound(index);
    }

    public void PlayWeaponSound(int index)
    {
        soundsWeapons[index].Play();
    }

    // Network footstep sound.
    [Command]
    void CmdPlayFootstepSound (GameObject caller) {
        if (!isServer) return;

        RpcPlayFootstepSound(caller);
    }

    [ClientRpc]
    void RpcPlayFootstepSound (GameObject caller) {
        caller.GetComponent<Player>().PlayFootstepSound();
    }

    void PlayFootstepSound () {
        soundsFootsteps[Random.Range(0, soundsFootsteps.Length)].Play();
    }

    // This event is emitted in the third person character.
    void OnFootstep (float forwardAmount) {
        if (forwardAmount > 0.6f && stepTimer <= 0) {
            stepTimer = stepInterval;
            CmdPlayFootstepSound(gameObject);
        }
    }

    // Network jump sound.
    void OnJump () {
        CmdJump(gameObject);
    }

    [Command]
    void CmdJump(GameObject caller) {
        if (!isServer) return;

        RpcJump(caller);
    }

    [ClientRpc]
    void RpcJump(GameObject caller) {
        caller.GetComponent<Player>().PlayJumpSound();
    }

    public void PlayJumpSound() {
        soundJump.Play();
    }

    // Network hit sound.
    [Command]
    void CmdHit (GameObject caller) {
        if (!isServer) return;

        RpcHit(caller);
    }

    [ClientRpc]
    void RpcHit (GameObject caller) {
        caller.GetComponent<Player>().PlayHitSound();
    }

    public void PlayHitSound () {
        soundHit.Play();
    }

    public override void OnStartLocalPlayer()
	{
        base.OnStartLocalPlayer();

        CmdRefreshModels();
	}

    //Network models

    [Command]
    void CmdRefreshModels () {
        if (!isServer) return;

        foreach (Player player in FindObjectsOfType<Player>()) {
            player.RpcRefreshModel();
        }
    }

    [ClientRpc]
    public void RpcRefreshModel () {
        CmdShowModel(gameObject, modelName);
    }

    [Command]
    void CmdShowModel (GameObject caller, string newModel) {
        if (!isServer) return;

        RpcShowModel(caller, newModel);
    }

    [ClientRpc]
    void RpcShowModel (GameObject caller, string newModel) {
        caller.GetComponent<Player>().ShowModel(newModel);
    }

    public void ShowModel(string newModel)
    {
        modelName = newModel;

        modelAxe.SetActive(modelName == "Pickaxe");
        modelPistol.SetActive(modelName == "Pistol");
        modelShotgun.SetActive(modelName == "Shotgun");
        modelMachineGun.SetActive(modelName == "MachineGun");
        modelSniper.SetActive(modelName == "Sniper");
        modelRocketLauncher.SetActive(modelName == "RocketLauncher");
    }

    //Network energy balls
    [Command]
    void CmdDeactivateEnergyBall(GameObject caller)
    {
        if (!isServer) return;

        RpcDeactivateEnergyBall(caller);
    }

    [ClientRpc]
    public void RpcDeactivateEnergyBall(GameObject caller)
    {
        caller.GetComponent<Player>().IsInEnergyMode = false;
    }
}
