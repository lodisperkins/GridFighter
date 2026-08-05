using FixedPoints;
using Lodis.Accessories;
using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Types;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class WF_WhipPull : ProjectileAbility
    {
        private LineFollowBehaviour _line;
        private EntityDataBehaviour _whipTip;
        private PanelBehaviour _dropPanel;

        private FTransform _originalParent;
        private FTransform _opponentTransform;
        private KnockbackBehaviour _opponentKnockback;
        private GridPhysicsBehaviour _oppPhysics;

        private bool _returning;
        private bool _opponentAttached;
        private bool _goneThroughTeleporter;
        private bool _canGrab = true;

        private Transform _heldItemSpawn;
        private AccessoryEffectBehaviour _enforcerInstance;
        private FVector3 _originalPosition;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(newOwner);

            _originalPosition = OwnerMoveset.ProjectileSpawner.Entity.FixedTransform.LocalPosition;
        }

        protected void OnCollision(Collision collision)
        {
            //Don't collide with self or if already returning.
            if (collision.OtherEntity == null || collision.OtherEntity == Owner.Data || _returning || !_canGrab)
                return;

            //Inivincibility/intangible check
            _opponentKnockback = collision.OtherEntity.GetComponent<KnockbackBehaviour>();

            if (!_opponentKnockback)
            {
                return;
            }

            if (_opponentKnockback.IsIntangible || _opponentKnockback.IsInvincible)
                return;

            //Set up opponent transform to be attached to whip
            _originalParent = collision.OtherEntity.Transform.Parent;
            _opponentTransform = collision.OtherEntity.Transform;
            //_opponentTransform.LocalRotation = FQuaternion.Identity;

            collision.OtherEntity.Transform.Parent = _whipTip.FixedTransform;
            collision.OtherEntity.Transform.LocalPosition = FVector3.Zero;

            //Stun the opponent so they can't do anything while being pulled.
            Fixed32 stunTime = abilityData.GetCustomStatValue("StunTime");
            _opponentKnockback.Stun(abilityData.recoverTime + stunTime);

            _opponentKnockback.MovementBehaviour.CancelMovement();
            _opponentKnockback.MovementBehaviour.AlwaysLookAtOpposingSide = false;
            _opponentKnockback.MovementBehaviour.Position = _dropPanel.Position;

            //Ignore teleporters on the return trip if we didn't go through one already.
            //This prevents weird behavior where the whip still teleports even after catching the opponent.
            if (!_goneThroughTeleporter)
            {
                _line.IgnoreTeleporters = true;
                _line.ShouldCancelTeleport = true;
            }

            //Make the whip return to the user.
            Sound.SoundManagerBehaviour.Instance.PlaySound(abilityData.Sounds[0]);
            FlipWhipVelocity();

            _returning = true;
            _opponentAttached = true;

            //Tell the whip to drop the opponnent if it is destroyed.
            _whipTip.Data.ClearEndEvent();
            _whipTip.Data.OnEnd += ResetOpponent;
        }

        protected void FlipWhipVelocity()
        {
            GridPhysicsBehaviour physics = _whipTip.GetComponent<GridPhysicsBehaviour>();

            physics.Velocity = -physics.Velocity * 2;

            if (_goneThroughTeleporter)
            {
                _line.IgnoreTeleporters = false;
            }
        }

        protected void ResetOpponent()
        {
            if (_whipTip == null)
                return;

            _whipTip.FixedTransform.RemoveChild(_opponentTransform);
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            //Reset flags.
            _returning = false;
            _opponentAttached = false;
            _goneThroughTeleporter = false;
            _canGrab = true;

            if (_line)
                _line.IgnoreTeleporters = false;

            //Find the panel to drop the opponent at when the whip is done.
            FVector2 dropPanelPos = OwnerMoveScript.Position + new FVector2(1, 0) * OwnerMoveScript.GetAlignmentX();

            GridBehaviour.Instance.GetPanel(dropPanelPos, out _dropPanel);

            if (_dropPanel == null)
            {
                _dropPanel = OwnerMoveScript.CurrentPanel;
            }

            _heldItemSpawn = OwnerMoveset.HeldItemSpawnLeft;
            if (OwnerMoveScript.Alignment == GridScripts.GridAlignment.RIGHT)
                _heldItemSpawn = OwnerMoveset.HeldItemSpawnRight;

            ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.SpawnEffect, _heldItemSpawn, true, alignment: OwnerMoveScript.Alignment);
            _enforcerInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.Visual, _heldItemSpawn, true, alignment: OwnerMoveScript.Alignment).GetComponent<AccessoryEffectBehaviour>();

        }

        protected void OnTeleported(TeleporterBehaviour teleporter)
        {
            _goneThroughTeleporter = true;
            _canGrab = false;
            FixedPointTimer.StartNewTimedAction(() => 
            {
                _canGrab = true;
                _line.IgnoreTeleporters = true;
            }, GridGame.FixedTimeStep);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            base.OnActivate(args);

            //The projectile base class handles firing the whip projectile but we'll set up the trail and collider here.
            _whipTip = Projectile;
            _line = _whipTip.GetComponentInChildren<LineFollowBehaviour>();
            _line.Start = OwnerMoveset.ProjectileSpawner.transform;
            _line.OnTeleportedEvent += OnTeleported;

            HitColliderBehaviour hitCollider = _whipTip.GetComponent<HitColliderBehaviour>();

            hitCollider.ColliderInfo = GetColliderData(0);
            hitCollider.Spawner = Owner;

            hitCollider.OnHit += OnCollision;
        }

        protected override void OnRecover(params object[] args)
        {
            base.OnRecover(args);

            //No need to flip the velocity of the whip if the opponent is already attached.
            if (_opponentAttached)
                return;

            FlipWhipVelocity();

            _returning = true;
        }

        public override void Tick(Fixed32 dt)
        {
            base.Tick(dt);

            if (_whipTip == null || !_whipTip.Active)
                return;


            OwnerMoveset.ProjectileSpawner.Entity.FixedTransform.WorldPosition = _enforcerInstance.ProjectileSpawnPoint.WorldPosition;

            //If the whip is close enough to the owner and is returning end the ability.
            Fixed32 distance = FVector3.Distance(_whipTip.FixedTransform.WorldPosition, Owner.FixedTransform.WorldPosition);

            if (distance <= 1 && _returning)
            {
                EndAbility();
            }
        }

        protected override void OnEnd()
        {
            base.OnEnd();

            //Be sure the opponent is dropped and tell the whip to not worry about dropping the opponent.
            ResetOpponent();

            if (_whipTip)
                _whipTip.Data.ClearEndEvent();

            //If we caught the opponent drop them in front of us.
            if (_opponentAttached)
            {
                _opponentTransform.WorldPosition = _dropPanel.FixedWorldPosition + FVector3.Up;

                _opponentKnockback.MovementBehaviour.AlwaysLookAtOpposingSide = true;
            }

            //Disable the whip and the stun.
            ObjectPoolBehaviour.Instance.ReturnGameObject(_whipTip, alignment: OwnerMoveScript.Alignment);
            OwnerMoveset.ProjectileSpawner.Entity.FixedTransform.LocalPosition = _originalPosition;
            ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.SpawnEffect, _heldItemSpawn, true);


            if (_enforcerInstance != null)
            {
                ObjectPoolBehaviour.Instance.ReturnGameObject(_enforcerInstance.GetComponent<EntityDataBehaviour>(), true);
                _enforcerInstance = null;
            }

            if (_line)
                _line.OnTeleportedEvent -= OnTeleported;
        }

        protected override void OnMatchRestart()
        {
            base.OnMatchRestart();

            OwnerMoveset.ProjectileSpawner.Entity.FixedTransform.LocalPosition = _originalPosition;
        }

        protected override void OnSerialize(BinaryWriter bw)
        {
            base.OnSerialize(bw);

            bw.Write(_returning);
            bw.Write(_opponentAttached);
        }

        protected override void OnDeserialize(BinaryReader br)
        {
            base.OnDeserialize(br);

            _returning = br.ReadBoolean();
            _opponentAttached = br.ReadBoolean();
        }
    }
}