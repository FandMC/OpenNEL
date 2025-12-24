namespace OpenNEL.WPFLauncher.Entities.RentalGame;

public enum EnumServerStatus
{
    None = -1,
    ServerOff,
    ServerOn,
    Uninitialized,
    Opening,
    Closing,
    OutOfDate,
    SaveCleaning,
    Resetting,
    Upgrading,
    DiscOverflow
}
