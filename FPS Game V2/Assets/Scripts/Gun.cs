using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Gun : MonoBehaviour
{
    //NEED TO MAKE A RELOAD TIMER FOR EACH GUN
    //Add recoil to camera 
    #region variables
    //Scripts
    [SerializeField] public PlayerController player;

    public Text ammo;

    public Animator animator;

    private int shotsFired;
    public int magazineSize;
    public int maxAmmo;//ammo on the right
    private int totalMaxAmmo;// currentAmmo + maxAmmo
    private int currentAmmo;//ammo on the left

    #region floats
    public float reloadTime;
    //public float swapTime;
    public float holsterTime;
    public float drawTime;
    public float damage = 10f;
    public float range = 100f;
    public float fireRate = 15f;
    private float nextTimeToFire = 0f;
    #endregion

    private bool isReloading = false;
    private bool isSwaping = false;
    [SerializeField] public bool rapidFire = false;
    [SerializeField] public bool scope = false;
    private bool hasHolstered = false;

    private Camera cam;
    private WaitForSeconds rapidFireWait;
    private WaitForSeconds weaponSwapWait;
    private WaitForSeconds holsterWait;

    public GameObject[] muzzelFlash;//array of muzzle flashes
    public GameObject muzzelFlashPool;//array of muzzle flashes
    
    public Transform muzzelSpawn;//where the muzzle flash will appear
    private GameObject holdFlash;
    public GameObject bulletHole;

    private Transform pos;
    private Recoil recoil_script;

    public AudioSource shoot_sound_source, reloadSound_source;//sounds
    //public static AudioSource hitMarker;
    #endregion
    private void Awake()
    {
        rapidFireWait = new WaitForSeconds(1 / fireRate);
        holsterWait = new WaitForSeconds(0.1f);
    }
    // Start is called before the first frame update
    void Start()
    {
        currentAmmo = magazineSize;
        totalMaxAmmo = maxAmmo;
        recoil_script = gameObject.GetComponent<Recoil>();
        shoot_sound_source = transform.Find("AudioSourceShoot").GetComponent<AudioSource>();
        reloadSound_source = transform.Find("AudioSourceReload").GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        isReloading = false;
        animator.SetBool("Reloading", false);
    }

    // Update is called once per frame
    void Update()
    {
        transform.localRotation = pos.localRotation;
        //Debug.Log(maxAmmo);
    }

    public void Shoot(bool check)
    {
        if (isReloading || isSwaping)
            return;
        
        if (currentAmmo <= 0)
        {
            if (maxAmmo <= 0)
            {
                return;
            }
            StartCoroutine(Reload());
            return;
        }
        if(check && Time.time>=nextTimeToFire)
        {
           nextTimeToFire = Time.time + 1f / fireRate;
           Firing(); 
        }
    }

    public void Firing()
    {
        recoil_script.RecoilFire();//muzzle flash positon does not sync up with the recoil
        currentAmmo--;
        shotsFired++;
        int random = Random.Range(0, 5);
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        holdFlash = muzzelFlashPool.GetComponent<NewObjectPool>().releaseRandom(); //Object Pool atempt
        holdFlash.transform.position = muzzelSpawn.transform.position;
        holdFlash.transform.rotation = muzzelSpawn.transform.rotation * Quaternion.Euler(0, 0, 90);
        holdFlash.SetActive(true);
        if (shoot_sound_source)
            shoot_sound_source.Play();
        if (Physics.Raycast(cam.transform.position,cam.transform.forward,out hit,range,7))
        {
            //Debug.Log(hit.transform.name);
            
            //Target target = hit.transform.GetComponent<Target>();
            PlayerController player = hit.transform.GetComponent<PlayerController>();
            if(player != null)
            {
                player.takeDamage(damage);
                
            }
            SharedPool.SharedInstance.setRaycastHit(hit);
            SharedPool.SharedInstance.createBulletHole();
        }
    }

    public IEnumerator RapidFire()
    {
        if(rapidFire)
        {
            while (true)
            {
                Shoot(rapidFire);
                yield return rapidFireWait;
            }
        }
        else
        {
            Shoot(rapidFire);
            yield return null;
        }
    }

    IEnumerator Reload()
    {    
        isReloading = true;
        //Debug.Log("Reloading...");
        animator.SetBool("Reloading", true);
        if (reloadSound_source)
            reloadSound_source.Play();
        yield return new WaitForSeconds(reloadTime);
        
        //Debug.Log(maxAmmo);
        if (shotsFired > maxAmmo)
        {
            shotsFired = maxAmmo;
        }
        maxAmmo -= shotsFired;
        currentAmmo += shotsFired;


        shotsFired = 0;
        animator.SetBool("Reloading", false);
        //Debug.Log("Done");

        isReloading = false;
    }

    public IEnumerator Holster(GameObject secondGun)
    {
        isSwaping = true;
        hasHolstered = true;
        animator.SetBool("Holster", true);
        //Debug.Log("Swapping weapons");
        //Debug.Log(isSwaping);

        yield return new WaitForSeconds(holsterTime);
        animator.SetBool("Holster", false);
        gameObject.SetActive(false);
        hasHolstered = false;
    }

    public IEnumerator Draw(GameObject secondGun)
    {
        while(hasHolstered)
        {
            //Debug.Log("waiting");
            yield return holsterWait;
        }
        secondGun.SetActive(true);
        animator.SetBool("Draw", true);
        yield return new WaitForSeconds(secondGun.GetComponent<Gun>().drawTime);
        
        animator.SetBool("Draw", false);
        
        //Debug.Log("Swap completed");
        isSwaping = false;
    }

    /*public IEnumerator Swapping(GameObject secondGun)
    {
        isSwaping = true;
        Debug.Log("Swapping weapons");
        //Debug.Log(isSwaping);
        
        yield return new WaitForSeconds(swapTime/2);
        gameObject.SetActive(false);
        //Debug.Log("Swap completed");
        secondGun.SetActive(true);
        player.ammo.text = secondGun.GetComponent<Gun>().updateAmmoText();//Check this
        isSwaping = false;
    }*/

    public void Load()
    {
        if (maxAmmo < 0)
        {
            return;
        }
        StartCoroutine(Reload());
    }

    public void setPosition(Transform position)
    {
        pos = position;
    }

    public void checkIfSwaping(bool check)
    {
        isSwaping = check;
    }

    public void getCamera(Camera camera)
    {
        cam = camera;
    }
    
    public int getShotsFired()
    {
        return shotsFired;
    }

    public int getCurrentAmmo()
    {
        return currentAmmo;
    }

    public int getTotalMaxAmmo()
    {
        return totalMaxAmmo;
    }

    public bool holsterCheck()
    {
        return hasHolstered;
    }

    public string updateAmmoText()
    {
        return currentAmmo.ToString() + " / " + maxAmmo.ToString();
    }

}