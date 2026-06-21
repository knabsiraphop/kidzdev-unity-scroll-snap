namespace KidzDev.Unity.ScrollSnap
{
    public interface IScrollSnapIndicator
    {
        void Setup(int pageCount);
        void OnPageChanged(int page);
    }
}
