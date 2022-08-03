using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldController : MonoBehaviour
{
    #region Variables
    [Header("Shield Object")]
    [Tooltip("GameObject with shield's material to interact with. Will select current gameobject if null")]
    public GameObject shieldObject;

    Material shieldMaterial; // material this script interacts with, selected from shieldObject

    [Header("Hit settings")]
    [Tooltip("Animation curve of glow on impact")]
    public AnimationCurve hitCurve;
    [Tooltip("Duration of glow on hit")]
    public float hitDuration = 0.6f;

    [Tooltip("Prefab in Resources folder to instantiate on hit")]
    public string impactPrefabName = "VFX_ShieldImpact";

    [Tooltip("Can collisions with other rigidbodies impact this shield?")]
    public bool rigidBodyImpacts;

    float currentHP;
    [Tooltip("Max Hitpoints of the shield")]
    public float maxHP = 10;

    [Header("Dissolve values")]
    public float minDissolve = -1;
    public float maxDissolve = 1;

    [Header("Glow values")]
    public float minGlow = 1;
    public float maxGlow = 100;


    [Header("Shield durations")]
    [Tooltip("Time to load shield")]
    public float loadDuration = 1.5f;

    [Tooltip("Time to break shield")]
    public float breakDuration = 0.1f;

    [Tooltip("Time to recharge shield")]
    public float rechargeDuration = 2;

    [Tooltip("Time to wait after a hit before start recharging shield")]
    public float timeBeforeRecharge = 3;

    [HideInInspector]
    public bool shieldEnabled; // Is shield On ?

    float lastHitTime; // Time.time of last hit received
    bool rechargeRunning; // Is WaitForShieldRecharge routine running ?

    // Hit index for coroutines and hitVectors
    int currentHitIndex = 0;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        // If no shieldObject is selected, choose this gameObject instead
        if (shieldObject == null)
        {
            shieldObject = gameObject;
        }

        // Get material from MeshRenderer or SkinnedMeshRenderer to apply shader changes to
        if (shieldObject.GetComponent<MeshRenderer>() != null)
        {
            shieldMaterial = shieldObject.GetComponent<MeshRenderer>().material;
        }
        else if (shieldObject.GetComponent<SkinnedMeshRenderer>() != null)
        {
            shieldMaterial = shieldObject.GetComponent<SkinnedMeshRenderer>().material;
        }

        // Reset shader values to starting positions
        shieldMaterial.SetFloat("_Recharge", minDissolve);
        shieldMaterial.SetFloat("_Dissolve", minDissolve);
        shieldMaterial.SetFloat("_Glow", minGlow);

        // Load shield animation and set base stats
        StartCoroutine(LoadShield());
    }

    /// <summary>
    /// Call this on hit. Instantiates impact VFX and starts shader animations on hit point.
    /// </summary>
    /// <param name="hitPosition"></param>
    /// <param name="hitDirection"></param>
    /// <param name="hitSize"></param>
    public void GetHit(Vector3 hitPosition, Vector3 hitDirection, float hitSize, float damage)
    {
        if (!shieldEnabled) return; // Dont run if shield is already down

        // Set hit time
        lastHitTime = Time.time;

        // Deal damage to currentHP
        currentHP -= damage;
      
        // Instantiate VFX on hit
        if (Resources.Load(impactPrefabName, typeof(GameObject))!= null)
        {
          GameObject impactVFX = Instantiate(Resources.Load(impactPrefabName, typeof(GameObject)),  hitPosition, Quaternion.LookRotation(hitDirection)) as GameObject; ;
            impactVFX.transform.localScale = Vector3.one * hitSize;
            Destroy(impactVFX, 0.6f);
        }
       
        // Start hit glow animation
        StartCoroutine(HitAnimation(hitPosition, hitSize, currentHitIndex));

        // Set next hit index (and reset to 0 when max value reached)
        if (currentHitIndex == 9)
        {
            currentHitIndex = 0;
        }
        else
        {
            currentHitIndex++;
        }

        // Break shield if no hp remaining
        if (currentHP <= 0)
        {
            StartCoroutine(BreakShield());
        }
        // Otherwise, wait for shield recharge and set glow
        else
        {
            if (!rechargeRunning)
            {
                StartCoroutine(WaitForShieldRecharge());
            }

            // Set Glow according to HP ratio 
            shieldMaterial.SetFloat("_Glow", Mathf.Lerp(minGlow, maxGlow/1.5f, Mathf.Pow(1-(currentHP / maxHP), 3)));
        }
    }

    /// <summary>
    /// Animation of impact on shield's shader 
    /// </summary>
    /// <param name="position"></param>
    /// <param name="hitSize"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    IEnumerator HitAnimation(Vector3 position, float hitSize, int index)
    {
        float t = 0;
        Vector4 hitAnimationVector;

        // Set correct name from index
        string hitMaterialName = "_Hit" + index.ToString();
      
        while (t < 1)
        {
            t += Time.deltaTime / hitDuration;
            t = Mathf.Clamp(t, 0, 1);

            // Get correct intensity from animation curve
            hitAnimationVector = new Vector4(position.x, position.y, position.z, hitSize * hitCurve.Evaluate(t));

            // Apply Vector to shader depending on index
            shieldMaterial.SetVector(hitMaterialName, hitAnimationVector);

            yield return null;
        }

        // Reset vector to 0
         shieldMaterial.SetVector(hitMaterialName, Vector4.zero);       
    }

    /// <summary>
    /// Load shield to full from empty
    /// </summary>
    /// <returns></returns>
    IEnumerator LoadShield()
    {
        float t = 0;
        shieldEnabled = true;
        currentHP = maxHP;
        
        shieldMaterial.SetFloat("_Glow", minGlow);

        // Make shield appear with Dissolve
        while (t < 1)
        {
            t += Time.deltaTime / loadDuration;
            t = Mathf.Clamp(t, 0, 1);
            
            shieldMaterial.SetFloat("_Dissolve", Mathf.Lerp(minDissolve, maxDissolve, t));


            yield return null; 
        }
        // Glow after loading shield
        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / loadDuration;
            t = Mathf.Clamp(t, 0, 1);

            shieldMaterial.SetFloat("_Glow", Mathf.Lerp(minGlow, maxGlow, t*(1-t)*4));


            yield return null;
        }

       
    }

    /// <summary>
    /// Break shield and hide it
    /// </summary>
    /// <returns></returns>
    IEnumerator BreakShield()
    {
        float t = 0;
        shieldEnabled = false;

        // waits a little to see last impact
        yield return new WaitForSeconds(hitDuration / 3);

        // Glow to full amount
        while (t < 1)
        {
            t += Time.deltaTime / breakDuration /2;
            t = Mathf.Clamp(t, 0, 1);

            shieldMaterial.SetFloat("_Glow", Mathf.Lerp(maxGlow/1.5f, maxGlow, t ));

            yield return null;
        }

        // Dissolve to minimum amount
        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / breakDuration;
            t = Mathf.Clamp(t, 0, 1);

            shieldMaterial.SetFloat("_Dissolve", Mathf.Lerp(maxDissolve, minDissolve, t));

            yield return null;
        }
    }

    /// <summary>
    /// Recharge shield to full when damaged
    /// </summary>
    /// <returns></returns>
    IEnumerator RechargeShield()
    {
        float t = 0;
        float startHP = currentHP;

        while (t < 1)
        {
            t += Time.deltaTime / rechargeDuration;
            t = Mathf.Clamp(t, 0, 1);

            shieldMaterial.SetFloat("_Recharge", Mathf.Lerp(minDissolve, maxDissolve, t));
            currentHP = Mathf.Lerp(startHP, maxHP, t);
            shieldMaterial.SetFloat("_Glow", Mathf.Lerp(minGlow, maxGlow / 1.5f, Mathf.Pow(1 - (currentHP / maxHP), 3)));

            yield return null;
        }
    }

    /// <summary>
    /// Runs and waits for shield to recharge between hits
    /// </summary>
    /// <returns></returns>
    IEnumerator WaitForShieldRecharge()
    {
        rechargeRunning = true;

        // Wait until it has been long enough since last hit
        while (RecentlyHit())
        {
            yield return null;
        }
        rechargeRunning = false;

        // Recharge shield 
        if (shieldEnabled)
        {
            yield return RechargeShield();
        }
        // Load shield if shield was broken
        else
        {
            yield return LoadShield();
        }
    }

    /// <summary>
    /// Returns true if shield has been hit since longer than timeBeforeRecharge
    /// </summary>
    /// <returns></returns>
    bool RecentlyHit()
    {
        return Time.time - lastHitTime < timeBeforeRecharge;  
    }

    /// <summary>
    /// Deals damage on collisions
    /// </summary>
    /// <param name="collision"></param>
    private void OnCollisionEnter(Collision collision)
    {
        if (!rigidBodyImpacts) return; // only run  if rigidBodyImpacts is enabled

        if (collision.relativeVelocity.magnitude > 0.05f) // Ignores small collisions
        {
            float hitStrength = collision.relativeVelocity.magnitude * collision.collider.attachedRigidbody.mass / 10; // Get impact force from collision

            GetHit(collision.GetContact(0).point, collision.GetContact(0).normal, hitStrength/5, hitStrength); // Call GetHit with collision and hitStrength settings
        }
    }
}
