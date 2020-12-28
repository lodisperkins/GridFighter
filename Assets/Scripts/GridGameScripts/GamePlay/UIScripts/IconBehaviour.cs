using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IconBehaviour : MonoBehaviour
{

	[SerializeField] private List<GameObject> iconsLit;
	[SerializeField] private List<GameObject> iconsUnlit;

	[SerializeField] private GridGame.VariableScripts.IntVariable playerMaterials;
	public void ToggleIcons(int count)
	{
		for (int i = 0; i < count; i++)
		{
			iconsLit[i].SetActive(true);
			iconsUnlit[i].SetActive(false);
		}
		for (int i = count; i < iconsLit.Count; i++)
		{
			iconsLit[i].SetActive(false);
			iconsUnlit[i].SetActive(true);
		}
	}
	public void UpdateIcons()
	{
		if (playerMaterials.Val < 10)
		{
			ToggleIcons(0);
		}
		if (playerMaterials.Val >= 10)
		{
			ToggleIcons(1);
		}
		if (playerMaterials.Val >= 20)
		{
			ToggleIcons(2);
		}
		if (playerMaterials.Val >= 30)
		{
			ToggleIcons(4);
		}
	}
	// Update is called once per frame
	void Update () {
		UpdateIcons();
	}
}
