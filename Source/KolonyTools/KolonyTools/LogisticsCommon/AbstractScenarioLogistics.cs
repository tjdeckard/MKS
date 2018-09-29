using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KolonyTools
{
    /// <summary>
    /// Handles the core operations of logistics transfers.
    /// </summary>
    public abstract class AbstractScenarioLogistics<T, T1> : ScenarioModule, ITransferController<T1>
        where T: AbstractLogisticsGuiMain_Scenario<T1>
        where T1: AbstractLogisticsTransferRequest, new()
    {
        #region Local instance variables
        private double _nextCheckTime;
        private bool _isLoaded = false;

        private T _mainGui;
        #endregion

        #region Public instance variables
        public List<T1> PendingTransfers { get; private set; } =
            new List<T1>();

        public List<T1> ExpiredTransfers { get; private set; } =
            new List<T1>();
        #endregion

        public AbstractScenarioLogistics(T guiRenderer)
        {
            _mainGui = guiRenderer;
            _mainGui.SetTransferController(this);
        }

        /// <summary>
        /// Implementation of <see cref="ScenarioModule.OnAwake"/>.
        /// </summary>
        public override void OnAwake()
        {
            base.OnAwake();

            // Schedule the first transfer processing attempt
            _nextCheckTime = Planetarium.GetUniversalTime() + 2;
        }

        /// <summary>
        /// Loads transfers from game save file.
        /// </summary>
        /// <remarks>Implementation of <see cref="ScenarioModule.OnLoad(ConfigNode)"/>.</remarks>
        /// <param name="node"></param>
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            ConfigNode.LoadObjectFromConfig(this, node);

            PendingTransfers.Clear();
            ExpiredTransfers.Clear();

            T1 transfer;
            foreach (ConfigNode subNode in node.nodes)
            {
                transfer = new T1();
                transfer.Load(subNode);

                if (transfer.Status == DeliveryStatus.Launched || transfer.Status == DeliveryStatus.Returning)
                    PendingTransfers.Add(transfer);
                else
                    ExpiredTransfers.Add(transfer);
            }

            // Sort transfers by arrival time
            PendingTransfers.Sort();
            ExpiredTransfers.Sort();

            _isLoaded = true;
        }

        /// <summary>
        /// Persists transfers to game save file.
        /// </summary>
        /// <remarks>Implementation of <see cref="ScenarioModule.OnSave(ConfigNode)"/>.</remarks>
        /// <param name="node"></param>
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            ConfigNode.CreateConfigFromObject(this, node);

            ConfigNode transferNode;
            foreach (var transfer in PendingTransfers)
            {
                transferNode = new ConfigNode();
                transfer.Save(transferNode);

                node.AddNode(transferNode);
            }
            foreach (var transfer in ExpiredTransfers)
            {
                transferNode = new ConfigNode();
                transfer.Save(transferNode);

                node.AddNode(transferNode);
            }
        }

        /// <summary>
        /// Click handler for toolbar button.
        /// </summary>
        private void GuiOn()
        {
            try
            {
                _mainGui.SetVisible(true);
            }
            catch (Exception ex)
            {
                Debug.LogError("[MKS] ERROR in " + GetType().Name + ".GuiOn: " + ex.Message);
            }
        }

        /// <summary>
        /// Click handler for toolbar button.
        /// </summary>
        private void GuiOff()
        {
            try
            {
                _mainGui.SetVisible(false);
            }
            catch (Exception ex)
            {
                Debug.LogError("[MKS] ERROR in " + GetType().Name + ".GuiOff: " + ex.Message);
            }
        }

        /// <summary>
        /// Implementation of <see cref="MonoBehaviour"/>.OnGUI
        /// </summary>
        void OnGUI()
        {
            try
            {
                if (!_isLoaded || !_mainGui.IsVisible())
                    return;

                // Draw main window and transfer view window, if available
                _mainGui.DrawWindow();

                if (_mainGui.ReviewTransferGui != null && _mainGui.ReviewTransferGui.IsVisible())
                    _mainGui.ReviewTransferGui.DrawWindow();
            }
            catch (Exception ex)
            {
                Debug.LogError("[MKS] ERROR in " + GetType().Name + ".OnGUI: " + ex.Message);
            }
        }

        /// <summary>
        /// Implementation of <see cref="MonoBehaviour"/>.Update.
        /// </summary>
        /// <remarks>This is where resource exchange between vessels is initiated.</remarks>
        void Update()
        {
            // Transfers won't be processed during time warp and don't need to run every frame 
            if (
                !_isLoaded || PendingTransfers.Count < 1 || _nextCheckTime > Planetarium.GetUniversalTime()
                || (TimeWarp.CurrentRate > 1 && TimeWarp.WarpMode == TimeWarp.Modes.HIGH)
            )
            {
                return;
            }

            // To further reduce the impact on frame times, processing is done in a coroutine.
            StartCoroutine(ProcessTransfers());

            // Wait for 2 seconds before next processing window
            _nextCheckTime = Planetarium.GetUniversalTime() + 2;
        }

        /// <summary>
        /// Processes transfers that are ready for delivery.
        /// </summary>
        IEnumerator ProcessTransfers()
        {
            // C# Tip: Copy List into an array and iterate over the array if List will be modified
            // Unity Performance Tip: Use <for> instead of <foreach>
            T1[] transferList = PendingTransfers.ToArray();
            T1 transfer;
            for (int i = 0; i < transferList.Length; i++)
            {
                transfer = transferList[i];

                // This should never happen but in case it does...
                if (transfer.Status != DeliveryStatus.Launched && transfer.Status != DeliveryStatus.Returning)
                {
                    // Move the transfer out of pending into expired
                    PendingTransfers.Remove(transfer);
                    ExpiredTransfers.Add(transfer);
                    ExpiredTransfers.Sort();
                }
                // Look for transfers that are ready for delivery
                else if (transfer.GetArrivalTime() <= Planetarium.GetUniversalTime())
                {
                    // Allow Unity to be even lazier about processing the delivery with another coroutine
                    StartCoroutine(transfer.Deliver());

                    while (transfer.Status == DeliveryStatus.Launched || transfer.Status == DeliveryStatus.Returning)
                        yield return null;

                    // Move the transfer out of pending into expired
                    PendingTransfers.Remove(transfer);
                    ExpiredTransfers.Add(transfer);
                    ExpiredTransfers.Sort();
                }
            }
        }

        /// <summary>
        /// Abort the transfer and remove from transfer list.
        /// </summary>
        /// <param name="transfer"></param>
        public void AbortTransfer(T1 transfer)
        {
            if (ExpiredTransfers.Contains(transfer))
            {
                ExpiredTransfers.Remove(transfer);
            }
            if (PendingTransfers.Contains(transfer))
            {
                transfer.Abort();
            }
        }

        /// <summary>
        /// Resume the transfer.
        /// </summary>
        /// <param name="transfer"></param>
        public void ResumeTransfer(T1 transfer)
        {
            if (PendingTransfers.Contains(transfer))
            {
                PendingTransfers.Remove(transfer);
                transfer.Launch(PendingTransfers);
            }
        }
    }
}
