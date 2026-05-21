namespace TeamSpot.Service.Device
{
    public interface IUsbInputEvent { }

    public interface IUsbInputEventParser
    {
        static abstract bool CanParse(byte[] inputReport);
        static abstract IUsbInputEvent Parse(byte[] inputReport);
    }
}
