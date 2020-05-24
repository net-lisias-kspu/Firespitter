﻿/*
Firespitter /L
Copyright 2013-2018, Andreas Aakvik Gogstad (Snjo)
Copyright 2018-2020, LisiasT

    Developers: LisiasT, Snjo

    This file is part of Firespitter.
*/
using System;
using UnityEngine;

namespace Firespitter.cockpit
{
    /// <summary>
    /// For switches.
    /// Assign this to a collider with isTrigger.
    /// Activates the buttonClick function on the part's first available FSActionGroupSwitch.
    /// Multiple buttons are told apart for the receiving function by the buttonNumber
    /// </summary>
    [Obsolete("Use FSgenericButtonHandler instead", false)]
    public class FSswitchButtonHandler : InternalModule
    {
        /// <summary>
        /// A part with an assigned FSActionGroupSwitch module
        /// </summary>
        public GameObject target;
        /// <summary>
        /// an identifying number to tell multiple buttons apart, if several are assigned to the target module.
        /// </summary>
        public int buttonNumber = 1;

        public void OnMouseDown()
        {
            //target.GetComponent<FSActionGroupSwitch>().buttonClick(buttonNumber);
            target.GetComponent<FSActionGroupSwitch>().buttonClick();
        }
    }
}