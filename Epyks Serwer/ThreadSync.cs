
namespace Epyks_Serwer
{
    static class ThreadSync
    {
        public static object Lock { get; private set; } = new object();
    }
}
