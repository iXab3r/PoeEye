namespace PoeShared.Native
{
    public interface ICloseController
    {
        void Close();
    }
    
    public interface ICloseController<in TValue>
    {
        void Close(TValue value);
    }
}