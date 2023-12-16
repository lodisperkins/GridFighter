using Lodis.ScriptableObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ReplaceMesh
{
    public SkinnedMeshRenderer MeshRenderer;
    public BodyPartSlot Part;
}

public class MeshReplacementBehaviour : MonoBehaviour
{
    [SerializeField]
    private ReplaceMesh[] _replacementSlots = new ReplaceMesh[16];
    [SerializeField]
    private List<ArmorData> _armorReplacements = new List<ArmorData>();
    [SerializeField]
    private SkinnedMeshRenderer _bodyRenderer;
    [SerializeField]
    private SkinnedMeshRenderer _hairRenderer;

    public List<ArmorData> ArmorReplacements { get => _armorReplacements; private set => _armorReplacements = value; }
    public Color FaceColor 
    {
        get => _bodyRenderer.material.GetColor("_Color");
        set => _bodyRenderer.material.SetColor("_Color", value);
    }
    public Color HairColor
    {
        get => _hairRenderer.material.GetColor("_Color");
        set => _hairRenderer.material.SetColor("_Color", value);
    }

    public void ReplaceMeshes()
    {
        foreach (ArmorData data in ArmorReplacements)
        {
            ReplaceMesh(data);
        }
    }


    public void ReplaceMeshes(List<ArmorData> armorData)
    {
        foreach (ArmorData data in armorData)
        {
            ReplaceMesh(data);
        }
    }

    public void RemoveMeshes(List<ArmorData> armorData)
    {
        foreach (ArmorData data in armorData)
        {
            RemoveMesh(data);
        }
    }

    public void RemoveMeshes()
    {
        if (ArmorReplacements == null)
            return;

        foreach (ArmorData data in ArmorReplacements)
        {
            RemoveMesh(data);
        }
    }

    public void ReplaceMesh(ArmorData data)
    {
        foreach (ArmorPiece piece in data.ArmorPieces)
        {
            ReplaceMesh mesh = Array.Find(_replacementSlots, replaceMesh => replaceMesh.Part == piece.BodyPart);
            mesh.MeshRenderer.sharedMesh = piece.ArmorMesh;
            mesh.MeshRenderer.enabled = true;
            mesh.MeshRenderer.material = data.ArmorMaterial;
        }

        int index = ArmorReplacements.FindIndex(set => set.BodySection == data.BodySection);

        if (index == -1)
            ArmorReplacements.Add(data);
        else
            ArmorReplacements[index] = data;
    }

    public void RemoveMesh(ArmorData data)
    {
        foreach (ArmorPiece piece in data.ArmorPieces)
        {
            ReplaceMesh mesh = Array.Find(_replacementSlots, replaceMesh => replaceMesh.Part == piece.BodyPart);
            mesh.MeshRenderer.sharedMesh = null;
            mesh.MeshRenderer.enabled = false;
        }
    }
}
