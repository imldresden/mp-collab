namespace IMLD.MixedReality.Network
{
    public interface INetworkFilter
    {
        public void FilterMessage(INetworkService networkService, ref MessageContainer messageContainer);
        public void Dispose();
    }
}