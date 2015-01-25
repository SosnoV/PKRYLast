
namespace PkryServ
{
    class Program
    {
        static void Main(string[] args)
        {
            Server ser = new Server("localhost");
            ser.Run();
        }
    }
}
