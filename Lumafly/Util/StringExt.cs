using System;

namespace Lumafly.Util
{
  public static class StringExt
  {
    public static string FormatWith(this string format, params object[] args)
    {
      try
      {
        return string.Format(format, args);
      }
      catch (FormatException)
      {
        return format; // fallback: return unformatted if format invalid
      }
    }
  }
}
