namespace KolonyTools
{
    public interface ITransferInfoGuiRenderer<T>
        where T: AbstractLogisticsTransferRequest
    {
        T Transfer { get; set; }
        void SetTransferController(ITransferController<T> controller);
        void DrawWindow();
        bool IsVisible();
        void SetVisible(bool visible);
    }
}
