namespace ReefExplorer.Interaction
{
    public interface IScannable
    {
        bool IsScanned { get; }
        bool TryScan();
        void ResetScanned();
    }
}
