using System.Text;

namespace Infolyzer.IcuParser
{
    public static class StringBuilderExtensions
    {
        public static string Flush(this StringBuilder sb)
        {
            var value = sb.ToString();
            sb.Clear();
            return value;
        }
    }
}
