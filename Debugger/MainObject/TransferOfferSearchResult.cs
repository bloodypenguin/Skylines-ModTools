namespace ModTools.MainObject
{
    public readonly struct TransferOfferSearchResult
    {
        public readonly TransferManager.TransferOffer TransferOffer;
        public readonly uint ValueIndex;

        public TransferOfferSearchResult(uint valueIndex, TransferManager.TransferOffer transferOffer)
        {
            ValueIndex = valueIndex;
            TransferOffer = transferOffer;
        }
    }
}