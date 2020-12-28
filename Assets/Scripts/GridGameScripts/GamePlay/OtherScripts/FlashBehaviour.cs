using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashBehaviour : MonoBehaviour {
    [SerializeField]
    private GameObject normalObject;
    [SerializeField]
    private GameObject flashObject;
    [SerializeField]
    private int numberOfFlashes;
    [SerializeField]
    private float flashSpeed;
    [SerializeField]
    private List<MeshRenderer> flashRenderers;
    [SerializeField]
    private List<MeshRenderer> normalRenderers;
    public bool isInfinite;
    // Use this for initialization
    void Start () {
    }
    private IEnumerator FlashMeshrenderer()
    {
        if (flashRenderers != null && normalRenderers != null)
        {
            for (var i = 0; i < numberOfFlashes; i++)
            {
                foreach(MeshRenderer renderer in normalRenderers)
                {
                    renderer.enabled = false;
                }

                foreach (MeshRenderer renderer in flashRenderers)
                {
                    renderer.enabled = true;
                }
                yield return new WaitForSeconds(flashSpeed);
                foreach (MeshRenderer renderer in normalRenderers)
                {
                    renderer.enabled = true;
                }

                foreach (MeshRenderer renderer in flashRenderers)
                {
                    renderer.enabled = false;
                }
                yield return new WaitForSeconds(flashSpeed);
                if(isInfinite)
                {
                    i--;
                }
            }
            foreach (MeshRenderer renderer in normalRenderers)
            {
                renderer.enabled = true;
            }
            foreach (MeshRenderer renderer in flashRenderers)
            {
                renderer.enabled = false;
            }
        }
        else
        {
            for (var i = 0; i < numberOfFlashes; i++)
            {
                foreach (MeshRenderer renderer in normalRenderers)
                {
                    renderer.enabled = false;
                }

                yield return new WaitForSeconds(flashSpeed);
                foreach (MeshRenderer renderer in normalRenderers)
                {
                    renderer.enabled = true;
                }
;
                yield return new WaitForSeconds(flashSpeed);
                if (isInfinite)
                {
                    i--;
                }
            }
            foreach (MeshRenderer renderer in normalRenderers)
            {
                renderer.enabled = true;
            }
        }
    }
    private IEnumerator FlashGameObject()
    {
        if (flashObject != null && normalObject != null)
        {
            for (var i = 0; i < numberOfFlashes; i++)
            {
                normalObject.SetActive(false);
                flashObject.SetActive(true);
                yield return new WaitForSeconds(flashSpeed);
                normalObject.SetActive(true);
                flashObject.SetActive(false);
                yield return new WaitForSeconds(flashSpeed);
                if (isInfinite)
                {
                    i--;
                }
            }
            normalObject.SetActive(true);
            flashObject.SetActive(false);
        }
        else
        {
            for (var i = 0; i < numberOfFlashes; i++)
            {
                normalObject.SetActive(false);
                yield return new WaitForSeconds(flashSpeed);
                normalObject.SetActive(true);
                yield return new WaitForSeconds(flashSpeed);
                if (isInfinite)
                {
                    i--;
                }
            }
            normalObject.SetActive(true);
            flashObject.SetActive(false);
        }
    }
    public void StartFlashing()
    {
        if(normalRenderers.Count <=0)
        {
            StartCoroutine(FlashGameObject());
            return;
        }
        StartCoroutine(FlashMeshrenderer());
    }
    public void StopFlashing()
    {
        if(normalObject != null)
        {
            normalObject.SetActive(true);
            if (flashObject != null)
            {
                flashObject.SetActive(false);
            }
        }
        if(normalRenderers.Count >0)
        {
            foreach (MeshRenderer renderer in normalRenderers)
            {
                renderer.enabled = false;
            }
            if(flashRenderers.Count > 0)
            {
                foreach (MeshRenderer renderer in flashRenderers)
                {
                    renderer.enabled = false;
                }
            }
        }
        StopAllCoroutines();
    }
    private void OnDisable()
    {
        StopAllCoroutines();
    }
    // Update is called once per frame
    void Update () {
		
	}
}
