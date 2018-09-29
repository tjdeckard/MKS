using UnityEngine;

namespace KolonyTools
{
    /// <summary>
    /// Displays the UI for <see cref="AbstractScenarioLogistics{T1, T}"/>.
    /// </summary>
    public abstract class AbstractLogisticsGuiMain_Scenario<T> : IScenarioGuiRenderer<T>, ITransferController<T>
        where T : AbstractLogisticsTransferRequest
    {
        #region Local instance variables
        private ITransferController<T> _transferController;
        private Vector2 _scrollPosition;
        private T _selectedTransfer;
        private bool _isVisible;
        #endregion

        #region Public instance variables
        public ITransferInfoGuiRenderer<T> ReviewTransferGui { get; set; }
        #endregion

        /// <summary>
        /// Called by <see cref="ILogisticsScenario{T}"/>
        /// </summary>
        /// <param name="transferController"></param>
        public void SetTransferController(ITransferController<T> transferController)
        {
            _transferController = transferController;
            SetVisible(true);
        }

        /// <summary>
        /// Called by <see cref="KolonizationMonitor"/> to render the UI.
        /// </summary>
        public abstract void DrawWindow();

        /// <summary>
        /// Implementation of <see cref="Window.SetVisible(bool)"/>.
        /// </summary>
        /// <param name="newValue"></param>
        public void SetVisible(bool newValue)
        {
            _isVisible = newValue;

            // Always hide child windows when main window visibility is altered
            if (ReviewTransferGui != null)
                ReviewTransferGui.SetVisible(false);
        }

        /// <summary>
        /// Returns the current visibility status.
        /// </summary>
        /// <returns></returns>
        public bool IsVisible()
        {
            return _isVisible;
        }

        /// <summary>
        /// Aborts a transfer via <see cref="ILogisticsScenario{T}"/>.
        /// </summary>
        /// <remarks>
        /// Implementation of <see cref="ITransferRequestViewParent.AbortTransfer(T)"/>.
        /// </remarks>
        /// <param name="transfer"></param>
        public void AbortTransfer(T transfer)
        {
            _transferController.AbortTransfer(transfer);
        }

        /// <summary>
        /// Resumes a cancelled transfer via <see cref="ILogisticsScenario{T}"/>.
        /// </summary>
        /// <remarks>
        /// Implementation of <see cref="ITransferRequestViewParent.ResumeTransfer(T)"/>.
        /// </remarks>
        /// <param name="transfer"></param>
        public void ResumeTransfer(T transfer)
        {
            _transferController.ResumeTransfer(transfer);
        }
    }
}
