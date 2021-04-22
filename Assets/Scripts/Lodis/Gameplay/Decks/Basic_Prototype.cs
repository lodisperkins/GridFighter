using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class Basic_Prototype : Deck
    {
        public Basic_Prototype() : base()
        {
            AddAbility(new WN_Blaster());
            AddAbility(new WS_DoubleShot());
            AddAbility(new WF_ForwardShot());
            AddAbility(new WB_LobShot());
            AddAbility(new SN_ChargeShot());
            AddAbility(new SS_ChargeDoubleShot());
            AddAbility(new SF_ChargeForwardShot());
            AddAbility(new SB_ChargeLobShot());
        }
    }
}

