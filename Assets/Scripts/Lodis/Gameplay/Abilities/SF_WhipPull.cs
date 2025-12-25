using FixedPoints;
using Lodis.Accessories;
using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Utility;
using System;
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
    public class SF_WhipPull : ProjectileAbility
    {
        private LineFollowBehaviour _line;
        private Fixed32 _statScale;
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
        private bool _uppercutHappened;
        private HitColliderBehaviour _hitCollider;

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

            if (!_opponentKnockback || _opponentKnockback.IsIntangible || _opponentKnockback.IsInvincible)
                return;

            //Set up opponent transform to be attached to whip
            _originalParent = collision.OtherEntity.Transform.Parent;
            _opponentTransform = collision.OtherEntity.Transform;
            _opponentTransform.LocalRotation = FQuaternion.Identity;

            collision.OtherEntity.Transform.Parent = _whipTip.FixedTransform;
            collision.OtherEntity.Transform.LocalPosition = FVector3.Zero;

            //Stun the opponent so they can't do anything while being pulled.
            _opponentKnockback.Stun(abilityData.recoverTime);

            _opponentKnockback.MovementBehaviour.CancelMovement();
            _opponentKnockback.MovementBehaviour.AlwaysLookAtOpposingSide = false;
            _opponentKnockback.MovementBehaviour.Position = _dropPanel.Position;

            //Ignore teleporters on the return trip if we didn't go through one already.
            //This prevents weird behavior where the whip still teleports even after catching the opponent.
            if (!_goneThroughTeleporter)
            {
                _line.IgnoreTeleporters = true;
            }

            _returning = true;
            _opponentAttached = true;

            //Make the whip return to the user.
            FlipWhipVelocity();

            //Tell the whip to drop the opponnent if it is destroyed.
            _whipTip.Data.ClearEndEvent();
            _whipTip.Data.OnEnd += ResetOpponent;
        }

        protected void FlipWhipVelocity()
        {
            if (_opponentAttached)
            {
                AnimationClip uppercutClip = null;
                abilityData.GetAdditionalAnimation(0, out uppercutClip);

                OwnerAnimationScript.PlayAnimation(uppercutClip, 1, true);
                FixedPointTimer.StartNewTimedAction(EndAbility, uppercutClip.length);


                GameObject uppercutEffect = MonoBehaviour.Instantiate(abilityData.Effects[0], Owner.transform.position, Camera.main.transform.rotation);
                uppercutEffect.GetComponent<ParticleColorManagerBehaviour>().SetColors(OwnerMoveScript.Alignment);
            }

            GridPhysicsBehaviour physics = _whipTip.GetComponent<GridPhysicsBehaviour>();

            physics.Velocity = -physics.Velocity * 2;
        }

        protected void ResetOpponent()
        {
            _whipTip.FixedTransform.RemoveChild(_opponentTransform);
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            //Reset flags.
            _returning = false;
            _opponentAttached = false;
            _uppercutHappened = false;
            _goneThroughTeleporter = false;
            _canGrab = true;

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

            ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.SpawnEffect, _heldItemSpawn, true);
            _enforcerInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.Visual, _heldItemSpawn, true).GetComponent<AccessoryEffectBehaviour>();

        }
        protected void OnTeleported(TeleporterBehaviour teleporter)
        {
            _goneThroughTeleporter = true;
            _canGrab = false;
            FixedPointTimer.StartNewTimedAction(() => { _canGrab = true; }, GridGame.FixedTimeStep);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            base.OnActivate(args);

            _statScale = (Fixed32)args[0];

            //The projectile base class handles firing the whip projectile but we'll set up the trail and collider here.
            _whipTip = Projectile;
            _line = _whipTip.GetComponentInChildren<LineFollowBehaviour>();
            _line.Start = OwnerMoveset.ProjectileSpawner.transform;
            _line.OnTeleportedEvent += OnTeleported;

            ColliderBehaviour hitCollider = _whipTip.GetComponent<ColliderBehaviour>();

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
            //_whipTip.FixedTransform.WorldPosition = new FVector3(_whipTip.FixedTransform.WorldPosition.X, OwnerMoveset.FixedTransform.WorldPosition.Y, _whipTip.FixedTransform.WorldPosition.Z);

            //If the whip is close enough to the owner and is returning end the ability.
            Fixed32 distance = FVector3.Distance(_whipTip.FixedTransform.WorldPosition, Owner.FixedTransform.WorldPosition);

            if (distance <= 1 && _returning)
            {
                if (!_uppercutHappened && _opponentAttached)
                {
                    HandleEndWhip();

                    _opponentKnockback.MovementBehaviour.SetAlignmentRotation();

                    _hitCollider = HitColliderSpawner.SpawnCollider(Owner.FixedTransform, 2, 2, GetColliderData(0), Owner);
                    _hitCollider.ColliderInfo = _hitCollider.ColliderInfo.ScaleStats(_statScale);
                    ObjectPoolBehaviour.Instance.OnReturnToPool.AddListener(RemoveHitColliderAsChild);
                    _uppercutHappened = true;
                }
                else
                {
                    EndAbility();
                }
            }
        }

        private void RemoveHitColliderAsChild(GameObject g)
        {
            if (_hitCollider == null)
                return;

            if (g != _hitCollider.gameObject)
                return;

            _hitCollider.FixedTransform.Parent = null;
        }

        protected void HandleEndWhip()
        {
            //Be sure the opponent is dropped and tell the whip to not worry about dropping the opponent.
            ResetOpponent();
            _whipTip.Data.ClearEndEvent();

            //If we caught the opponent drop them in front of us.
            if (_opponentAttached)
            {
                _opponentTransform.WorldPosition = _dropPanel.FixedWorldPosition + FVector3.Up;

                _opponentKnockback.MovementBehaviour.AlwaysLookAtOpposingSide = true;
                _opponentKnockback.CancelStun();
            }

            //Disable the whip and the stun.
            ObjectPoolBehaviour.Instance.ReturnGameObject(_whipTip);
            OwnerMoveset.ProjectileSpawner.Entity.FixedTransform.LocalPosition = _originalPosition;
            ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.SpawnEffect, _heldItemSpawn, true);

            if (_hitCollider != null)
                ObjectPoolBehaviour.Instance.ReturnGameObject(_hitCollider.Entity);

            if (_enforcerInstance != null)
            {
                GridGame.RemoveEntityFromGame(_enforcerInstance.GetComponent<EntityDataBehaviour>(), true);
                _enforcerInstance = null;
            }

            _line.OnTeleportedEvent -= OnTeleported;
        }

        protected override void OnEnd()
        {
            base.OnEnd();

            if (_uppercutHappened)
                return;

            HandleEndWhip();
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