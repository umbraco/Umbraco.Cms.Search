namespace Package.Helpers;

public interface IDateTimeOffsetConverter
{
    DateTimeOffset ToDateTimeOffset(DateTime dateTime);
}