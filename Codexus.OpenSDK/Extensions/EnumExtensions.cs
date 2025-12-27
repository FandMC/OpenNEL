using System.Globalization;
using Codexus.OpenSDK.Entities.X19;

namespace Codexus.OpenSDK.Extensions;

public static class EnumExtensions
{
    extension(X19EnumGameType type)
    {
        public string ToOrdinalString()
        {
            return ((int)type).ToString(CultureInfo.InvariantCulture);
        }
    }
}