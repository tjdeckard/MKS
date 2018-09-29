namespace KolonyTools
{
    public interface ITransferController<T>
        where T: AbstractLogisticsTransferRequest
    {
        void AbortTransfer(T transfer);
        void ResumeTransfer(T transfer);
    }
}
