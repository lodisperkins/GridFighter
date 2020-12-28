using GridGame.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GridGame.Interfaces
{
    /// <summary>
    /// These are the items that each block has that can be passsed on to another
    /// block to upgrade it. These are usually the defining feature of a block.
    /// For example, the GridGame.Interfaces.IUpgradable item for an Attack block would be its AttackBlockBehaviour script
    /// that allows it to fire.
    /// </summary>
    public interface IUpgradable
    {
        //The block this item is attached to
        BlockBehaviour block
        {
            get;set;
        }
        Color displayColor
        {
            get;set;
        }

        //The defining characteristic for this item. This is its ability Ex: Attack Block - Bullet Emitter
        GameObject specialFeature
        {
            get;
        }
        //The name of this item
        string Name
        {
            get; 
        }
        bool CanBeHeld
        {
            get;
        }
        GridPhysicsBehaviour PhysicsBehaviour
        {
            get;set;
        }
        //Upgrades whatever block this GridGame.Interfaces.IUpgradable item has touched
        void UpgradeBlock(GameObject otherBlock);
        //Transfers ownership of this item to the block that this item is upgrading
        void TransferOwner(GameObject otherBlock);
        //Does whatever this item needs to do upon colliding with any other object
        void ResolveCollision(GameObject collision);
        //Disables whatever component this item has so that it may be displayed using whatever ability it has
        void ActivateDisplayMode();
        void UpgradePlayer(PlayerAttackBehaviour player);
        void ActivatePowerUp();
        void DeactivatePowerUp();
        void DetachFromPlayer();
        void Stun();
        void Unstun();
    }
}

