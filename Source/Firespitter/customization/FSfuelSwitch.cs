﻿/*
Firespitter /L
Copyright 2013-2018, Andreas Aakvik Gogstad (Snjo)
Copyright 2018-2020, LisiasT

    Developers: LisiasT, Snjo

    This file is part of Firespitter.
*/
using System.Collections.Generic;
using System.Text;

namespace Firespitter.customization
{
    public class FSfuelSwitch : PartModule, IPartCostModifier
    {
        [KSPField]
        public string resourceNames = "ElectricCharge;LiquidFuel,Oxidizer;MonoPropellant";
        [KSPField]
        public string resourceAmounts = "100;75,25;200";
        [KSPField]
        public string initialResourceAmounts = "";
        [KSPField]
        public float basePartMass = 0.25f;
        [KSPField]
        public string tankMass = "0;0;0;0";
        [KSPField]
        public string tankCost = "0; 0; 0; 0";
        [KSPField]
        public bool displayCurrentTankCost = false;
        [KSPField]
        public bool hasGUI = true;
        [KSPField]
        public bool availableInFlight = false;
        [KSPField]
        public bool availableInEditor = true;
        
        [KSPField(isPersistant = true)]
        public int selectedTankSetup = -1;
        [KSPField(isPersistant = true)]
        public bool hasLaunched = false;
        [KSPField]
        public bool showInfo = true; // if false, does not feed info to the part list pop up info menu

        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Added cost")]
        public float addedCost = 0f;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "Dry mass")]
        public float dryMassInfo = 0f;
        private List<FSmodularTank> tankList;
        private List<double> weightList;
        private List<double> tankCostList;
        private bool initialized = false;
        [KSPField (isPersistant = true)]
        public bool configLoaded = false;

        UIPartActionWindow tweakableUI;        

        public override void OnStart(PartModule.StartState state)
        {            
            initializeData();
            if (selectedTankSetup == -1)
            {
                selectedTankSetup = 0;
                assignResourcesToPart(false);
            }
        }

        public override void OnAwake()
        {
            Log.dbg("FS AWAKE {0} {1} {2}", initialized, configLoaded, resourceAmounts);
            if (configLoaded)
            {
                initializeData();
            }
            Log.dbg("FS AWAKE DONE {0}", (configLoaded ? tankList.Count.ToString() : "NO CONFIG"));
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            Log.dbg("FS LOAD {0} {1} {2}", initialized, resourceAmounts, configLoaded);
            if (!configLoaded)
            {
                initializeData();
            }
            if (basePartMass != part.mass)
            {
                Log.err("FSFuelSwitch Mass Discrepancy detected in part '{0}'!", part.name);
            }
            configLoaded = true;
            Log.dbg("FS LOAD DONE {0}", tankList.Count);
        }

        private void initializeData()
        {
            if (!initialized)
            {
                setupTankList(false);
                weightList = Tools.parseDoubles(tankMass);
                tankCostList = Tools.parseDoubles(tankCost);
                if (HighLogic.LoadedSceneIsFlight) hasLaunched = true;
                if (hasGUI)
                {
                    Events["nextTankSetupEvent"].guiActive = availableInFlight;
                    Events["nextTankSetupEvent"].guiActiveEditor = availableInEditor;
                    Events["previousTankSetupEvent"].guiActive = availableInFlight;
                    Events["previousTankSetupEvent"].guiActiveEditor = availableInEditor;
                }
                else
                {
                    Events["nextTankSetupEvent"].guiActive = false;
                    Events["nextTankSetupEvent"].guiActiveEditor = false;
                    Events["previousTankSetupEvent"].guiActive = false;
                    Events["previousTankSetupEvent"].guiActiveEditor = false;
                }

                if (HighLogic.CurrentGame == null || HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                {
                    Fields["addedCost"].guiActiveEditor = displayCurrentTankCost;
                }

                initialized = true;
            }
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Next tank setup")]
        public void nextTankSetupEvent()
        {
            selectedTankSetup++;
            if (selectedTankSetup >= tankList.Count)
            {
                selectedTankSetup = 0;
            }
            assignResourcesToPart(true);            
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Previous tank setup")]
        public void previousTankSetupEvent()
        {
            selectedTankSetup--;
            if (selectedTankSetup < 0)
            {
                selectedTankSetup = tankList.Count-1;
            }
            assignResourcesToPart(true);
        }

        public void selectTankSetup(int i, bool calledByPlayer)
        {            
            initializeData();
            if (selectedTankSetup != i)
            {
                selectedTankSetup = i;
                assignResourcesToPart(calledByPlayer);
            }
        }

        private void assignResourcesToPart(bool calledByPlayer)
        {            
            // destroying a resource messes up the gui in editor, but not in flight.
            setupTankInPart(part, calledByPlayer);
            if (HighLogic.LoadedSceneIsEditor)
            {
                for (int s = 0; s < part.symmetryCounterparts.Count; s++)
                {
                    setupTankInPart(part.symmetryCounterparts[s], calledByPlayer);
                    FSfuelSwitch symSwitch = part.symmetryCounterparts[s].GetComponent<FSfuelSwitch>();
                    if (symSwitch != null)
                    {
                        symSwitch.selectedTankSetup = selectedTankSetup;
                    }
                }
            }

            Log.dbg("refreshing UI");

            if (tweakableUI == null)
            {
                tweakableUI = Tools.FindActionWindow(part);
            }
            if (tweakableUI != null)
            {
                tweakableUI.CreatePartList(true);
                tweakableUI.displayDirty = true;
            }
            else
            {
                Log.info("no UI to refresh");
            }
        }

        private void setupTankInPart(Part currentPart, bool calledByPlayer)
        {
            currentPart.Resources.dict = new DictionaryValueList<int, PartResource>();
            PartResource[] partResources = currentPart.GetComponents<PartResource>();
            //for (int i = 0; i < partResources.Length; i++)
            //{
            //    DestroyImmediate(partResources[i]);
            //}            

            for (int tankCount = 0; tankCount < tankList.Count; tankCount++)
            {
                if (selectedTankSetup == tankCount)
                {
                    for (int resourceCount = 0; resourceCount < tankList[tankCount].resources.Count; resourceCount++)
                    {
                        if (tankList[tankCount].resources[resourceCount].name != "Structural")
                        {
                            //Log.dbg("new node: {0}", tankList[i].resources[j].name);
                            ConfigNode newResourceNode = new ConfigNode("RESOURCE");
                            newResourceNode.AddValue("name", tankList[tankCount].resources[resourceCount].name);
                            newResourceNode.AddValue("maxAmount", tankList[tankCount].resources[resourceCount].maxAmount);
                            if (calledByPlayer && !HighLogic.LoadedSceneIsEditor)
                            {
                                newResourceNode.AddValue("amount", 0.0f);
                            } 
                            else
                            {
                                newResourceNode.AddValue("amount", tankList[tankCount].resources[resourceCount].amount);
                            }

                            Log.dbg("add node to part");
                            currentPart.AddResource(newResourceNode);                          
                        }
                        else
                        {
                            Log.dbg("Skipping structural fuel type");
                        }
                    }
                }
            }

            updateWeight(currentPart, selectedTankSetup);
            updateCost();
        }

        private float updateCost()
        {
            if (selectedTankSetup >= 0 && selectedTankSetup < tankCostList.Count)
            {
                addedCost = (float)tankCostList[selectedTankSetup];
            }
            else
            {
                addedCost = 0f;
            }
            //GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship); //crashes game
            return addedCost;
        }

        private void updateWeight(Part currentPart, int newTankSetup)
        {
            if (newTankSetup < weightList.Count)
            {
                currentPart.mass = (float)(basePartMass + weightList[newTankSetup]);
            }
        }

        public override void OnUpdate()
        {
        }

        public void Update()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                dryMassInfo = part.mass;
            }
        }

        private void setupTankList(bool calledByPlayer)
        {
            tankList = new List<FSmodularTank>();
            weightList = new List<double>();
            tankCostList = new List<double>();

            // First find the amounts each tank type is filled with

            List<List<double>> resourceList = new List<List<double>>();
            List<List<double>> initialResourceList = new List<List<double>>();
            string[] resourceTankArray = resourceAmounts.Split(';');
            string[] initialResourceTankArray = initialResourceAmounts.Split(';');
            if (initialResourceAmounts.Equals("") ||
                initialResourceTankArray.Length != resourceTankArray.Length)
            {
                initialResourceTankArray = resourceTankArray;
            }
            Log.dbg("FSDEBUGRES: {0} {1}", resourceTankArray.Length, resourceAmounts);
            for (int tankCount = 0; tankCount < resourceTankArray.Length; tankCount++)
            {
                resourceList.Add(new List<double>());
                initialResourceList.Add(new List<double>());
                string[] resourceAmountArray = resourceTankArray[tankCount].Trim().Split(',');
                string[] initialResourceAmountArray = initialResourceTankArray[tankCount].Trim().Split(',');
                if (initialResourceAmounts.Equals("") ||
                    initialResourceAmountArray.Length != resourceAmountArray.Length)
                {
                    initialResourceAmountArray = resourceAmountArray;
                }
                for (int amountCount = 0; amountCount < resourceAmountArray.Length; amountCount++)
                {
                    try
                    {
                        resourceList[tankCount].Add(double.Parse(resourceAmountArray[amountCount].Trim()));
                        initialResourceList[tankCount].Add(double.Parse(initialResourceAmountArray[amountCount].Trim()));
                    }
                    catch
                    {
                        Log.err("FSfuelSwitch: error parsing resource amount {0}/{1}: '{2}': '{3}'", tankCount, amountCount, resourceTankArray[amountCount], resourceAmountArray[amountCount].Trim());
                    }
                }
            }

            // Then find the kinds of resources each tank holds, and fill them with the amounts found previously, or the amount hey held last (values kept in save persistence/craft)

            string[] tankArray = resourceNames.Split(';');
            for (int tankCount = 0; tankCount < tankArray.Length; tankCount++)
            {
                FSmodularTank newTank = new FSmodularTank();
                tankList.Add(newTank);
                string[] resourceNameArray = tankArray[tankCount].Split(',');
                for (int nameCount = 0; nameCount < resourceNameArray.Length; nameCount++)
                {
                    engine.FSresource newResource = new engine.FSresource(resourceNameArray[nameCount].Trim(' '));
                    if (resourceList[tankCount] != null)
                    {
                        if (nameCount < resourceList[tankCount].Count)
                        {
                            newResource.maxAmount = resourceList[tankCount][nameCount];
                            newResource.amount    = initialResourceList[tankCount][nameCount];
                        }
                    }
                    newTank.resources.Add(newResource);
                }
            }            
        }

        public override string GetInfo()
        {
            if (showInfo)
            {
                List<string> resourceList = Tools.parseNames(resourceNames);
                StringBuilder info = new StringBuilder();
                info.AppendLine("Fuel tank setups available:");
                for (int i = 0; i < resourceList.Count; i++)
                {
                    info.AppendLine(resourceList[i].Replace(",", ", "));
                }
                return info.ToString();
            }
            else
                return string.Empty;
        }

        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            return updateCost();
        }

        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return ModifierChangeWhen.CONSTANTLY;
        }
    }
}
